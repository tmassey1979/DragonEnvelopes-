using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class RecurringExecutionCsvExporterTests
{
    [Fact]
    public void BuildCsv_IncludesRequiredColumns_WithIsoTimestamp()
    {
        var exporter = new RecurringExecutionCsvExporter();
        var execution = new RecurringBillExecutionItemViewModel(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            new DateOnly(2026, 3, 1),
            new DateTimeOffset(2026, 3, 1, 13, 45, 30, TimeSpan.Zero),
            "Failed",
            "-",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa:2026-03-01",
            "gateway timeout");

        var csv = exporter.BuildCsv([execution]);
        var lines = csv.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Id,IdempotencyKey,Result,ExecutedAtUtc,DueDate,TransactionId,Notes", lines[0]);
        Assert.Contains(execution.IdempotencyKey, lines[1], StringComparison.Ordinal);
        Assert.Contains(execution.Result, lines[1], StringComparison.Ordinal);
        Assert.Contains(execution.ExecutedAtIso, lines[1], StringComparison.Ordinal);
        Assert.Contains(execution.Notes, lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildCsv_EscapesQuotedAndCommaDelimitedValues()
    {
        var exporter = new RecurringExecutionCsvExporter();
        var execution = new RecurringBillExecutionItemViewModel(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            new DateOnly(2026, 3, 2),
            new DateTimeOffset(2026, 3, 2, 8, 15, 0, TimeSpan.Zero),
            "Failed",
            "-",
            "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:2026-03-02",
            "failed, reason=\"network timeout\"");

        var csv = exporter.BuildCsv([execution]);

        Assert.Contains("\"failed, reason=\"\"network timeout\"\"\"", csv, StringComparison.Ordinal);
    }
}
