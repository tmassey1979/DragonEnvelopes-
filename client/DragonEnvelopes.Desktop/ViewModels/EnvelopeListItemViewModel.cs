using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class EnvelopeListItemViewModel : ObservableObject
{
    public EnvelopeListItemViewModel(
        Guid id,
        string name,
        decimal monthlyBudget,
        decimal currentBalance,
        bool isArchived,
        Guid? goalId = null,
        decimal? goalTargetAmount = null,
        DateOnly? goalDueDate = null,
        string? goalStatus = null,
        decimal? goalProgressPercent = null,
        string? goalProjectionStatus = null,
        string? goalDueStatus = null)
    {
        Id = id;
        Name = name;
        MonthlyBudget = monthlyBudget;
        CurrentBalance = currentBalance;
        IsArchived = isArchived;
        GoalId = goalId;
        GoalTargetAmount = goalTargetAmount;
        GoalDueDate = goalDueDate;
        GoalStatus = goalStatus;
        GoalProgressPercent = goalProgressPercent;
        GoalProjectionStatus = goalProjectionStatus;
        GoalDueStatus = goalDueStatus ?? "NoGoal";
    }

    public Guid Id { get; }

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private decimal monthlyBudget;

    [ObservableProperty]
    private decimal currentBalance;

    [ObservableProperty]
    private bool isArchived;

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private Guid? goalId;

    [ObservableProperty]
    private decimal? goalTargetAmount;

    [ObservableProperty]
    private DateOnly? goalDueDate;

    [ObservableProperty]
    private string? goalStatus;

    [ObservableProperty]
    private decimal? goalProgressPercent;

    [ObservableProperty]
    private string? goalProjectionStatus;

    [ObservableProperty]
    private string goalDueStatus = "NoGoal";

    public string MonthlyBudgetDisplay => MonthlyBudget.ToString("$#,##0.00");

    public string CurrentBalanceDisplay => CurrentBalance.ToString("$#,##0.00");

    public bool HasGoal => GoalId.HasValue;

    public string GoalTargetDisplay => GoalTargetAmount.HasValue
        ? GoalTargetAmount.Value.ToString("$#,##0.00")
        : "-";

    public string GoalDueDateDisplay => GoalDueDate.HasValue
        ? GoalDueDate.Value.ToString("yyyy-MM-dd")
        : "-";

    public string GoalProgressDisplay
    {
        get
        {
            if (!GoalProgressPercent.HasValue)
            {
                return "-";
            }

            var projection = string.IsNullOrWhiteSpace(GoalProjectionStatus)
                ? string.Empty
                : $" ({GoalProjectionStatus})";
            return $"{GoalProgressPercent.Value:0.0}%{projection}";
        }
    }

    public string GoalDueStatusDisplay => GoalDueStatus switch
    {
        "Overdue" => "Overdue",
        "DueSoon" => "Due Soon",
        "OnSchedule" => "On Schedule",
        _ => "-"
    };

    public string GoalSummaryDisplay => HasGoal
        ? $"Goal {GoalTargetDisplay} by {GoalDueDateDisplay} | Status: {GoalStatus ?? "Active"} | Progress: {GoalProgressDisplay} | {GoalDueStatusDisplay}"
        : "Goal not configured.";

    partial void OnGoalIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(HasGoal));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalTargetAmountChanged(decimal? value)
    {
        OnPropertyChanged(nameof(GoalTargetDisplay));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalDueDateChanged(DateOnly? value)
    {
        OnPropertyChanged(nameof(GoalDueDateDisplay));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalStatusChanged(string? value)
    {
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalProgressPercentChanged(decimal? value)
    {
        OnPropertyChanged(nameof(GoalProgressDisplay));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalProjectionStatusChanged(string? value)
    {
        OnPropertyChanged(nameof(GoalProgressDisplay));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }

    partial void OnGoalDueStatusChanged(string value)
    {
        OnPropertyChanged(nameof(GoalDueStatusDisplay));
        OnPropertyChanged(nameof(GoalSummaryDisplay));
    }
}
