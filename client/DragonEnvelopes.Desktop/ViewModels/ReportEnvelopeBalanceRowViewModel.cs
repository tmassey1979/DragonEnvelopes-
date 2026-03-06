namespace DragonEnvelopes.Desktop.ViewModels;

public sealed record ReportEnvelopeBalanceRowViewModel(
    string EnvelopeName,
    string MonthlyBudget,
    string CurrentBalance,
    bool IsArchived);
