using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IOnboardingBootstrapService
{
    Task<OnboardingBootstrapDetails> BootstrapAsync(
        Guid familyId,
        IReadOnlyList<(string Name, string Type, decimal OpeningBalance)> accounts,
        IReadOnlyList<(string Name, decimal MonthlyBudget)> envelopes,
        (string Month, decimal TotalIncome)? budget,
        CancellationToken cancellationToken = default);
}
