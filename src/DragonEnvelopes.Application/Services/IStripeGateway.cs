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
}
