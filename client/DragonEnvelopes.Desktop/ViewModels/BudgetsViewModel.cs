using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class BudgetsViewModel : ObservableObject
{
    private readonly IBudgetsDataService _budgetsDataService;
    private Guid? _activeBudgetId;

    public BudgetsViewModel(IBudgetsDataService budgetsDataService)
    {
        _budgetsDataService = budgetsDataService;
        SelectedMonth = DateTime.UtcNow.ToString("yyyy-MM");

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadAsync);
        SaveBudgetCommand = new AsyncRelayCommand(SaveBudgetAsync);
        SaveEnvelopeRolloverPolicyCommand = new AsyncRelayCommand<BudgetAllocationEnvelopeViewModel>(SaveEnvelopeRolloverPolicyAsync);
        PreviewMonthEndRolloverCommand = new AsyncRelayCommand(PreviewMonthEndRolloverAsync);
        ApplyMonthEndRolloverCommand = new AsyncRelayCommand(ApplyMonthEndRolloverAsync);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IAsyncRelayCommand SaveBudgetCommand { get; }
    public IAsyncRelayCommand<BudgetAllocationEnvelopeViewModel> SaveEnvelopeRolloverPolicyCommand { get; }
    public IAsyncRelayCommand PreviewMonthEndRolloverCommand { get; }
    public IAsyncRelayCommand ApplyMonthEndRolloverCommand { get; }

    [ObservableProperty]
    private string selectedMonth = string.Empty;

    [ObservableProperty]
    private bool includeArchived;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string totalIncomeDisplay = "$0.00";

    [ObservableProperty]
    private string allocatedDisplay = "$0.00";

    [ObservableProperty]
    private string remainingDisplay = "$0.00";

    [ObservableProperty]
    private string allocationPercentDisplay = "0.0%";

    [ObservableProperty]
    private string monthDisplay = "--";

    [ObservableProperty]
    private decimal draftTotalIncome;

    [ObservableProperty]
    private string budgetEditorMessage = "Create monthly budget total.";

    [ObservableProperty]
    private string budgetActionLabel = "Create Budget";

    [ObservableProperty]
    private ObservableCollection<BudgetAllocationEnvelopeViewModel> envelopeAllocations = [];

    [ObservableProperty]
    private ObservableCollection<string> rolloverModes = ["None", "Full", "Cap"];

    [ObservableProperty]
    private string rolloverPreviewSummary = "Month-end rollover preview not loaded.";

    [ObservableProperty]
    private string rolloverApplySummary = "Month-end rollover has not been applied.";

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var budget = await _budgetsDataService.GetBudgetAsync(SelectedMonth, cancellationToken);
            _activeBudgetId = budget?.Id;
            DraftTotalIncome = budget?.TotalIncome ?? 0m;
            BudgetActionLabel = budget is null ? "Create Budget" : "Update Budget";
            BudgetEditorMessage = budget is null
                ? "No budget exists for this month. Create one to enable full budget tracking."
                : $"Editing budget for {budget.Month}.";

            var workspace = await _budgetsDataService.GetWorkspaceAsync(SelectedMonth, IncludeArchived, cancellationToken);

            var allocationPercent = workspace.TotalIncome == 0m
                ? 0m
                : decimal.Round((workspace.AllocatedAmount / workspace.TotalIncome) * 100m, 1, MidpointRounding.AwayFromZero);

            MonthDisplay = workspace.Month;
            TotalIncomeDisplay = FormatCurrency(workspace.TotalIncome);
            AllocatedDisplay = FormatCurrency(workspace.AllocatedAmount);
            RemainingDisplay = FormatCurrency(workspace.RemainingAmount);
            AllocationPercentDisplay = $"{allocationPercent:0.0}%";

            EnvelopeAllocations = new ObservableCollection<BudgetAllocationEnvelopeViewModel>(
                workspace.Envelopes.Select(envelope =>
                {
                    var budgetPercent = workspace.TotalIncome == 0m
                        ? 0m
                        : decimal.Round((envelope.MonthlyBudget / workspace.TotalIncome) * 100m, 1, MidpointRounding.AwayFromZero);
                    var balancePercent = envelope.MonthlyBudget == 0m
                        ? 0m
                        : decimal.Round((envelope.CurrentBalance / envelope.MonthlyBudget) * 100m, 1, MidpointRounding.AwayFromZero);

                    return new BudgetAllocationEnvelopeViewModel(
                        envelope.EnvelopeId,
                        envelope.EnvelopeName,
                        FormatCurrency(envelope.MonthlyBudget),
                        FormatCurrency(envelope.CurrentBalance),
                        $"{budgetPercent:0.0}%",
                        $"{balancePercent:0.0}%",
                        envelope.IsArchived,
                        envelope.RolloverMode,
                        envelope.RolloverCap);
                }));

            IsEmpty = EnvelopeAllocations.Count == 0;
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Budget visualization load canceled.";
            ResetSummary();
            EnvelopeAllocations.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Budget visualization load failed: {ex.Message}";
            ResetSummary();
            EnvelopeAllocations.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ResetSummary()
    {
        MonthDisplay = "--";
        TotalIncomeDisplay = "$0.00";
        AllocatedDisplay = "$0.00";
        RemainingDisplay = "$0.00";
        AllocationPercentDisplay = "0.0%";
    }

    private static string FormatCurrency(decimal amount)
    {
        return amount.ToString("$#,##0.00");
    }

    private async Task SaveBudgetAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SelectedMonth) || !System.Text.RegularExpressions.Regex.IsMatch(SelectedMonth, @"^\d{4}-(0[1-9]|1[0-2])$"))
        {
            HasError = true;
            ErrorMessage = "Month must be in yyyy-MM format.";
            return;
        }

        if (DraftTotalIncome < 0m)
        {
            HasError = true;
            ErrorMessage = "Total income cannot be negative.";
            return;
        }

        try
        {
            if (_activeBudgetId.HasValue)
            {
                await _budgetsDataService.UpdateBudgetAsync(_activeBudgetId.Value, DraftTotalIncome, cancellationToken);
                BudgetEditorMessage = "Budget updated.";
            }
            else
            {
                var created = await _budgetsDataService.CreateBudgetAsync(SelectedMonth, DraftTotalIncome, cancellationToken);
                _activeBudgetId = created.Id;
                BudgetEditorMessage = "Budget created.";
            }

            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save budget: {ex.Message}";
        }
    }

    private async Task SaveEnvelopeRolloverPolicyAsync(
        BudgetAllocationEnvelopeViewModel? envelope,
        CancellationToken cancellationToken)
    {
        if (envelope is null)
        {
            HasError = true;
            ErrorMessage = "Select an envelope row to update rollover policy.";
            return;
        }

        if (!RolloverModes.Contains(envelope.DraftRolloverMode))
        {
            HasError = true;
            ErrorMessage = "Rollover mode is invalid.";
            return;
        }

        var requiresCap = string.Equals(envelope.DraftRolloverMode, "Cap", StringComparison.OrdinalIgnoreCase);
        if (requiresCap && !envelope.DraftRolloverCap.HasValue)
        {
            HasError = true;
            ErrorMessage = "Rollover cap is required for Cap mode.";
            return;
        }

        if (!requiresCap && envelope.DraftRolloverCap.HasValue)
        {
            envelope.DraftRolloverCap = null;
        }

        if (envelope.DraftRolloverCap is < 0m)
        {
            HasError = true;
            ErrorMessage = "Rollover cap cannot be negative.";
            return;
        }

        try
        {
            await _budgetsDataService.UpdateEnvelopeRolloverPolicyAsync(
                envelope.EnvelopeId,
                envelope.DraftRolloverMode,
                envelope.DraftRolloverCap,
                cancellationToken);

            BudgetEditorMessage = $"Updated rollover policy for '{envelope.Name}'.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to update rollover policy: {ex.Message}";
        }
    }

    private async Task PreviewMonthEndRolloverAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SelectedMonth) || !System.Text.RegularExpressions.Regex.IsMatch(SelectedMonth, @"^\d{4}-(0[1-9]|1[0-2])$"))
        {
            HasError = true;
            ErrorMessage = "Month must be in yyyy-MM format.";
            return;
        }

        try
        {
            var preview = await _budgetsDataService.PreviewEnvelopeRolloverAsync(SelectedMonth, cancellationToken);
            RolloverPreviewSummary =
                $"Preview {preview.Month}: {FormatCurrency(preview.TotalSourceBalance)} -> {FormatCurrency(preview.TotalRolloverBalance)} ({preview.Items.Count} envelopes).";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to preview month-end rollover: {ex.Message}";
        }
    }

    private async Task ApplyMonthEndRolloverAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(SelectedMonth) || !System.Text.RegularExpressions.Regex.IsMatch(SelectedMonth, @"^\d{4}-(0[1-9]|1[0-2])$"))
        {
            HasError = true;
            ErrorMessage = "Month must be in yyyy-MM format.";
            return;
        }

        try
        {
            var result = await _budgetsDataService.ApplyEnvelopeRolloverAsync(SelectedMonth, cancellationToken);
            var applyVerb = result.AlreadyApplied ? "Already applied" : "Applied";
            RolloverApplySummary =
                $"{applyVerb} {result.Month}: {FormatCurrency(result.TotalRolloverBalance)} across {result.EnvelopeCount} envelopes at {result.AppliedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to apply month-end rollover: {ex.Message}";
        }
    }
}
