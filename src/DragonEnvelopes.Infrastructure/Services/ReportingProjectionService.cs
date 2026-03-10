using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DragonEnvelopes.Infrastructure.Services;

public sealed class ReportingProjectionService(
    DragonEnvelopesDbContext dbContext,
    IClock clock,
    ILogger<ReportingProjectionService> logger) : IReportingProjectionService
{
    private const string AppliedStatus = "Applied";
    private const string FailedStatus = "Failed";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] RelevantRoutingKeys =
    [
        IntegrationEventRoutingKeys.PlanningEnvelopeCreatedV1,
        IntegrationEventRoutingKeys.PlanningEnvelopeUpdatedV1,
        IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1,
        IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
        IntegrationEventRoutingKeys.LedgerTransactionUpdatedV1,
        IntegrationEventRoutingKeys.LedgerTransactionDeletedV1,
        IntegrationEventRoutingKeys.LedgerTransactionRestoredV1
    ];

    public Task<ReportingProjectionBatchDetails> ProjectPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return ProjectPendingInternalAsync(
            NormalizeBatchSize(batchSize),
            familyId: null,
            cancellationToken);
    }

    public async Task<ReportingProjectionReplayDetails> ReplayAsync(
        Guid? familyId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var startedAtUtc = clock.UtcNow;
        var normalizedBatchSize = NormalizeBatchSize(batchSize);

        if (familyId.HasValue)
        {
            if (familyId.Value == Guid.Empty)
            {
                throw new InvalidOperationException("Family id cannot be empty.");
            }

            var envelopeRows = await dbContext.ReportEnvelopeBalanceProjections
                .Where(x => x.FamilyId == familyId.Value)
                .ToArrayAsync(cancellationToken);
            var transactionRows = await dbContext.ReportTransactionProjections
                .Where(x => x.FamilyId == familyId.Value)
                .ToArrayAsync(cancellationToken);
            var appliedRows = await dbContext.ReportProjectionAppliedEvents
                .Where(x => x.FamilyId == familyId.Value)
                .ToArrayAsync(cancellationToken);

            dbContext.ReportEnvelopeBalanceProjections.RemoveRange(envelopeRows);
            dbContext.ReportTransactionProjections.RemoveRange(transactionRows);
            dbContext.ReportProjectionAppliedEvents.RemoveRange(appliedRows);
        }
        else
        {
            var envelopeRows = await dbContext.ReportEnvelopeBalanceProjections.ToArrayAsync(cancellationToken);
            var transactionRows = await dbContext.ReportTransactionProjections.ToArrayAsync(cancellationToken);
            var appliedRows = await dbContext.ReportProjectionAppliedEvents.ToArrayAsync(cancellationToken);

            dbContext.ReportEnvelopeBalanceProjections.RemoveRange(envelopeRows);
            dbContext.ReportTransactionProjections.RemoveRange(transactionRows);
            dbContext.ReportProjectionAppliedEvents.RemoveRange(appliedRows);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var replayedCount = 0;
        var appliedCount = 0;
        var failedCount = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = await ProjectPendingInternalAsync(
                normalizedBatchSize,
                familyId,
                cancellationToken);

            if (batch.LoadedCount == 0)
            {
                break;
            }

            replayedCount += batch.LoadedCount;
            appliedCount += batch.AppliedCount;
            failedCount += batch.FailedCount;
        }

        var envelopeProjectionRowCount = await CountEnvelopeRowsAsync(familyId, cancellationToken);
        var transactionProjectionRowCount = await CountTransactionRowsAsync(familyId, cancellationToken);

        return new ReportingProjectionReplayDetails(
            familyId,
            replayedCount,
            appliedCount,
            failedCount,
            envelopeProjectionRowCount,
            transactionProjectionRowCount,
            startedAtUtc,
            clock.UtcNow);
    }

    public async Task<ReportingProjectionStatusDetails> GetStatusAsync(
        Guid? familyId,
        CancellationToken cancellationToken = default)
    {
        var pendingCount = await CountPendingAsync(familyId, cancellationToken);
        var appliedQuery = dbContext.ReportProjectionAppliedEvents.AsNoTracking().AsQueryable();
        if (familyId.HasValue)
        {
            appliedQuery = appliedQuery.Where(x => x.FamilyId == familyId.Value);
        }

        var appliedCount = await appliedQuery
            .Where(x => x.ProcessingStatus == AppliedStatus)
            .CountAsync(cancellationToken);
        var failedCount = await appliedQuery
            .Where(x => x.ProcessingStatus == FailedStatus)
            .CountAsync(cancellationToken);
        var lastAppliedAtUtc = await appliedQuery
            .Where(x => x.ProcessingStatus == AppliedStatus)
            .Select(x => (DateTimeOffset?)x.AppliedAtUtc)
            .MaxAsync(cancellationToken);

        var envelopeProjectionRowCount = await CountEnvelopeRowsAsync(familyId, cancellationToken);
        var transactionProjectionRowCount = await CountTransactionRowsAsync(familyId, cancellationToken);
        var latestEventOccurredAtUtc = await BuildRelevantOutboxQuery(familyId)
            .Select(x => (DateTimeOffset?)x.OccurredAtUtc)
            .MaxAsync(cancellationToken);
        var oldestPendingOccurredAtUtc = pendingCount > 0
            ? await BuildPendingQuery(familyId)
                .Select(x => (DateTimeOffset?)x.OccurredAtUtc)
                .MinAsync(cancellationToken)
            : null;

        decimal? lagSeconds = null;
        if (oldestPendingOccurredAtUtc.HasValue)
        {
            lagSeconds = decimal.Round(
                Math.Max(0m, (decimal)(clock.UtcNow - oldestPendingOccurredAtUtc.Value).TotalSeconds),
                2,
                MidpointRounding.AwayFromZero);
        }
        else if (latestEventOccurredAtUtc.HasValue)
        {
            lagSeconds = 0m;
        }

        return new ReportingProjectionStatusDetails(
            familyId,
            pendingCount,
            appliedCount,
            failedCount,
            envelopeProjectionRowCount,
            transactionProjectionRowCount,
            lastAppliedAtUtc,
            latestEventOccurredAtUtc,
            lagSeconds);
    }

    private async Task<ReportingProjectionBatchDetails> ProjectPendingInternalAsync(
        int batchSize,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        var pendingMessages = await BuildPendingQuery(familyId)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        if (pendingMessages.Length == 0)
        {
            return new ReportingProjectionBatchDetails(
                LoadedCount: 0,
                AppliedCount: 0,
                FailedCount: 0,
                RemainingCount: 0,
                ProcessedAtUtc: clock.UtcNow);
        }

        var appliedCount = 0;
        var failedCount = 0;
        foreach (var message in pendingMessages)
        {
            try
            {
                await ApplyMessageAsync(message, cancellationToken);
                dbContext.ReportProjectionAppliedEvents.Add(
                    new ReportProjectionAppliedEvent(
                        message.Id,
                        message.EventId,
                        message.FamilyId,
                        message.RoutingKey,
                        message.SourceService,
                        message.OccurredAtUtc,
                        clock.UtcNow,
                        AppliedStatus,
                        errorMessage: null));
                appliedCount += 1;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to apply reporting projection for outbox message. OutboxMessageId={OutboxMessageId}, EventId={EventId}, RoutingKey={RoutingKey}",
                    message.Id,
                    message.EventId,
                    message.RoutingKey);

                dbContext.ReportProjectionAppliedEvents.Add(
                    new ReportProjectionAppliedEvent(
                        message.Id,
                        message.EventId,
                        message.FamilyId,
                        message.RoutingKey,
                        message.SourceService,
                        message.OccurredAtUtc,
                        clock.UtcNow,
                        FailedStatus,
                        TrimError(ex.Message)));
                failedCount += 1;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var remainingCount = await CountPendingAsync(familyId, cancellationToken);

        if (pendingMessages.Length > 0)
        {
            logger.LogInformation(
                "Reporting projection batch processed. Loaded={Loaded}, Applied={Applied}, Failed={Failed}, Remaining={Remaining}",
                pendingMessages.Length,
                appliedCount,
                failedCount,
                remainingCount);
        }

        return new ReportingProjectionBatchDetails(
            pendingMessages.Length,
            appliedCount,
            failedCount,
            remainingCount,
            clock.UtcNow);
    }

    private async Task ApplyMessageAsync(
        IntegrationOutboxMessage message,
        CancellationToken cancellationToken)
    {
        switch (message.RoutingKey)
        {
            case IntegrationEventRoutingKeys.PlanningEnvelopeCreatedV1:
            {
                var payload = DeserializePayload<EnvelopeCreatedIntegrationEvent>(message);
                await UpsertEnvelopeProjectionAsync(
                    payload.EnvelopeId,
                    payload.FamilyId,
                    payload.Name,
                    payload.MonthlyBudget,
                    payload.CurrentBalance,
                    payload.IsArchived,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.PlanningEnvelopeUpdatedV1:
            {
                var payload = DeserializePayload<EnvelopeUpdatedIntegrationEvent>(message);
                await UpsertEnvelopeProjectionAsync(
                    payload.EnvelopeId,
                    payload.FamilyId,
                    payload.Name,
                    payload.MonthlyBudget,
                    payload.CurrentBalance,
                    payload.IsArchived,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1:
            {
                var payload = DeserializePayload<EnvelopeArchivedIntegrationEvent>(message);
                await UpsertEnvelopeProjectionAsync(
                    payload.EnvelopeId,
                    payload.FamilyId,
                    payload.Name,
                    monthlyBudget: 0m,
                    currentBalance: payload.CurrentBalance,
                    isArchived: true,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.LedgerTransactionCreatedV1:
            {
                var payload = DeserializePayload<LedgerTransactionCreatedIntegrationEvent>(message);
                await UpsertTransactionProjectionAsync(
                    payload.TransactionId,
                    payload.FamilyId,
                    payload.AccountId,
                    payload.Amount,
                    payload.Category,
                    isDeletedOverride: false,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.LedgerTransactionUpdatedV1:
            {
                var payload = DeserializePayload<TransactionUpdatedIntegrationEvent>(message);
                await UpsertTransactionProjectionAsync(
                    payload.TransactionId,
                    payload.FamilyId,
                    payload.AccountId,
                    payload.Amount,
                    payload.Category,
                    isDeletedOverride: null,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.LedgerTransactionDeletedV1:
            {
                var payload = DeserializePayload<TransactionDeletedIntegrationEvent>(message);
                await UpsertTransactionProjectionAsync(
                    payload.TransactionId,
                    payload.FamilyId,
                    payload.AccountId,
                    payload.Amount,
                    categoryFallback: null,
                    isDeletedOverride: true,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            case IntegrationEventRoutingKeys.LedgerTransactionRestoredV1:
            {
                var payload = DeserializePayload<TransactionRestoredIntegrationEvent>(message);
                await UpsertTransactionProjectionAsync(
                    payload.TransactionId,
                    payload.FamilyId,
                    payload.AccountId,
                    payload.Amount,
                    categoryFallback: null,
                    isDeletedOverride: false,
                    payload.EventId.ToString("D"),
                    payload.OccurredAtUtc,
                    cancellationToken);
                return;
            }
            default:
                throw new InvalidOperationException($"Unsupported reporting projection routing key: {message.RoutingKey}");
        }
    }

    private async Task UpsertEnvelopeProjectionAsync(
        Guid envelopeId,
        Guid familyId,
        string envelopeName,
        decimal monthlyBudget,
        decimal currentBalance,
        bool isArchived,
        string eventId,
        DateTimeOffset eventOccurredAtUtc,
        CancellationToken cancellationToken)
    {
        var projection = await dbContext.ReportEnvelopeBalanceProjections
            .FirstOrDefaultAsync(x => x.EnvelopeId == envelopeId, cancellationToken);

        if (projection is null)
        {
            projection = new ReportEnvelopeBalanceProjection(
                envelopeId,
                familyId,
                envelopeName,
                monthlyBudget,
                currentBalance,
                isArchived,
                eventId,
                eventOccurredAtUtc,
                clock.UtcNow);
            dbContext.ReportEnvelopeBalanceProjections.Add(projection);
            return;
        }

        var resolvedBudget = monthlyBudget;
        if (resolvedBudget == 0m && !isArchived)
        {
            resolvedBudget = projection.MonthlyBudget;
        }

        projection.Apply(
            envelopeName,
            resolvedBudget,
            currentBalance,
            isArchived,
            eventId,
            eventOccurredAtUtc,
            clock.UtcNow);
    }

    private async Task UpsertTransactionProjectionAsync(
        Guid transactionId,
        Guid familyId,
        Guid accountIdFallback,
        decimal amountFallback,
        string? categoryFallback,
        bool? isDeletedOverride,
        string eventId,
        DateTimeOffset eventOccurredAtUtc,
        CancellationToken cancellationToken)
    {
        var transactionSnapshot = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.Id == transactionId)
            .Select(x => new
            {
                x.AccountId,
                Amount = x.Amount.Amount,
                x.Category,
                x.OccurredAt,
                x.TransferId,
                IsDeleted = x.DeletedAtUtc.HasValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        var resolvedAccountId = transactionSnapshot?.AccountId ?? accountIdFallback;
        var resolvedAmount = transactionSnapshot?.Amount ?? amountFallback;
        var resolvedCategory = transactionSnapshot?.Category ?? categoryFallback;
        var resolvedOccurredAt = transactionSnapshot?.OccurredAt ?? eventOccurredAtUtc;
        var resolvedTransferId = transactionSnapshot?.TransferId;
        var resolvedIsDeleted = isDeletedOverride ?? transactionSnapshot?.IsDeleted ?? false;

        var projection = await dbContext.ReportTransactionProjections
            .FirstOrDefaultAsync(x => x.TransactionId == transactionId, cancellationToken);

        if (projection is null)
        {
            projection = new ReportTransactionProjection(
                transactionId,
                familyId,
                resolvedAccountId,
                resolvedAmount,
                resolvedCategory,
                resolvedOccurredAt,
                resolvedTransferId,
                resolvedIsDeleted,
                eventId,
                eventOccurredAtUtc,
                clock.UtcNow);
            dbContext.ReportTransactionProjections.Add(projection);
            return;
        }

        projection.Apply(
            resolvedAccountId,
            resolvedAmount,
            resolvedCategory,
            resolvedOccurredAt,
            resolvedTransferId,
            resolvedIsDeleted,
            eventId,
            eventOccurredAtUtc,
            clock.UtcNow);
    }

    private IQueryable<IntegrationOutboxMessage> BuildPendingQuery(Guid? familyId)
    {
        return BuildRelevantOutboxQuery(familyId)
            .Where(x => !dbContext.ReportProjectionAppliedEvents
                .AsNoTracking()
                .Any(applied => applied.OutboxMessageId == x.Id))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id);
    }

    private IQueryable<IntegrationOutboxMessage> BuildRelevantOutboxQuery(Guid? familyId)
    {
        var query = dbContext.IntegrationOutboxMessages
            .AsNoTracking()
            .Where(x => RelevantRoutingKeys.Contains(x.RoutingKey))
            .Where(x => x.SourceService == IntegrationEventSourceServices.LedgerApi
                        || x.SourceService == IntegrationEventSourceServices.PlanningApi);

        if (familyId.HasValue)
        {
            query = query.Where(x => x.FamilyId == familyId.Value);
        }

        return query;
    }

    private Task<int> CountPendingAsync(Guid? familyId, CancellationToken cancellationToken)
    {
        return BuildPendingQuery(familyId).CountAsync(cancellationToken);
    }

    private Task<int> CountEnvelopeRowsAsync(Guid? familyId, CancellationToken cancellationToken)
    {
        return familyId.HasValue
            ? dbContext.ReportEnvelopeBalanceProjections
                .AsNoTracking()
                .CountAsync(x => x.FamilyId == familyId.Value, cancellationToken)
            : dbContext.ReportEnvelopeBalanceProjections
                .AsNoTracking()
                .CountAsync(cancellationToken);
    }

    private Task<int> CountTransactionRowsAsync(Guid? familyId, CancellationToken cancellationToken)
    {
        return familyId.HasValue
            ? dbContext.ReportTransactionProjections
                .AsNoTracking()
                .CountAsync(x => x.FamilyId == familyId.Value, cancellationToken)
            : dbContext.ReportTransactionProjections
                .AsNoTracking()
                .CountAsync(cancellationToken);
    }

    private static TPayload DeserializePayload<TPayload>(IntegrationOutboxMessage message)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<TPayload>(message.PayloadJson, SerializerOptions);
            if (payload is null)
            {
                throw new InvalidOperationException("Payload deserialized to null.");
            }

            return payload;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not deserialize outbox payload for routing key '{message.RoutingKey}'.",
                ex);
        }
    }

    private static int NormalizeBatchSize(int batchSize)
    {
        return Math.Clamp(batchSize, 1, 2000);
    }

    private static string? TrimError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 1000
            ? normalized
            : normalized[..1000];
    }
}
