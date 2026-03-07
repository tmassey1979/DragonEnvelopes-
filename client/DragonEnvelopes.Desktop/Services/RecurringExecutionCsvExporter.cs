using System.Text;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class RecurringExecutionCsvExporter : IRecurringExecutionCsvExporter
{
    public string BuildCsv(IReadOnlyList<RecurringBillExecutionItemViewModel> executions)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id,IdempotencyKey,Result,ExecutedAtUtc,DueDate,TransactionId,Notes");

        foreach (var execution in executions)
        {
            builder
                .Append(Escape(execution.Id.ToString("D"))).Append(',')
                .Append(Escape(execution.IdempotencyKey)).Append(',')
                .Append(Escape(execution.Result)).Append(',')
                .Append(Escape(execution.ExecutedAtIso)).Append(',')
                .Append(Escape(execution.DueDateDisplay)).Append(',')
                .Append(Escape(execution.TransactionId)).Append(',')
                .Append(Escape(execution.Notes))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var requiresQuoting = value.IndexOfAny([',', '"', '\r', '\n']) >= 0;
        if (!requiresQuoting)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
