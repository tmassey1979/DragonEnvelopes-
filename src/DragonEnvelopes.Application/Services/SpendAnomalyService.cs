using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Application.Services;

public sealed class SpendAnomalyService(
    ISpendAnomalyEventRepository anomalyEventRepository,
    IClock clock,
    IOptions<SpendAnomalyDetectionOptions> optionsAccessor) : ISpendAnomalyService
{
    public async Task DetectAndRecordAsync(
        Guid familyId,
        Guid transactionId,
        Guid accountId,
        string merchant,
        decimal amount,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (transactionId == Guid.Empty)
        {
            throw new DomainValidationException("Transaction id is required.");
        }

        if (accountId == Guid.Empty)
        {
            throw new DomainValidationException("Account id is required.");
        }

        if (amount >= 0m)
        {
            return;
        }

        if (await anomalyEventRepository.ExistsForTransactionAsync(transactionId, cancellationToken))
        {
            return;
        }

        var options = optionsAccessor.Value;
        var lookbackDays = Math.Max(1, options.LookbackDays);
        var sampleLimit = Math.Max(10, options.HistorySampleLimit);
        var minimumMerchantSamples = Math.Max(1, options.MinimumMerchantSamples);
        var minimumFamilySamples = Math.Max(1, options.MinimumFamilySamples);
        var merchantZScoreThreshold = Math.Max(0.1m, options.MerchantDeviationZScoreThreshold);
        var familyRatioThreshold = Math.Max(1m, options.FamilyDeviationRatioThreshold);
        var minimumAbsoluteAmount = Math.Max(0m, options.MinimumAbsoluteAmount);
        var minimumStandardDeviation = Math.Max(0.01m, options.MinimumStandardDeviation);
        var spendAmount = decimal.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);
        if (spendAmount < minimumAbsoluteAmount)
        {
            return;
        }

        var normalizedMerchant = NormalizeMerchant(merchant);
        var samples = await anomalyEventRepository.ListRecentSpendSamplesAsync(
            familyId,
            occurredAt.AddDays(-lookbackDays),
            transactionId,
            sampleLimit,
            cancellationToken);
        if (samples.Count == 0)
        {
            return;
        }

        var merchantAmounts = samples
            .Where(sample => sample.Merchant.Equals(normalizedMerchant, StringComparison.OrdinalIgnoreCase))
            .Select(static sample => Math.Abs(sample.Amount))
            .ToArray();

        decimal baselineAverage;
        decimal baselineStandardDeviation;
        int baselineSampleSize;
        decimal deviationRatio;
        int severityScore;
        string reason;

        if (merchantAmounts.Length >= minimumMerchantSamples)
        {
            baselineSampleSize = merchantAmounts.Length;
            baselineAverage = CalculateAverage(merchantAmounts);
            baselineStandardDeviation = Math.Max(
                minimumStandardDeviation,
                CalculateStandardDeviation(merchantAmounts, baselineAverage));
            var zScore = (spendAmount - baselineAverage) / baselineStandardDeviation;
            if (zScore < merchantZScoreThreshold)
            {
                return;
            }

            deviationRatio = baselineAverage <= 0m ? spendAmount : spendAmount / baselineAverage;
            severityScore = CalculateMerchantSeverityScore(zScore, deviationRatio, merchantZScoreThreshold);
            reason = $"{normalizedMerchant} spend exceeded merchant baseline by {decimal.Round(zScore, 2, MidpointRounding.AwayFromZero)} standard deviations.";
        }
        else
        {
            var familyAmounts = samples
                .Select(static sample => Math.Abs(sample.Amount))
                .ToArray();
            if (familyAmounts.Length < minimumFamilySamples)
            {
                return;
            }

            baselineSampleSize = familyAmounts.Length;
            baselineAverage = CalculateAverage(familyAmounts);
            baselineStandardDeviation = Math.Max(
                minimumStandardDeviation,
                CalculateStandardDeviation(familyAmounts, baselineAverage));
            deviationRatio = baselineAverage <= 0m ? spendAmount : spendAmount / baselineAverage;
            if (deviationRatio < familyRatioThreshold)
            {
                return;
            }

            severityScore = CalculateFamilySeverityScore(deviationRatio, familyRatioThreshold);
            reason = $"Spend amount was {decimal.Round(deviationRatio, 2, MidpointRounding.AwayFromZero)}x above family baseline average.";
        }

        var anomalyEvent = new SpendAnomalyEvent(
            Guid.NewGuid(),
            familyId,
            transactionId,
            accountId,
            normalizedMerchant,
            spendAmount,
            decimal.Round(baselineAverage, 2, MidpointRounding.AwayFromZero),
            decimal.Round(baselineStandardDeviation, 2, MidpointRounding.AwayFromZero),
            baselineSampleSize,
            decimal.Round(deviationRatio, 4, MidpointRounding.AwayFromZero),
            severityScore,
            reason,
            clock.UtcNow);

        await anomalyEventRepository.AddAsync(anomalyEvent, cancellationToken);
        await anomalyEventRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SpendAnomalyEventDetails>> ListByFamilyAsync(
        Guid familyId,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        var options = optionsAccessor.Value;
        var maxTake = Math.Max(1, options.MaxListTake);
        var normalizedTake = Math.Clamp(take <= 0 ? 50 : take, 1, maxTake);

        var events = await anomalyEventRepository.ListByFamilyAsync(
            familyId,
            normalizedTake,
            cancellationToken);
        return events.Select(Map).ToArray();
    }

    private static SpendAnomalyEventDetails Map(SpendAnomalyEvent anomalyEvent)
    {
        return new SpendAnomalyEventDetails(
            anomalyEvent.Id,
            anomalyEvent.FamilyId,
            anomalyEvent.TransactionId,
            anomalyEvent.AccountId,
            anomalyEvent.Merchant,
            anomalyEvent.Amount,
            anomalyEvent.BaselineAverageAmount,
            anomalyEvent.BaselineStandardDeviation,
            anomalyEvent.BaselineSampleSize,
            anomalyEvent.DeviationRatio,
            anomalyEvent.SeverityScore,
            anomalyEvent.Reason,
            anomalyEvent.DetectedAtUtc);
    }

    private static string NormalizeMerchant(string merchant)
    {
        if (string.IsNullOrWhiteSpace(merchant))
        {
            return "Unknown Merchant";
        }

        return merchant.Trim();
    }

    private static decimal CalculateAverage(IReadOnlyCollection<decimal> values)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        return values.Sum() / values.Count;
    }

    private static decimal CalculateStandardDeviation(
        IReadOnlyCollection<decimal> values,
        decimal mean)
    {
        if (values.Count == 0)
        {
            return 0m;
        }

        var variance = values
            .Select(value =>
            {
                var delta = value - mean;
                return delta * delta;
            })
            .Average();

        return (decimal)Math.Sqrt((double)variance);
    }

    private static int CalculateMerchantSeverityScore(
        decimal zScore,
        decimal deviationRatio,
        decimal threshold)
    {
        var normalizedZ = zScore <= threshold ? 0m : (zScore - threshold) / Math.Max(threshold, 0.1m);
        var ratioBoost = Math.Max(0m, deviationRatio - 1m);
        var rawScore = 55m + (normalizedZ * 18m) + (ratioBoost * 10m);
        return Math.Clamp((int)Math.Round(rawScore, MidpointRounding.AwayFromZero), 1, 100);
    }

    private static int CalculateFamilySeverityScore(
        decimal deviationRatio,
        decimal threshold)
    {
        var normalizedRatio = deviationRatio <= threshold ? 0m : (deviationRatio - threshold) / Math.Max(threshold, 1m);
        var rawScore = 45m + (normalizedRatio * 40m);
        return Math.Clamp((int)Math.Round(rawScore, MidpointRounding.AwayFromZero), 1, 100);
    }
}
