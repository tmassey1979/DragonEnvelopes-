using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class BudgetsViewModel : ObservableObject
{
    private readonly IBudgetsDataService _budgetsDataService;

    public BudgetsViewModel(IBudgetsDataService budgetsDataService)
    {
        _budgetsDataService = budgetsDataService;
        SelectedMonth = DateTime.UtcNow.ToString("yyyy-MM");

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadAsync);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand ApplyFiltersCommand { get; }

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
    private ObservableCollection<BudgetAllocationEnvelopeViewModel> envelopeAllocations = [];

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var workspace = await _budgetsDataService.GetWorkspaceAsync(SelectedMonth, IncludeArchived, cancellationToken);
            if (workspace is null)
            {
                ResetSummary();
                EnvelopeAllocations.Clear();
                IsEmpty = true;
                return;
            }

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
                        envelope.EnvelopeName,
                        FormatCurrency(envelope.MonthlyBudget),
                        FormatCurrency(envelope.CurrentBalance),
                        $"{budgetPercent:0.0}%",
                        $"{balancePercent:0.0}%",
                        envelope.IsArchived);
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
}
