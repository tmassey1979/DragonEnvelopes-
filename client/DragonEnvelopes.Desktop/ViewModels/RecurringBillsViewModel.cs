using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class RecurringBillsViewModel : ObservableObject
{
    private static readonly string[] Frequencies = ["Monthly", "Weekly", "BiWeekly"];
    private static readonly string[] ExecutionResults = ["All", "Posted", "Skipped", "Failed", "AlreadyProcessed"];
    private readonly IRecurringBillsDataService _recurringBillsDataService;
    private readonly IRecurringExecutionCsvExporter _recurringExecutionCsvExporter;
    private Guid? _editingBillId;

    public RecurringBillsViewModel(
        IRecurringBillsDataService recurringBillsDataService,
        IRecurringExecutionCsvExporter recurringExecutionCsvExporter)
    {
        _recurringBillsDataService = recurringBillsDataService;
        _recurringExecutionCsvExporter = recurringExecutionCsvExporter;
        DraftFrequency = Frequencies[0];
        DraftDayOfMonth = 1;
        DraftStartDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        ProjectionFrom = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        ProjectionTo = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)).ToString("yyyy-MM-dd");
        ExecutionFilterResult = ExecutionResults[0];

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveBillCommand = new AsyncRelayCommand(SaveBillAsync);
        DeleteBillCommand = new AsyncRelayCommand(DeleteBillAsync);
        BeginCreateCommand = new RelayCommand(BeginCreate);
        LoadProjectionCommand = new AsyncRelayCommand(LoadProjectionAsync);
        RefreshExecutionHistoryCommand = new AsyncRelayCommand(LoadExecutionHistoryAsync);
        ExportExecutionHistoryCommand = new AsyncRelayCommand(ExportExecutionHistoryAsync);
        RunAutoPostNowCommand = new AsyncRelayCommand(RunAutoPostNowAsync);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveBillCommand { get; }
    public IAsyncRelayCommand DeleteBillCommand { get; }
    public IRelayCommand BeginCreateCommand { get; }
    public IAsyncRelayCommand LoadProjectionCommand { get; }
    public IAsyncRelayCommand RefreshExecutionHistoryCommand { get; }
    public IAsyncRelayCommand ExportExecutionHistoryCommand { get; }
    public IAsyncRelayCommand RunAutoPostNowCommand { get; }
    public IReadOnlyList<string> FrequencyOptions { get; } = Frequencies;
    public IReadOnlyList<string> ExecutionResultOptions { get; } = ExecutionResults;

    [ObservableProperty]
    private ObservableCollection<RecurringBillItemViewModel> bills = [];

    [ObservableProperty]
    private RecurringBillItemViewModel? selectedBill;

    [ObservableProperty]
    private ObservableCollection<RecurringBillProjectionItemViewModel> projectionItems = [];

    [ObservableProperty]
    private ObservableCollection<RecurringBillExecutionItemViewModel> executionItems = [];

    [ObservableProperty]
    private ObservableCollection<RecurringAutoPostExecutionItemViewModel> autoPostRunExecutions = [];

    [ObservableProperty]
    private string projectionFrom = string.Empty;

    [ObservableProperty]
    private string projectionTo = string.Empty;

    [ObservableProperty]
    private string executionSummary = "Select a recurring bill to view execution history.";

    [ObservableProperty]
    private string executionFilterResult = "All";

    [ObservableProperty]
    private string executionFilterFrom = string.Empty;

    [ObservableProperty]
    private string executionFilterTo = string.Empty;

    [ObservableProperty]
    private string autoPostRunSummary = "Manual auto-post has not been run in this session.";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string editorTitle = "Create Recurring Bill";

    [ObservableProperty]
    private string editorMessage = "Define repeating bills and project upcoming due dates.";

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private string draftMerchant = string.Empty;

    [ObservableProperty]
    private decimal draftAmount;

    [ObservableProperty]
    private string draftFrequency = string.Empty;

    [ObservableProperty]
    private int draftDayOfMonth;

    [ObservableProperty]
    private string draftStartDate = string.Empty;

    [ObservableProperty]
    private string draftEndDate = string.Empty;

    [ObservableProperty]
    private bool draftIsActive = true;

    [ObservableProperty]
    private string saveActionLabel = "Create";

    partial void OnSelectedBillChanged(RecurringBillItemViewModel? value)
    {
        if (value is null)
        {
            ExecutionItems.Clear();
            ExecutionSummary = "Select a recurring bill to view execution history.";
            return;
        }

        _editingBillId = value.Id;
        EditorTitle = $"Edit: {value.Name}";
        SaveActionLabel = "Update";
        DraftName = value.Name;
        DraftMerchant = value.Merchant;
        DraftAmount = value.AmountValue;
        DraftFrequency = value.Frequency;
        DraftDayOfMonth = value.DayOfMonth;
        DraftStartDate = value.StartDate;
        DraftEndDate = value.EndDate == "-" ? string.Empty : value.EndDate;
        DraftIsActive = value.IsActive;
        _ = RefreshExecutionHistoryCommand.ExecuteAsync(null);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var selectedBillId = SelectedBill?.Id;
            var bills = await _recurringBillsDataService.GetBillsAsync(cancellationToken);
            Bills = new ObservableCollection<RecurringBillItemViewModel>(bills);
            SelectedBill = selectedBillId.HasValue
                ? Bills.FirstOrDefault(bill => bill.Id == selectedBillId.Value) ?? Bills.FirstOrDefault()
                : Bills.FirstOrDefault();
            IsEmpty = Bills.Count == 0;
            await LoadProjectionAsync(cancellationToken);
            await LoadExecutionHistoryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load recurring bills: {ex.Message}";
            Bills.Clear();
            ProjectionItems.Clear();
            ExecutionItems.Clear();
            ExecutionSummary = "Execution history unavailable.";
            AutoPostRunExecutions.Clear();
            AutoPostRunSummary = "Manual auto-post result unavailable.";
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveBillAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftName) || string.IsNullOrWhiteSpace(DraftMerchant))
        {
            HasError = true;
            ErrorMessage = "Name and merchant are required.";
            return;
        }

        if (DraftAmount <= 0m)
        {
            HasError = true;
            ErrorMessage = "Amount must be greater than zero.";
            return;
        }

        if (!Frequencies.Contains(DraftFrequency, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Frequency is invalid.";
            return;
        }

        if (DraftDayOfMonth is < 1 or > 31)
        {
            HasError = true;
            ErrorMessage = "Day of month must be between 1 and 31.";
            return;
        }

        if (!DateOnly.TryParse(DraftStartDate, out var startDate))
        {
            HasError = true;
            ErrorMessage = "Start date is invalid.";
            return;
        }

        DateOnly? endDate = null;
        if (!string.IsNullOrWhiteSpace(DraftEndDate))
        {
            if (!DateOnly.TryParse(DraftEndDate, out var parsed))
            {
                HasError = true;
                ErrorMessage = "End date is invalid.";
                return;
            }

            endDate = parsed;
        }

        try
        {
            if (_editingBillId.HasValue)
            {
                await _recurringBillsDataService.UpdateBillAsync(
                    _editingBillId.Value,
                    DraftName.Trim(),
                    DraftMerchant.Trim(),
                    DraftAmount,
                    DraftFrequency,
                    DraftDayOfMonth,
                    startDate,
                    endDate,
                    DraftIsActive,
                    cancellationToken);
                EditorMessage = "Recurring bill updated.";
            }
            else
            {
                await _recurringBillsDataService.CreateBillAsync(
                    DraftName.Trim(),
                    DraftMerchant.Trim(),
                    DraftAmount,
                    DraftFrequency,
                    DraftDayOfMonth,
                    startDate,
                    endDate,
                    DraftIsActive,
                    cancellationToken);
                EditorMessage = "Recurring bill created.";
            }

            BeginCreate();
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save recurring bill: {ex.Message}";
        }
    }

    private async Task DeleteBillAsync(CancellationToken cancellationToken)
    {
        if (!_editingBillId.HasValue)
        {
            HasError = true;
            ErrorMessage = "Select a recurring bill to delete.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await _recurringBillsDataService.DeleteBillAsync(_editingBillId.Value, cancellationToken);
            EditorMessage = "Recurring bill deleted.";
            BeginCreate();
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to delete recurring bill: {ex.Message}";
        }
    }

    private async Task LoadProjectionAsync(CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParse(ProjectionFrom, out var from) || !DateOnly.TryParse(ProjectionTo, out var to))
        {
            HasError = true;
            ErrorMessage = "Projection range dates are invalid.";
            ProjectionItems.Clear();
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var projection = await _recurringBillsDataService.GetProjectionAsync(from, to, cancellationToken);
            ProjectionItems = new ObservableCollection<RecurringBillProjectionItemViewModel>(projection);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load projection: {ex.Message}";
            ProjectionItems.Clear();
        }
    }

    private async Task LoadExecutionHistoryAsync(CancellationToken cancellationToken)
    {
        if (SelectedBill is null)
        {
            ExecutionItems.Clear();
            ExecutionSummary = "Select a recurring bill to view execution history.";
            return;
        }

        var fromDate = ParseExecutionDateFilter(ExecutionFilterFrom, "from");
        if (HasError)
        {
            return;
        }

        var toDate = ParseExecutionDateFilter(ExecutionFilterTo, "to");
        if (HasError)
        {
            return;
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            HasError = true;
            ErrorMessage = "Execution filter range is invalid: from date must be earlier than or equal to to date.";
            ExecutionItems.Clear();
            ExecutionSummary = "Execution history unavailable.";
            return;
        }

        try
        {
            HasError = false;
            ErrorMessage = string.Empty;
            var resultFilter = NormalizeExecutionResultFilter();
            var executions = await _recurringBillsDataService.GetExecutionHistoryAsync(
                SelectedBill.Id,
                take: 25,
                result: resultFilter,
                fromDate: fromDate,
                toDate: toDate,
                cancellationToken: cancellationToken);
            ExecutionItems = new ObservableCollection<RecurringBillExecutionItemViewModel>(executions);
            ExecutionSummary = executions.Count == 0
                ? "No execution records found for the selected filters."
                : $"Showing {executions.Count} execution record(s) for current filters.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load execution history: {ex.Message}";
            ExecutionItems.Clear();
            ExecutionSummary = "Execution history unavailable.";
        }
    }

    private async Task ExportExecutionHistoryAsync(CancellationToken cancellationToken)
    {
        if (SelectedBill is null)
        {
            HasError = true;
            ErrorMessage = "Select a recurring bill before exporting execution history.";
            return;
        }

        if (ExecutionItems.Count == 0)
        {
            HasError = true;
            ErrorMessage = "No execution history rows are available to export.";
            return;
        }

        try
        {
            HasError = false;
            ErrorMessage = string.Empty;

            var csv = _recurringExecutionCsvExporter.BuildCsv(ExecutionItems.ToArray());
            var exportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DragonEnvelopes",
                "Exports");
            Directory.CreateDirectory(exportDirectory);

            var fileName = $"recurring-executions-{SelectedBill.Id:N}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
            var filePath = Path.Combine(exportDirectory, fileName);
            await File.WriteAllTextAsync(filePath, csv, cancellationToken);

            ExecutionSummary = $"Exported {ExecutionItems.Count} row(s) to {filePath}.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to export execution history: {ex.Message}";
        }
    }

    private async Task RunAutoPostNowAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var dueDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var result = await _recurringBillsDataService.RunAutoPostAsync(dueDate, cancellationToken);

            AutoPostRunExecutions = new ObservableCollection<RecurringAutoPostExecutionItemViewModel>(result.Executions);
            AutoPostRunSummary =
                $"Run date {result.DueDate:yyyy-MM-dd}: due {result.DueBillCount}, posted {result.PostedCount}, skipped {result.SkippedCount}, failed {result.FailedCount}, already processed {result.AlreadyProcessedCount}.";

            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to run recurring auto-post: {ex.Message}";
            AutoPostRunExecutions.Clear();
            AutoPostRunSummary = "Manual auto-post execution failed.";
        }
    }

    private void BeginCreate()
    {
        _editingBillId = null;
        SelectedBill = null;
        EditorTitle = "Create Recurring Bill";
        SaveActionLabel = "Create";
        DraftName = string.Empty;
        DraftMerchant = string.Empty;
        DraftAmount = 0m;
        DraftFrequency = Frequencies[0];
        DraftDayOfMonth = 1;
        DraftStartDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        DraftEndDate = string.Empty;
        DraftIsActive = true;
    }

    private DateOnly? ParseExecutionDateFilter(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed;
        }

        HasError = true;
        ErrorMessage = $"Execution {label} date filter is invalid. Use yyyy-MM-dd.";
        ExecutionItems.Clear();
        ExecutionSummary = "Execution history unavailable.";
        return null;
    }

    private string? NormalizeExecutionResultFilter()
    {
        if (string.IsNullOrWhiteSpace(ExecutionFilterResult)
            || string.Equals(ExecutionFilterResult, "All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return ExecutionFilterResult.Trim();
    }
}
