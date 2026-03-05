namespace DragonEnvelopes.Contracts.RecurringBills;

public sealed record CreateRecurringBillRequest(
    Guid FamilyId,
    string Name,
    string Merchant,
    decimal Amount,
    string Frequency,
    int DayOfMonth,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive);
