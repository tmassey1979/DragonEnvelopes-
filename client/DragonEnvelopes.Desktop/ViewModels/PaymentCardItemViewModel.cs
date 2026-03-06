namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record PaymentCardItemViewModel(
    Guid Id,
    Guid EnvelopeId,
    string Provider,
    string ProviderCardId,
    string Type,
    string Status,
    string Brand,
    string Last4,
    string CreatedAt,
    string UpdatedAt)
{
    public bool IsPhysical => Type.Equals("Physical", StringComparison.OrdinalIgnoreCase);
}
