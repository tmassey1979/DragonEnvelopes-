namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record RecurringBillProjectionItemResponse(
    Guid RecurringBillId,
    string Name,
    string Merchant,
    decimal Amount,
    DateOnly DueDate);
