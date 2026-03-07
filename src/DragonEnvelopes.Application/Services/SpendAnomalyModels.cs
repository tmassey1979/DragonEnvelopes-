namespace DragonEnvelopes.Application.Services;

public sealed class SpendAnomalyDetectionOptions
{
    public int LookbackDays { get; init; } = 90;

    public int HistorySampleLimit { get; init; } = 500;

    public int MinimumMerchantSamples { get; init; } = 3;

    public int MinimumFamilySamples { get; init; } = 10;

    public decimal MerchantDeviationZScoreThreshold { get; init; } = 2.5m;

    public decimal FamilyDeviationRatioThreshold { get; init; } = 3.0m;

    public decimal MinimumAbsoluteAmount { get; init; } = 50m;

    public decimal MinimumStandardDeviation { get; init; } = 5m;

    public int MaxListTake { get; init; } = 200;
}
