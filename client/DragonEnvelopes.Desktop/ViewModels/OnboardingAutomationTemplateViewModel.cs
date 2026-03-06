using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class OnboardingAutomationTemplateViewModel : ObservableObject
{
    public OnboardingAutomationTemplateViewModel(
        string name,
        string ruleType,
        int priority,
        bool isSelectedByDefault,
        bool requiresEnvelopeSelection,
        string conditionsJsonTemplate,
        string actionJsonTemplate,
        string summary)
    {
        Name = name;
        RuleType = ruleType;
        Priority = priority;
        IsSelected = isSelectedByDefault;
        RequiresEnvelopeSelection = requiresEnvelopeSelection;
        ConditionsJsonTemplate = conditionsJsonTemplate;
        ActionJsonTemplate = actionJsonTemplate;
        Summary = summary;
    }

    public string Name { get; }

    public string RuleType { get; }

    public int Priority { get; }

    public bool RequiresEnvelopeSelection { get; }

    public string ConditionsJsonTemplate { get; }

    public string ActionJsonTemplate { get; }

    public string Summary { get; }

    [ObservableProperty]
    private bool isSelected;
}
