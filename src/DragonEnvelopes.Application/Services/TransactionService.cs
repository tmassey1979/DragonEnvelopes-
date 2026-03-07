using System.Diagnostics;
using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class TransactionService(
    ITransactionRepository transactionRepository,
    IEnvelopeRepository envelopeRepository,
    ICategorizationRuleEngine categorizationRuleEngine,
    IIncomeAllocationEngine incomeAllocationEngine,
    ISpendAnomalyService? spendAnomalyService = null,
    IIntegrationOutboxRepository? integrationOutboxRepository = null) : ITransactionService
{
    private const string OutboxSchemaVersion = "1.0";
    private const string LedgerSourceService = "ledger-api";

    public async Task<TransactionDetails> CreateAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        bool hasSplits,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default)
    {
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(accountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        var categoryAssignedByAutomation = false;
        if (string.IsNullOrWhiteSpace(category))
        {
            category = await categorizationRuleEngine.EvaluateAsync(
                familyId.Value,
                description,
                merchant,
                amount,
                category,
                cancellationToken);
            categoryAssignedByAutomation = !string.IsNullOrWhiteSpace(category);
        }

        var hasManualSplitItems = hasSplits && splits is { Count: > 0 };
        var splitInputs = hasManualSplitItems
            ? splits!.ToArray()
            : Array.Empty<TransactionSplitCreateDetails>();
        var usedAutomaticAllocation = false;

        if (!hasManualSplitItems && !envelopeId.HasValue && amount > 0m)
        {
            splitInputs = (await incomeAllocationEngine.AllocateAsync(
                    familyId.Value,
                    description,
                    merchant,
                    amount,
                    category,
                    cancellationToken))
                .ToArray();
            usedAutomaticAllocation = splitInputs.Length > 0;
        }

        var hasSplitItems = splitInputs.Length > 0;
        Envelope? envelope = null;
        if (envelopeId.HasValue && !hasManualSplitItems)
        {
            envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }
        }

        var splitEntries = new List<TransactionSplitEntry>();
        Transaction transaction;
        if (hasSplitItems)
        {
            if (usedAutomaticAllocation)
            {
                transaction = new Transaction(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(amount),
                    description,
                    merchant,
                    occurredAt,
                    category,
                    envelopeId: null);
            }
            else
            {
                var splitValueObjects = splitInputs
                    .Select(static split => new TransactionSplit(
                        split.EnvelopeId,
                        Money.FromDecimal(split.Amount),
                        split.Category))
                    .ToArray();

                transaction = Transaction.CreateWithSplits(
                    Guid.NewGuid(),
                    accountId,
                    Money.FromDecimal(amount),
                    description,
                    merchant,
                    occurredAt,
                    splitValueObjects,
                    category);
            }

            foreach (var split in splitInputs)
            {
                var splitEnvelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (splitEnvelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                var splitAmount = Money.FromDecimal(Math.Abs(split.Amount));
                if (split.Amount < 0m)
                {
                    splitEnvelope.Spend(splitAmount, occurredAt);
                }
                else
                {
                    splitEnvelope.Allocate(splitAmount, occurredAt);
                }

                splitEntries.Add(new TransactionSplitEntry(
                    Guid.NewGuid(),
                    transaction.Id,
                    split.EnvelopeId,
                    Money.FromDecimal(split.Amount),
                    split.Category,
                    split.Notes));
            }
        }
        else
        {
            transaction = new Transaction(
                Guid.NewGuid(),
                accountId,
                Money.FromDecimal(amount),
                description,
                merchant,
                occurredAt,
                category,
                envelopeId);
        }

        if (envelope is not null)
        {
            var transactionAmount = Money.FromDecimal(Math.Abs(amount));
            if (amount < 0m)
            {
                envelope.Spend(transactionAmount, occurredAt);
            }
            else
            {
                envelope.Allocate(transactionAmount, occurredAt);
            }
        }

        await transactionRepository.AddTransactionAsync(transaction, splitEntries, cancellationToken);
        if (integrationOutboxRepository is not null)
        {
            var eventTimestamp = DateTimeOffset.UtcNow;
            if (categoryAssignedByAutomation)
            {
                await EnqueueAutomationOutboxAsync(
                    familyId.Value,
                    IntegrationEventRoutingKeys.AutomationRuleExecutedV1,
                    AutomationIntegrationEventNames.AutomationRuleExecuted,
                    new AutomationRuleExecutedIntegrationEvent(
                        Guid.NewGuid(),
                        eventTimestamp,
                        familyId.Value,
                        ResolveCorrelationId(),
                        transaction.Id,
                        ExecutionType: "Categorization",
                        AssignedCategory: category,
                        AppliedSplits: false,
                        SplitCount: 0),
                    eventTimestamp,
                    cancellationToken);
            }

            if (usedAutomaticAllocation)
            {
                await EnqueueAutomationOutboxAsync(
                    familyId.Value,
                    IntegrationEventRoutingKeys.AutomationRuleExecutedV1,
                    AutomationIntegrationEventNames.AutomationRuleExecuted,
                    new AutomationRuleExecutedIntegrationEvent(
                        Guid.NewGuid(),
                        eventTimestamp,
                        familyId.Value,
                        ResolveCorrelationId(),
                        transaction.Id,
                        ExecutionType: "Allocation",
                        AssignedCategory: category,
                        AppliedSplits: splitEntries.Count > 0,
                        SplitCount: splitEntries.Count),
                    eventTimestamp,
                    cancellationToken);
            }

            var createdEvent = new LedgerTransactionCreatedIntegrationEvent(
                Guid.NewGuid(),
                eventTimestamp,
                familyId.Value,
                transaction.Id,
                transaction.AccountId,
                transaction.Amount.Amount,
                transaction.Description,
                transaction.Merchant,
                transaction.Category,
                transaction.EnvelopeId,
                splitEntries.Count > 0);
            await EnqueueLedgerOutboxAsync(
                familyId.Value,
                IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
                LedgerIntegrationEventNames.TransactionCreated,
                createdEvent,
            cancellationToken);
        }
        await transactionRepository.SaveChangesAsync(cancellationToken);

        if (spendAnomalyService is not null && amount < 0m)
        {
            await spendAnomalyService.DetectAndRecordAsync(
                familyId.Value,
                transaction.Id,
                transaction.AccountId,
                transaction.Merchant,
                transaction.Amount.Amount,
                transaction.OccurredAt,
                cancellationToken);
        }

        return Map(transaction, splitEntries);
    }

    public async Task<IReadOnlyList<TransactionDetails>> ListAsync(
        Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await transactionRepository.ListTransactionsAsync(accountId, cancellationToken);
        var transactionIds = transactions.Select(static transaction => transaction.Id).ToArray();
        var splits = await transactionRepository.ListTransactionSplitsAsync(transactionIds, cancellationToken);
        var splitsByTransaction = splits
            .GroupBy(static split => split.TransactionId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        return transactions
            .Select(transaction =>
            {
                splitsByTransaction.TryGetValue(transaction.Id, out var transactionSplits);
                return Map(transaction, transactionSplits ?? []);
            })
            .ToArray();
    }

    public async Task<TransactionDetails> UpdateAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitCreateDetails>? splits,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(transaction.AccountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        if (transaction.DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Deleted transactions cannot be updated.");
        }

        if (transaction.IsTransfer)
        {
            throw new DomainValidationException("Transfer transactions cannot be edited.");
        }

        var existingSplitsForEvent = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);
        if (replaceAllocation)
        {
            if (envelopeId.HasValue && splits is { Count: > 0 })
            {
                throw new DomainValidationException("EnvelopeId cannot be set when splits are provided.");
            }

            if (splits is { Count: > 0 })
            {
                var splitTotal = splits.Sum(static split => split.Amount);
                if (splitTotal != transaction.Amount.Amount)
                {
                    throw new DomainValidationException("Split totals must equal transaction amount.");
                }
            }

            var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);
            await RebalanceEnvelopesForAllocationChangeAsync(
                transaction,
                existingSplits,
                envelopeId,
                splits,
                cancellationToken);

            var updatedSplitEntries = splits is { Count: > 0 }
                ? splits.Select(split => new TransactionSplitEntry(
                        Guid.NewGuid(),
                        transaction.Id,
                        split.EnvelopeId,
                        Money.FromDecimal(split.Amount),
                        split.Category,
                        split.Notes))
                    .ToArray()
                : [];
            await transactionRepository.ReplaceTransactionSplitsAsync(
                transaction.Id,
                updatedSplitEntries,
                cancellationToken);

            transaction.AssignEnvelope(envelopeId);
        }

        transaction.UpdateMetadata(description, merchant, category);
        if (integrationOutboxRepository is not null)
        {
            var hasSplits = replaceAllocation
                ? splits is { Count: > 0 }
                : existingSplitsForEvent.Count > 0;
            var updatedEvent = new TransactionUpdatedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                familyId.Value,
                ResolveCorrelationId(),
                transaction.Id,
                transaction.AccountId,
                transaction.Amount.Amount,
                transaction.Description,
                transaction.Merchant,
                transaction.Category,
                transaction.EnvelopeId,
                hasSplits,
                replaceAllocation);
            await EnqueueLedgerOutboxAsync(
                familyId.Value,
                IntegrationEventRoutingKeys.LedgerTransactionUpdatedV1,
                LedgerIntegrationEventNames.TransactionUpdated,
                updatedEvent,
                cancellationToken);
        }
        await transactionRepository.SaveChangesAsync(cancellationToken);

        var refreshedSplits = await transactionRepository.ListTransactionSplitsAsync([transactionId], cancellationToken);
        return Map(transaction, refreshedSplits);
    }

    public async Task DeleteAsync(
        Guid transactionId,
        string? deletedByUserId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(transaction.AccountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        if (transaction.IsTransfer)
        {
            throw new DomainValidationException("Transfer transactions cannot be deleted.");
        }

        var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);

        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyReverseAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var envelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyReverseAmountToEnvelope(envelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        transaction.SoftDelete(DateTimeOffset.UtcNow, deletedByUserId);
        if (integrationOutboxRepository is not null)
        {
            var deletedEvent = new TransactionDeletedIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                familyId.Value,
                ResolveCorrelationId(),
                transaction.Id,
                transaction.AccountId,
                transaction.Amount.Amount,
                deletedByUserId);
            await EnqueueLedgerOutboxAsync(
                familyId.Value,
                IntegrationEventRoutingKeys.LedgerTransactionDeletedV1,
                LedgerIntegrationEventNames.TransactionDeleted,
                deletedEvent,
                cancellationToken);
        }
        await transactionRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransactionDetails> RestoreAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetTransactionByIdForUpdateAsync(transactionId, cancellationToken);
        if (transaction is null)
        {
            throw new DomainValidationException("Transaction was not found.");
        }
        var familyId = await transactionRepository.GetAccountFamilyIdAsync(transaction.AccountId, cancellationToken);
        if (!familyId.HasValue)
        {
            throw new DomainValidationException("Account was not found.");
        }

        if (!transaction.DeletedAtUtc.HasValue)
        {
            throw new DomainValidationException("Transaction is not deleted.");
        }

        if (transaction.IsTransfer)
        {
            throw new DomainValidationException("Transfer transactions cannot be restored.");
        }

        var existingSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);

        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyTransactionAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var envelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (envelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyTransactionAmountToEnvelope(envelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        transaction.Restore();
        if (integrationOutboxRepository is not null)
        {
            var restoredEvent = new TransactionRestoredIntegrationEvent(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                familyId.Value,
                ResolveCorrelationId(),
                transaction.Id,
                transaction.AccountId,
                transaction.Amount.Amount);
            await EnqueueLedgerOutboxAsync(
                familyId.Value,
                IntegrationEventRoutingKeys.LedgerTransactionRestoredV1,
                LedgerIntegrationEventNames.TransactionRestored,
                restoredEvent,
                cancellationToken);
        }
        await transactionRepository.SaveChangesAsync(cancellationToken);

        var refreshedSplits = await transactionRepository.ListTransactionSplitsByTransactionIdAsync(transactionId, cancellationToken);
        return Map(transaction, refreshedSplits);
    }

    public async Task<IReadOnlyList<TransactionDetails>> ListDeletedAsync(
        Guid familyId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var boundedDays = Math.Clamp(days, 1, 90);
        var deletedSinceUtc = DateTimeOffset.UtcNow.AddDays(-boundedDays);
        var transactions = await transactionRepository.ListDeletedTransactionsByFamilyAsync(
            familyId,
            deletedSinceUtc,
            cancellationToken);

        var transactionIds = transactions.Select(static transaction => transaction.Id).ToArray();
        var splits = await transactionRepository.ListTransactionSplitsAsync(transactionIds, cancellationToken);
        var splitsByTransaction = splits
            .GroupBy(static split => split.TransactionId)
            .ToDictionary(static group => group.Key, static group => group.ToArray());

        return transactions
            .Select(transaction =>
            {
                splitsByTransaction.TryGetValue(transaction.Id, out var transactionSplits);
                return Map(transaction, transactionSplits ?? []);
            })
            .ToArray();
    }

    private async Task RebalanceEnvelopesForAllocationChangeAsync(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> existingSplits,
        Guid? updatedEnvelopeId,
        IReadOnlyList<TransactionSplitCreateDetails>? updatedSplits,
        CancellationToken cancellationToken)
    {
        if (existingSplits.Count > 0)
        {
            foreach (var split in existingSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyReverseAmountToEnvelope(envelope, split.Amount.Amount, transaction.OccurredAt);
            }
        }
        else if (transaction.EnvelopeId.HasValue)
        {
            var currentEnvelope = await envelopeRepository.GetByIdForUpdateAsync(transaction.EnvelopeId.Value, cancellationToken);
            if (currentEnvelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyReverseAmountToEnvelope(currentEnvelope, transaction.Amount.Amount, transaction.OccurredAt);
        }

        if (updatedSplits is { Count: > 0 })
        {
            foreach (var split in updatedSplits)
            {
                var envelope = await envelopeRepository.GetByIdForUpdateAsync(split.EnvelopeId, cancellationToken);
                if (envelope is null)
                {
                    throw new DomainValidationException($"Envelope was not found for split id {split.EnvelopeId}.");
                }

                ApplyTransactionAmountToEnvelope(envelope, split.Amount, transaction.OccurredAt);
            }

            return;
        }

        if (updatedEnvelopeId.HasValue)
        {
            var updatedEnvelope = await envelopeRepository.GetByIdForUpdateAsync(updatedEnvelopeId.Value, cancellationToken);
            if (updatedEnvelope is null)
            {
                throw new DomainValidationException("Envelope was not found.");
            }

            ApplyTransactionAmountToEnvelope(updatedEnvelope, transaction.Amount.Amount, transaction.OccurredAt);
        }
    }

    private static void ApplyTransactionAmountToEnvelope(Envelope envelope, decimal amount, DateTimeOffset occurredAt)
    {
        var absoluteAmount = Money.FromDecimal(Math.Abs(amount));
        if (amount < 0m)
        {
            envelope.Spend(absoluteAmount, occurredAt);
        }
        else
        {
            envelope.Allocate(absoluteAmount, occurredAt);
        }
    }

    private static void ApplyReverseAmountToEnvelope(Envelope envelope, decimal amount, DateTimeOffset occurredAt)
    {
        var absoluteAmount = Money.FromDecimal(Math.Abs(amount));
        if (amount < 0m)
        {
            envelope.Allocate(absoluteAmount, occurredAt);
        }
        else
        {
            envelope.Spend(absoluteAmount, occurredAt);
        }
    }

    private async Task EnqueueLedgerOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        CancellationToken cancellationToken)
    {
        if (integrationOutboxRepository is null)
        {
            return;
        }

        var outboxMessage = new IntegrationOutboxMessage(
            Guid.NewGuid(),
            familyId,
            ResolveEventId(payload),
            routingKey,
            eventName,
            OutboxSchemaVersion,
            LedgerSourceService,
            ResolveCorrelationId(payload),
            causationId: null,
            JsonSerializer.Serialize(payload),
            ResolveOccurredAtUtc(payload),
            DateTimeOffset.UtcNow);
        await integrationOutboxRepository.AddAsync(outboxMessage, cancellationToken);
    }

    private Task EnqueueAutomationOutboxAsync<TPayload>(
        Guid familyId,
        string routingKey,
        string eventName,
        TPayload payload,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        return IntegrationOutboxEnqueuer.EnqueueAsync(
            integrationOutboxRepository,
            familyId,
            IntegrationEventSourceServices.AutomationApi,
            routingKey,
            eventName,
            payload,
            createdAtUtc,
            cancellationToken);
    }

    private static string ResolveCorrelationId<TPayload>(TPayload payload)
    {
        if (TryGetStringProperty(payload, "CorrelationId", out var correlationId))
        {
            return correlationId;
        }

        return ResolveCorrelationId();
    }

    private static string ResolveCorrelationId()
    {
        return Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
    }

    private static string ResolveEventId<TPayload>(TPayload payload)
    {
        if (TryGetGuidProperty(payload, "EventId", out var eventId))
        {
            return eventId.ToString("D");
        }

        return Guid.NewGuid().ToString("D");
    }

    private static DateTimeOffset ResolveOccurredAtUtc<TPayload>(TPayload payload)
    {
        if (TryGetDateTimeOffsetProperty(payload, "OccurredAtUtc", out var occurredAtUtc))
        {
            return occurredAtUtc;
        }

        return DateTimeOffset.UtcNow;
    }

    private static bool TryGetStringProperty<TPayload>(TPayload payload, string propertyName, out string value)
    {
        value = string.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        value = stringValue.Trim();
        return true;
    }

    private static bool TryGetGuidProperty<TPayload>(TPayload payload, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not Guid guidValue || guidValue == Guid.Empty)
        {
            return false;
        }

        value = guidValue;
        return true;
    }

    private static bool TryGetDateTimeOffsetProperty<TPayload>(TPayload payload, string propertyName, out DateTimeOffset value)
    {
        value = default;
        var property = payload?.GetType().GetProperty(propertyName);
        if (property?.GetValue(payload) is not DateTimeOffset dateTimeOffsetValue || dateTimeOffsetValue == default)
        {
            return false;
        }

        value = dateTimeOffsetValue;
        return true;
    }

    private static TransactionDetails Map(
        Transaction transaction,
        IReadOnlyList<TransactionSplitEntry> splitEntries)
    {
        return new TransactionDetails(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount.Amount,
            transaction.Description,
            transaction.Merchant,
            transaction.OccurredAt,
            transaction.Category,
            transaction.EnvelopeId,
            transaction.TransferId,
            transaction.TransferCounterpartyEnvelopeId,
            transaction.TransferDirection,
            splitEntries.Select(static split => new TransactionSplitDetails(
                    split.Id,
                    split.TransactionId,
                    split.EnvelopeId,
                    split.Amount.Amount,
                    split.Category,
                    split.Notes))
                .ToArray(),
            transaction.DeletedAtUtc,
            transaction.DeletedByUserId);
    }
}
