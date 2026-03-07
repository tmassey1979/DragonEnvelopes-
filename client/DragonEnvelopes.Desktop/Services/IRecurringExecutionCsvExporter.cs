using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IRecurringExecutionCsvExporter
{
    string BuildCsv(IReadOnlyList<RecurringBillExecutionItemViewModel> executions);
}
