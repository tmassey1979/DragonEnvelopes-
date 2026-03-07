using System.Globalization;
using System.Text;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class ScenarioSimulationCsvExporter : IScenarioSimulationCsvExporter
{
    public string BuildCsv(IReadOnlyList<ScenarioSimulationMonthPointViewModel> points)
    {
        var builder = new StringBuilder();
        builder.AppendLine("MonthIndex,Month,Income,Expenses,ProjectedBalance");

        foreach (var point in points.OrderBy(static item => item.MonthIndex))
        {
            builder.Append(point.MonthIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(Escape(point.Month));
            builder.Append(',');
            builder.Append(point.IncomeValue.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.ExpensesValue.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(point.ProjectedBalanceValue.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
