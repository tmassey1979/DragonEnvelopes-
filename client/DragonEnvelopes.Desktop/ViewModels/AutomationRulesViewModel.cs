using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class AutomationRulesViewModel : ObservableObject
{
    private static readonly string[] RuleTypes = ["Categorization", "Allocation"];
    private readonly IAutomationRulesDataService _automationRulesDataService;

    public AutomationRulesViewModel(IAutomationRulesDataService automationRulesDataService)
    {
        _automationRulesDataService = automationRulesDataService;
        FilterTypes = ["All", .. RuleTypes];
        FilterEnabledOptions = ["All", "Enabled", "Disabled"];

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        ApplyFiltersCommand = new AsyncRelayCommand(LoadAsync);
        BeginCreateCommand = new RelayCommand(BeginCreate);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ToggleEnabledCommand = new AsyncRelayCommand(ToggleEnabledAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand ApplyFiltersCommand { get; }
    public IRelayCommand BeginCreateCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand ToggleEnabledCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    public IReadOnlyList<string> FilterTypes { get; }
    public IReadOnlyList<string> FilterEnabledOptions { get; }
    public IReadOnlyList<string> RuleTypeOptions { get; } = RuleTypes;

    [ObservableProperty]
    private string selectedFilterType = "All";

    [ObservableProperty]
    private string selectedFilterEnabled = "All";

    [ObservableProperty]
    private ObservableCollection<AutomationRuleListItemViewModel> rules = [];

    [ObservableProperty]
    private AutomationRuleListItemViewModel? selectedRule;

    [ObservableProperty]
    private string editorTitle = "Create Rule";

    [ObservableProperty]
    private string editorStatus = "Create a rule or pick one to edit.";

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private string draftRuleType = RuleTypes[0];

    [ObservableProperty]
    private int draftPriority = 1;

    [ObservableProperty]
    private bool draftIsEnabled = true;

    [ObservableProperty]
    private string draftConditionsJson = "{}";

    [ObservableProperty]
    private string draftActionJson = "{}";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    partial void OnSelectedRuleChanged(AutomationRuleListItemViewModel? value)
    {
        if (value is null)
        {
            EditorTitle = "Create Rule";
            return;
        }

        EditorTitle = $"Edit Rule: {value.Name}";
        DraftName = value.Name;
        DraftRuleType = value.RuleType;
        DraftPriority = value.Priority;
        DraftIsEnabled = value.IsEnabled;
        DraftConditionsJson = value.ConditionsJson;
        DraftActionJson = value.ActionJson;
        EditorStatus = "Loaded selected rule.";
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var rules = await _automationRulesDataService.GetRulesAsync(
                NormalizeTypeFilter(SelectedFilterType),
                NormalizeEnabledFilter(SelectedFilterEnabled),
                cancellationToken);

            Rules = new ObservableCollection<AutomationRuleListItemViewModel>(rules);
            IsEmpty = Rules.Count == 0;

            if (SelectedRule is not null)
            {
                SelectedRule = Rules.FirstOrDefault(rule => rule.Id == SelectedRule.Id);
            }
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Automation rules load canceled.";
            Rules.Clear();
            IsEmpty = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Automation rules load failed: {ex.Message}";
            Rules.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BeginCreate()
    {
        SelectedRule = null;
        DraftName = string.Empty;
        DraftRuleType = RuleTypes[0];
        DraftPriority = 1;
        DraftIsEnabled = true;
        DraftConditionsJson = "{}";
        DraftActionJson = "{}";
        EditorTitle = "Create Rule";
        EditorStatus = "Ready to create a new rule.";
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftName))
        {
            HasError = true;
            ErrorMessage = "Rule name is required.";
            return;
        }

        if (!RuleTypes.Contains(DraftRuleType, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Rule type is invalid.";
            return;
        }

        if (DraftPriority < 1)
        {
            HasError = true;
            ErrorMessage = "Priority must be at least 1.";
            return;
        }

        if (!IsJsonObject(DraftConditionsJson) || !IsJsonObject(DraftActionJson))
        {
            HasError = true;
            ErrorMessage = "Conditions and action must be valid JSON objects.";
            return;
        }

        try
        {
            if (SelectedRule is null)
            {
                var created = await _automationRulesDataService.CreateRuleAsync(
                    DraftName.Trim(),
                    DraftRuleType,
                    DraftPriority,
                    DraftIsEnabled,
                    DraftConditionsJson.Trim(),
                    DraftActionJson.Trim(),
                    cancellationToken);

                EditorStatus = $"Created rule '{created.Name}'.";
            }
            else
            {
                var updated = await _automationRulesDataService.UpdateRuleAsync(
                    SelectedRule.Id,
                    DraftName.Trim(),
                    DraftPriority,
                    DraftIsEnabled,
                    DraftConditionsJson.Trim(),
                    DraftActionJson.Trim(),
                    cancellationToken);

                EditorStatus = $"Updated rule '{updated.Name}'.";
            }

            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Save failed: {ex.Message}";
        }
    }

    private async Task ToggleEnabledAsync(CancellationToken cancellationToken)
    {
        if (SelectedRule is null)
        {
            HasError = true;
            ErrorMessage = "Select a rule first.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var nextEnabled = !SelectedRule.IsEnabled;
            await _automationRulesDataService.SetRuleEnabledAsync(SelectedRule.Id, nextEnabled, cancellationToken);
            EditorStatus = nextEnabled ? "Rule enabled." : "Rule disabled.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Enable/disable failed: {ex.Message}";
        }
    }

    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedRule is null)
        {
            HasError = true;
            ErrorMessage = "Select a rule first.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var name = SelectedRule.Name;
            await _automationRulesDataService.DeleteRuleAsync(SelectedRule.Id, cancellationToken);
            BeginCreate();
            await LoadAsync(cancellationToken);
            EditorStatus = $"Deleted rule '{name}'.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Delete failed: {ex.Message}";
        }
    }

    private static bool IsJsonObject(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? NormalizeTypeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Equals("All", StringComparison.OrdinalIgnoreCase)
            ? null
            : value;
    }

    private static bool? NormalizeEnabledFilter(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return value.Equals("Enabled", StringComparison.OrdinalIgnoreCase);
    }
}
