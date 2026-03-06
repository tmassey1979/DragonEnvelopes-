namespace DragonEnvelopes.Application.Services;

public interface IStripeGateway
{
    Task<string> CreateCustomerAsync(
        Guid familyId,
        string email,
        string? name,
        CancellationToken cancellationToken = default);

    Task<(string SetupIntentId, string ClientSecret)> CreateSetupIntentAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    Task<string> CreateFinancialAccountAsync(
        string customerId,
        Guid familyId,
        Guid envelopeId,
        string displayName,
        CancellationToken cancellationToken = default);
}
