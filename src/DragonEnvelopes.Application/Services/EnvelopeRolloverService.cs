using System.Text.Json;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeRolloverService(
    IEnvelopeRepository envelopeRepository,
    IEnvelopeRolloverRunRepository rolloverRunRepository,
    IClock clock) : IEnvelopeRolloverService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<EnvelopeRolloverPreviewDetails> PreviewAsync(
        Guid familyId,
        string month,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedMonth = BudgetMonth.Parse(month).ToString();
        var envelopes = await envelopeRepository.ListByFamilyAsync(familyId, cancellationToken);
        var items = BuildItems(envelopes);

        return new EnvelopeRolloverPreviewDetails(
            familyId,
            normalizedMonth,
            clock.UtcNow,
            items.Sum(static item => item.CurrentBalance),
            items.Sum(static item => item.RolloverBalance),
            items);
    }

    public async Task<EnvelopeRolloverApplyDetails> ApplyAsync(
        Guid familyId,
        string month,
        string? appliedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedMonth = BudgetMonth.Parse(month).ToString();
        var existingRun = await rolloverRunRepository.GetByFamilyAndMonthAsync(familyId, normalizedMonth, cancellationToken);
        if (existingRun is not null)
        {
            return MapApplied(existingRun, alreadyApplied: true);
        }

        var now = clock.UtcNow;
        var envelopes = await envelopeRepository.ListByFamilyForUpdateAsync(familyId, cancellationToken);
        var activeEnvelopes = envelopes
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static envelope => envelope.Id)
            .ToArray();

        var items = new List<EnvelopeRolloverItemDetails>(activeEnvelopes.Length);
        foreach (var envelope in activeEnvelopes)
        {
            var rolloverBalance = envelope.CalculateRolloverBalance().Amount;
            var currentBalance = envelope.CurrentBalance.Amount;
            var adjustment = Money.FromDecimal(rolloverBalance - currentBalance).Amount;

            envelope.ApplyMonthEndRollover(Money.FromDecimal(rolloverBalance), now);

            items.Add(new EnvelopeRolloverItemDetails(
                envelope.Id,
                envelope.Name,
                currentBalance,
                envelope.RolloverMode.ToString(),
                envelope.RolloverCap?.Amount,
                rolloverBalance,
                adjustment));
        }

        var payload = new EnvelopeRolloverRunPayload(
            items.Select(static item => new EnvelopeRolloverRunPayloadItem(
                item.EnvelopeId,
                item.EnvelopeName,
                item.CurrentBalance,
                item.RolloverMode,
                item.RolloverCap,
                item.RolloverBalance,
                item.AdjustmentAmount))
            .ToArray());

        var run = new EnvelopeRolloverRun(
            Guid.NewGuid(),
            familyId,
            normalizedMonth,
            now,
            appliedByUserId,
            items.Count,
            items.Sum(static item => item.RolloverBalance),
            JsonSerializer.Serialize(payload, SerializerOptions));

        await rolloverRunRepository.AddAsync(run, cancellationToken);

        return new EnvelopeRolloverApplyDetails(
            run.Id,
            run.FamilyId,
            run.Month,
            AlreadyApplied: false,
            run.AppliedAtUtc,
            run.AppliedByUserId,
            run.EnvelopeCount,
            run.TotalRolloverBalance,
            items);
    }

    private static IReadOnlyList<EnvelopeRolloverItemDetails> BuildItems(IReadOnlyList<Envelope> envelopes)
    {
        return envelopes
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static envelope => envelope.Id)
            .Select(static envelope =>
            {
                var rolloverBalance = envelope.CalculateRolloverBalance().Amount;
                var currentBalance = envelope.CurrentBalance.Amount;
                var adjustment = Money.FromDecimal(rolloverBalance - currentBalance).Amount;

                return new EnvelopeRolloverItemDetails(
                    envelope.Id,
                    envelope.Name,
                    currentBalance,
                    envelope.RolloverMode.ToString(),
                    envelope.RolloverCap?.Amount,
                    rolloverBalance,
                    adjustment);
            })
            .ToArray();
    }

    private static EnvelopeRolloverApplyDetails MapApplied(EnvelopeRolloverRun run, bool alreadyApplied)
    {
        EnvelopeRolloverRunPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<EnvelopeRolloverRunPayload>(run.ResultJson, SerializerOptions);
        }
        catch (JsonException)
        {
            payload = null;
        }

        var items = payload?.Items?
            .OrderBy(static item => item.EnvelopeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.EnvelopeId)
            .Select(static item => new EnvelopeRolloverItemDetails(
                item.EnvelopeId,
                item.EnvelopeName,
                item.CurrentBalance,
                item.RolloverMode,
                item.RolloverCap,
                item.RolloverBalance,
                item.AdjustmentAmount))
            .ToArray()
            ?? [];

        return new EnvelopeRolloverApplyDetails(
            run.Id,
            run.FamilyId,
            run.Month,
            alreadyApplied,
            run.AppliedAtUtc,
            run.AppliedByUserId,
            run.EnvelopeCount,
            run.TotalRolloverBalance,
            items);
    }

    private sealed record EnvelopeRolloverRunPayload(IReadOnlyList<EnvelopeRolloverRunPayloadItem> Items);

    private sealed record EnvelopeRolloverRunPayloadItem(
        Guid EnvelopeId,
        string EnvelopeName,
        decimal CurrentBalance,
        string RolloverMode,
        decimal? RolloverCap,
        decimal RolloverBalance,
        decimal AdjustmentAmount);
}
