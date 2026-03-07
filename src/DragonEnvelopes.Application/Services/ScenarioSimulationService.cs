using System.Globalization;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;

namespace DragonEnvelopes.Application.Services;

public sealed class ScenarioSimulationService(IClock clock) : IScenarioSimulationService
{
    public Task<ScenarioSimulationDetails> SimulateAsync(
        Guid familyId,
        decimal monthlyIncome,
        decimal fixedExpenses,
        decimal? discretionaryCutPercent,
        int monthHorizon,
        decimal startingBalance,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("FamilyId is required.");
        }

        if (monthlyIncome < 0m)
        {
            throw new DomainValidationException("MonthlyIncome must be non-negative.");
        }

        if (fixedExpenses < 0m)
        {
            throw new DomainValidationException("FixedExpenses must be non-negative.");
        }

        if (monthHorizon is < 1 or > 120)
        {
            throw new DomainValidationException("MonthHorizon must be between 1 and 120.");
        }

        if (discretionaryCutPercent is < 0m or > 100m)
        {
            throw new DomainValidationException("DiscretionaryCutPercent must be between 0 and 100.");
        }

        var cutPercent = discretionaryCutPercent ?? 0m;
        var cutMultiplier = 1m - (cutPercent / 100m);
        var effectiveExpenses = RoundCurrency(fixedExpenses * cutMultiplier);
        var netMonthlyChange = RoundCurrency(monthlyIncome - effectiveExpenses);
        var monthCursor = new DateTimeOffset(clock.UtcNow.Year, clock.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var projectedBalance = RoundCurrency(startingBalance);
        var depletionMonth = default(int?);
        var months = new List<ScenarioSimulationMonthDetails>(monthHorizon);

        for (var index = 1; index <= monthHorizon; index++)
        {
            projectedBalance = RoundCurrency(projectedBalance + netMonthlyChange);
            if (!depletionMonth.HasValue && projectedBalance < 0m)
            {
                depletionMonth = index;
            }

            months.Add(new ScenarioSimulationMonthDetails(
                index,
                monthCursor.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                projectedBalance,
                monthlyIncome,
                effectiveExpenses));

            monthCursor = monthCursor.AddMonths(1);
        }

        return Task.FromResult(new ScenarioSimulationDetails(
            familyId,
            RoundCurrency(startingBalance),
            monthlyIncome,
            fixedExpenses,
            effectiveExpenses,
            netMonthlyChange,
            monthHorizon,
            depletionMonth,
            projectedBalance,
            months));
    }

    private static decimal RoundCurrency(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
