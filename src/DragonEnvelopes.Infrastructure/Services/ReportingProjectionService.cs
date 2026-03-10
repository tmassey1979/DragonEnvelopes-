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
    private const string ReplayRunningStatus = "Running";
    private const int DefaultReplayBatchSize = 500;
    private const int DefaultReplayMaxEvents = 50_000;
    private const int MaxReplayBatchSize = 2_000;
    private const int MaxReplayMaxEvents = 200_000;
    private const int MaxReplayThrottleMilliseconds = 5_000;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] EnvelopeProjectionRoutingKeys =
    [
        IntegrationEventRoutingKeys.PlanningEnvelopeCreatedV1,
        IntegrationEventRoutingKeys.PlanningEnvelopeUpdatedV1,
        IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1
    ];

    private static readonly string[] TransactionProjectionRoutingKeys =
    [
        IntegrationEventRoutingKeys.LedgerTransactionCreatedV1,
        IntegrationEventRoutingKeys.LedgerTransactionUpdatedV1,
        IntegrationEventRoutingKeys.LedgerTransactionDeletedV1,
        IntegrationEventRoutingKeys.LedgerTransactionRestoredV1
    ];

    private static readonly string[] RelevantRoutingKeys = EnvelopeProjectionRoutingKeys
        .Concat(TransactionProjectionRoutingKeys)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

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
        ReportingProjectionReplayRequestDetails request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeReplayRequest(request);
        var startedAtUtc = clock.UtcNow;

        var targetQuery = BuildReplayTargetQuery(normalizedRequest)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id);
        var targetedEventCount = await targetQuery.CountAsync(cancellationToken);
        var wasCappedByMaxEvents = targetedEventCount > normalizedRequest.MaxEvents;
        var targetOutboxIds = await targetQuery
            .Select(x => x.Id)
            .Take(normalizedRequest.MaxEvents)
            .ToArrayAsync(cancellationToken);

        var replayRun = new ReportProjectionReplayRun(
            Guid.NewGuid(),
            normalizedRequest.FamilyId,
            normalizedRequest.ProjectionSet,
            normalizedRequest.FromOccurredAtUtc,
            normalizedRequest.ToOccurredAtUtc,
            normalizedRequest.IsDryRun,
            normalizedRequest.ResetState,
            normalizedRequest.BatchSize,
            normalizedRequest.MaxEvents,
            normalizedRequest.ThrottleMilliseconds,
            targetOutboxIds.Length,
            wasCappedByMaxEvents,
            ReplayRunningStatus,
            normalizedRequest.RequestedByUserId,
            errorMessage: null,
            startedAtUtc,
            completedAtUtc: startedAtUtc);
        dbContext.ReportProjectionReplayRuns.Add(replayRun);
        await dbContext.SaveChangesAsync(cancellationToken);

        var processedCount = 0;
        var appliedCount = 0;
        var failedCount = 0;
        var batchesProcessed = 0;

        try
        {
            if (!normalizedRequest.IsDryRun && targetOutboxIds.Length > 0)
            {
                if (normalizedRequest.ResetState)
                {
                    await ResetReplayScopeAsync(
                        targetOutboxIds,
                        normalizedRequest.IncludeEnvelopeProjection,
                        normalizedRequest.IncludeTransactionProjection,
                        cancellationToken);
                }

                for (var offset = 0; offset < targetOutboxIds.Length; offset += normalizedRequest.BatchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchIds = targetOutboxIds
                        .Skip(offset)
                        .Take(normalizedRequest.BatchSize)
                        .ToArray();
                    if (batchIds.Length == 0)
                    {
                        continue;
                    }

                    var existingAppliedRows = await dbContext.ReportProjectionAppliedEvents
                        .Where(x => batchIds.Contains(x.OutboxMessageId))
                        .ToArrayAsync(cancellationToken);
                    if (existingAppliedRows.Length > 0)
                    {
                        dbContext.ReportProjectionAppliedEvents.RemoveRange(existingAppliedRows);
                    }

                    var messages = await dbContext.IntegrationOutboxMessages
                        .AsNoTracking()
                        .Where(x => batchIds.Contains(x.Id))
                        .OrderBy(x => x.CreatedAtUtc)
                        .ThenBy(x => x.Id)
                        .ToArrayAsync(cancellationToken);

                    foreach (var message in messages)
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
                                "Failed to apply reporting projection replay message. ReplayRunId={ReplayRunId}, OutboxMessageId={OutboxMessageId}, EventId={EventId}, RoutingKey={RoutingKey}",
                                replayRun.Id,
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
                    processedCount += messages.Length;
                    batchesProcessed += 1;

                    if (normalizedRequest.ThrottleMilliseconds > 0
                        && processedCount < targetOutboxIds.Length)
                    {
                        await Task.Delay(normalizedRequest.ThrottleMilliseconds, cancellationToken);
                    }
                }
            }

            replayRun.Complete(
                processedCount,
                appliedCount,
                failedCount,
                batchesProcessed,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Reporting projection replay failed. ReplayRunId={ReplayRunId}, FamilyId={FamilyId}, ProjectionSet={ProjectionSet}",
                replayRun.Id,
                replayRun.FamilyId,
                replayRun.ProjectionSet);

            replayRun.Fail(
                ex.Message,
                processedCount,
                appliedCount,
                failedCount,
                batchesProcessed,
                clock.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var envelopeProjectionRowCount = await CountEnvelopeRowsAsync(normalizedRequest.FamilyId, cancellationToken);
        var transactionProjectionRowCount = await CountTransactionRowsAsync(normalizedRequest.FamilyId, cancellationToken);

        return new ReportingProjectionReplayDetails(
            replayRun.Id,
            replayRun.FamilyId,
            replayRun.ProjectionSet,
            replayRun.FromOccurredAtUtc,
            replayRun.ToOccurredAtUtc,
            replayRun.IsDryRun,
            replayRun.ResetState,
            replayRun.BatchSize,
            replayRun.MaxEvents,
            replayRun.ThrottleMilliseconds,
            replayRun.TargetedEventCount,
            replayRun.ProcessedEventCount,
            replayRun.BatchesProcessed,
            replayRun.WasCappedByMaxEvents,
            ReplayedCount: replayRun.ProcessedEventCount,
            replayRun.AppliedCount,
            replayRun.FailedCount,
            envelopeProjectionRowCount,
            transactionProjectionRowCount,
            replayRun.StartedAtUtc,
            replayRun.CompletedAtUtc,
            replayRun.Status,
            replayRun.ErrorMessage);
    }

    public async Task<IReadOnlyList<ReportingProjectionReplayRunDetails>> ListReplayRunsAsync(
        Guid? familyId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var boundedTake = Math.Clamp(take <= 0 ? 20 : take, 1, 200);
        var query = dbContext.ReportProjectionReplayRuns
            .AsNoTracking()
            .AsQueryable();
        if (familyId.HasValue)
        {
            query = query.Where(x => x.FamilyId == familyId.Value);
        }

        var runs = await query
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(boundedTake)
            .ToArrayAsync(cancellationToken);
        return runs.Select(MapReplayRun).ToArray();
    }

    public async Task<ReportingProjectionReplayRunDetails?> GetReplayRunAsync(
        Guid replayRunId,
        CancellationToken cancellationToken = default)
    {
        if (replayRunId == Guid.Empty)
        {
            return null;
        }

        var run = await dbContext.ReportProjectionReplayRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == replayRunId, cancellationToken);
        return run is null
            ? null
            : MapReplayRun(run);
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

    private IQueryable<IntegrationOutboxMessage> BuildReplayTargetQuery(NormalizedReplayRequest request)
    {
        var applicableRoutingKeys = request.ProjectionSet switch
        {
            ReportingProjectionSets.All => RelevantRoutingKeys,
            ReportingProjectionSets.EnvelopeBalances => EnvelopeProjectionRoutingKeys,
            ReportingProjectionSets.Transactions => TransactionProjectionRoutingKeys,
            _ => RelevantRoutingKeys
        };

        var query = BuildRelevantOutboxQuery(request.FamilyId)
            .Where(x => applicableRoutingKeys.Contains(x.RoutingKey));

        if (request.FromOccurredAtUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= request.FromOccurredAtUtc.Value);
        }

        if (request.ToOccurredAtUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= request.ToOccurredAtUtc.Value);
        }

        return query;
    }

    private async Task ResetReplayScopeAsync(
        Guid[] targetOutboxIds,
        bool includeEnvelopeProjection,
        bool includeTransactionProjection,
        CancellationToken cancellationToken)
    {
        if (targetOutboxIds.Length == 0)
        {
            return;
        }

        HashSet<Guid>? envelopeIds = includeEnvelopeProjection ? [] : null;
        HashSet<Guid>? transactionIds = includeTransactionProjection ? [] : null;

        foreach (var outboxChunk in targetOutboxIds.Chunk(1000))
        {
            var batchIds = outboxChunk.ToArray();
            if (batchIds.Length == 0)
            {
                continue;
            }

            var appliedRows = await dbContext.ReportProjectionAppliedEvents
                .Where(x => batchIds.Contains(x.OutboxMessageId))
                .ToArrayAsync(cancellationToken);
            if (appliedRows.Length > 0)
            {
                dbContext.ReportProjectionAppliedEvents.RemoveRange(appliedRows);
            }

            if (!includeEnvelopeProjection && !includeTransactionProjection)
            {
                continue;
            }

            var messages = await dbContext.IntegrationOutboxMessages
                .AsNoTracking()
                .Where(x => batchIds.Contains(x.Id))
                .ToArrayAsync(cancellationToken);

            foreach (var message in messages)
            {
                if (includeEnvelopeProjection && EnvelopeProjectionRoutingKeys.Contains(message.RoutingKey))
                {
                    envelopeIds!.Add(GetEnvelopeId(message));
                }

                if (includeTransactionProjection && TransactionProjectionRoutingKeys.Contains(message.RoutingKey))
                {
                    transactionIds!.Add(GetTransactionId(message));
                }
            }
        }

        if (includeEnvelopeProjection && envelopeIds is not null && envelopeIds.Count > 0)
        {
            foreach (var envelopeChunk in envelopeIds.Chunk(1000))
            {
                var envelopeBatch = envelopeChunk.ToArray();
                var rows = await dbContext.ReportEnvelopeBalanceProjections
                    .Where(x => envelopeBatch.Contains(x.EnvelopeId))
                    .ToArrayAsync(cancellationToken);
                if (rows.Length > 0)
                {
                    dbContext.ReportEnvelopeBalanceProjections.RemoveRange(rows);
                }
            }
        }

        if (includeTransactionProjection && transactionIds is not null && transactionIds.Count > 0)
        {
            foreach (var transactionChunk in transactionIds.Chunk(1000))
            {
                var transactionBatch = transactionChunk.ToArray();
                var rows = await dbContext.ReportTransactionProjections
                    .Where(x => transactionBatch.Contains(x.TransactionId))
                    .ToArrayAsync(cancellationToken);
                if (rows.Length > 0)
                {
                    dbContext.ReportTransactionProjections.RemoveRange(rows);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid GetEnvelopeId(IntegrationOutboxMessage message)
    {
        return message.RoutingKey switch
        {
            IntegrationEventRoutingKeys.PlanningEnvelopeCreatedV1 =>
                DeserializePayload<EnvelopeCreatedIntegrationEvent>(message).EnvelopeId,
            IntegrationEventRoutingKeys.PlanningEnvelopeUpdatedV1 =>
                DeserializePayload<EnvelopeUpdatedIntegrationEvent>(message).EnvelopeId,
            IntegrationEventRoutingKeys.PlanningEnvelopeArchivedV1 =>
                DeserializePayload<EnvelopeArchivedIntegrationEvent>(message).EnvelopeId,
            _ => throw new InvalidOperationException(
                $"Outbox message '{message.Id}' does not contain an envelope projection payload.")
        };
    }

    private static Guid GetTransactionId(IntegrationOutboxMessage message)
    {
        return message.RoutingKey switch
        {
            IntegrationEventRoutingKeys.LedgerTransactionCreatedV1 =>
                DeserializePayload<LedgerTransactionCreatedIntegrationEvent>(message).TransactionId,
            IntegrationEventRoutingKeys.LedgerTransactionUpdatedV1 =>
                DeserializePayload<TransactionUpdatedIntegrationEvent>(message).TransactionId,
            IntegrationEventRoutingKeys.LedgerTransactionDeletedV1 =>
                DeserializePayload<TransactionDeletedIntegrationEvent>(message).TransactionId,
            IntegrationEventRoutingKeys.LedgerTransactionRestoredV1 =>
                DeserializePayload<TransactionRestoredIntegrationEvent>(message).TransactionId,
            _ => throw new InvalidOperationException(
                $"Outbox message '{message.Id}' does not contain a transaction projection payload.")
        };
    }

    private static NormalizedReplayRequest NormalizeReplayRequest(ReportingProjectionReplayRequestDetails request)
    {
        var familyId = request.FamilyId;
        if (familyId.HasValue && familyId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Family id cannot be empty.");
        }

        var projectionSet = string.IsNullOrWhiteSpace(request.ProjectionSet)
            ? ReportingProjectionSets.All
            : request.ProjectionSet.Trim();

        projectionSet = projectionSet switch
        {
            _ when projectionSet.Equals(ReportingProjectionSets.All, StringComparison.OrdinalIgnoreCase) =>
                ReportingProjectionSets.All,
            _ when projectionSet.Equals(ReportingProjectionSets.EnvelopeBalances, StringComparison.OrdinalIgnoreCase) =>
                ReportingProjectionSets.EnvelopeBalances,
            _ when projectionSet.Equals(ReportingProjectionSets.Transactions, StringComparison.OrdinalIgnoreCase) =>
                ReportingProjectionSets.Transactions,
            _ => throw new InvalidOperationException(
                $"Projection set '{projectionSet}' is not supported. Allowed values: {ReportingProjectionSets.All}, {ReportingProjectionSets.EnvelopeBalances}, {ReportingProjectionSets.Transactions}.")
        };

        var fromOccurredAtUtc = request.FromOccurredAtUtc;
        var toOccurredAtUtc = request.ToOccurredAtUtc;
        if (fromOccurredAtUtc.HasValue
            && toOccurredAtUtc.HasValue
            && fromOccurredAtUtc.Value > toOccurredAtUtc.Value)
        {
            throw new InvalidOperationException("FromOccurredAtUtc cannot be greater than ToOccurredAtUtc.");
        }

        var batchSize = request.BatchSize <= 0
            ? DefaultReplayBatchSize
            : Math.Clamp(request.BatchSize, 1, MaxReplayBatchSize);
        var maxEvents = request.MaxEvents <= 0
            ? DefaultReplayMaxEvents
            : Math.Clamp(request.MaxEvents, 1, MaxReplayMaxEvents);
        var throttleMilliseconds = Math.Clamp(request.ThrottleMilliseconds, 0, MaxReplayThrottleMilliseconds);

        var includeEnvelopeProjection = projectionSet is ReportingProjectionSets.All or ReportingProjectionSets.EnvelopeBalances;
        var includeTransactionProjection = projectionSet is ReportingProjectionSets.All or ReportingProjectionSets.Transactions;

        return new NormalizedReplayRequest(
            familyId,
            projectionSet,
            fromOccurredAtUtc,
            toOccurredAtUtc,
            request.IsDryRun,
            request.ResetState,
            batchSize,
            maxEvents,
            throttleMilliseconds,
            TrimToLength(request.RequestedByUserId, 128),
            includeEnvelopeProjection,
            includeTransactionProjection);
    }

    private static ReportingProjectionReplayRunDetails MapReplayRun(ReportProjectionReplayRun run)
    {
        return new ReportingProjectionReplayRunDetails(
            run.Id,
            run.FamilyId,
            run.ProjectionSet,
            run.FromOccurredAtUtc,
            run.ToOccurredAtUtc,
            run.IsDryRun,
            run.ResetState,
            run.BatchSize,
            run.MaxEvents,
            run.ThrottleMilliseconds,
            run.TargetedEventCount,
            run.ProcessedEventCount,
            run.AppliedCount,
            run.FailedCount,
            run.BatchesProcessed,
            run.WasCappedByMaxEvents,
            run.Status,
            run.RequestedByUserId,
            run.ErrorMessage,
            run.StartedAtUtc,
            run.CompletedAtUtc);
    }

    private static string? TrimToLength(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
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

    private sealed record NormalizedReplayRequest(
        Guid? FamilyId,
        string ProjectionSet,
        DateTimeOffset? FromOccurredAtUtc,
        DateTimeOffset? ToOccurredAtUtc,
        bool IsDryRun,
        bool ResetState,
        int BatchSize,
        int MaxEvents,
        int ThrottleMilliseconds,
        string? RequestedByUserId,
        bool IncludeEnvelopeProjection,
        bool IncludeTransactionProjection);
}
