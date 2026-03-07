using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class EnvelopesViewModel : ObservableObject
{
    private static readonly string[] GoalStatuses = ["Active", "Completed", "Cancelled"];
    private readonly IEnvelopesDataService _envelopesDataService;
    private bool _isCreatingNew;
    private bool _isApplyingSelection;

    public EnvelopesViewModel(IEnvelopesDataService envelopesDataService)
    {
        _envelopesDataService = envelopesDataService;

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        BeginCreateCommand = new RelayCommand(BeginCreate);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ArchiveCommand = new AsyncRelayCommand(ArchiveAsync);
        SelectEnvelopeCommand = new RelayCommand<EnvelopeListItemViewModel?>(SelectEnvelope);
        CancelEditCommand = new RelayCommand(CancelEdit);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IRelayCommand BeginCreateCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand ArchiveCommand { get; }
    public IRelayCommand<EnvelopeListItemViewModel?> SelectEnvelopeCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IReadOnlyList<string> GoalStatusOptions { get; } = GoalStatuses;

    [ObservableProperty]
    private ObservableCollection<EnvelopeListItemViewModel> envelopes = [];

    [ObservableProperty]
    private EnvelopeListItemViewModel? selectedEnvelope;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string editorTitle = "Envelope Detail";

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private decimal draftMonthlyBudget;

    [ObservableProperty]
    private bool draftIsArchived;

    [ObservableProperty]
    private string editorMessage = "Select an envelope or create a new one.";

    [ObservableProperty]
    private bool draftHasGoal;

    [ObservableProperty]
    private decimal draftGoalTargetAmount;

    [ObservableProperty]
    private string draftGoalDueDate = string.Empty;

    [ObservableProperty]
    private string draftGoalStatus = GoalStatuses[0];

    [ObservableProperty]
    private string goalEditorMessage = "Goal not configured.";

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        EditorMessage = "Loading envelopes...";

        try
        {
            var items = await _envelopesDataService.GetEnvelopesAsync(cancellationToken);
            Envelopes = new ObservableCollection<EnvelopeListItemViewModel>(items);
            IsEmpty = Envelopes.Count == 0;

            if (SelectedEnvelope is not null)
            {
                var refreshedSelection = Envelopes.FirstOrDefault(x => x.Id == SelectedEnvelope.Id);
                SelectEnvelope(refreshedSelection);
            }
            else if (Envelopes.Count > 0)
            {
                SelectEnvelope(Envelopes[0]);
            }
            else
            {
                EditorTitle = "Envelope Detail";
                EditorMessage = "No envelopes exist yet. Create one to get started.";
                DraftName = string.Empty;
                DraftMonthlyBudget = 0m;
                DraftIsArchived = false;
                DraftHasGoal = false;
                DraftGoalTargetAmount = 0m;
                DraftGoalDueDate = string.Empty;
                DraftGoalStatus = GoalStatuses[0];
                GoalEditorMessage = "Goal not configured.";
                _isCreatingNew = false;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load envelopes: {ex.Message}";
            IsEmpty = true;
            Envelopes.Clear();
            SelectedEnvelope = null;
            EditorMessage = "Unable to load envelopes.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectEnvelope(EnvelopeListItemViewModel? envelope)
    {
        if (_isApplyingSelection)
        {
            return;
        }

        _isApplyingSelection = true;
        SelectedEnvelope = envelope;
        _isApplyingSelection = false;

        foreach (var item in Envelopes)
        {
            item.IsSelected = envelope is not null && item.Id == envelope.Id;
        }

        _isCreatingNew = false;
        if (envelope is null)
        {
            EditorTitle = "Envelope Detail";
            DraftName = string.Empty;
            DraftMonthlyBudget = 0m;
            DraftIsArchived = false;
            DraftHasGoal = false;
            DraftGoalTargetAmount = 0m;
            DraftGoalDueDate = string.Empty;
            DraftGoalStatus = GoalStatuses[0];
            GoalEditorMessage = "Goal not configured.";
            EditorMessage = "Select an envelope from the list.";
            return;
        }

        EditorTitle = "Edit Envelope";
        DraftName = envelope.Name;
        DraftMonthlyBudget = envelope.MonthlyBudget;
        DraftIsArchived = envelope.IsArchived;
        DraftHasGoal = envelope.HasGoal;
        DraftGoalTargetAmount = envelope.GoalTargetAmount ?? 0m;
        DraftGoalDueDate = envelope.GoalDueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        DraftGoalStatus = string.IsNullOrWhiteSpace(envelope.GoalStatus) ? GoalStatuses[0] : envelope.GoalStatus!;
        GoalEditorMessage = envelope.HasGoal
            ? envelope.GoalSummaryDisplay
            : "Goal not configured.";
        EditorMessage = "Update values and save changes.";
    }

    partial void OnSelectedEnvelopeChanged(EnvelopeListItemViewModel? value)
    {
        if (_isApplyingSelection)
        {
            return;
        }

        SelectEnvelope(value);
    }

    private void BeginCreate()
    {
        _isCreatingNew = true;
        foreach (var item in Envelopes)
        {
            item.IsSelected = false;
        }

        SelectedEnvelope = null;
        EditorTitle = "Create Envelope";
        DraftName = string.Empty;
        DraftMonthlyBudget = 0m;
        DraftIsArchived = false;
        DraftHasGoal = false;
        DraftGoalTargetAmount = 0m;
        DraftGoalDueDate = string.Empty;
        DraftGoalStatus = GoalStatuses[0];
        GoalEditorMessage = "Goal not configured.";
        EditorMessage = "Enter a name and monthly budget for the new envelope.";
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftName))
        {
            HasError = true;
            ErrorMessage = "Envelope name is required.";
            return;
        }

        if (DraftMonthlyBudget < 0m)
        {
            HasError = true;
            ErrorMessage = "Monthly budget must be non-negative.";
            return;
        }

        try
        {
            EnvelopeListItemViewModel updated;
            var existingGoalId = _isCreatingNew ? (Guid?)null : SelectedEnvelope?.GoalId;
            if (_isCreatingNew)
            {
                updated = await _envelopesDataService.CreateEnvelopeAsync(
                    DraftName.Trim(),
                    DraftMonthlyBudget,
                    cancellationToken);
                EditorMessage = "Envelope created.";
            }
            else
            {
                if (SelectedEnvelope is null)
                {
                    HasError = true;
                    ErrorMessage = "Select an envelope to save.";
                    return;
                }

                updated = await _envelopesDataService.UpdateEnvelopeAsync(
                    SelectedEnvelope.Id,
                    DraftName.Trim(),
                    DraftMonthlyBudget,
                    DraftIsArchived,
                    cancellationToken);
                EditorMessage = "Envelope updated.";
            }

            await SaveGoalAsync(updated.Id, existingGoalId, cancellationToken);

            await RefreshSelectionAsync(updated.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to save envelope: {ex.Message}";
        }
    }

    private async Task ArchiveAsync(CancellationToken cancellationToken)
    {
        if (SelectedEnvelope is null)
        {
            HasError = true;
            ErrorMessage = "Select an envelope to archive.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var archived = await _envelopesDataService.ArchiveEnvelopeAsync(SelectedEnvelope.Id, cancellationToken);
            EditorMessage = $"Archived '{archived.Name}'.";
            await RefreshSelectionAsync(archived.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to archive envelope: {ex.Message}";
        }
    }

    private async Task RefreshSelectionAsync(Guid envelopeId, CancellationToken cancellationToken)
    {
        var items = await _envelopesDataService.GetEnvelopesAsync(cancellationToken);
        Envelopes = new ObservableCollection<EnvelopeListItemViewModel>(items);
        IsEmpty = Envelopes.Count == 0;

        var envelope = Envelopes.FirstOrDefault(x => x.Id == envelopeId) ?? Envelopes.FirstOrDefault();
        SelectEnvelope(envelope);
    }

    private void CancelEdit()
    {
        if (_isCreatingNew)
        {
            if (Envelopes.Count > 0)
            {
                SelectEnvelope(Envelopes[0]);
            }
            else
            {
                DraftName = string.Empty;
                DraftMonthlyBudget = 0m;
                DraftIsArchived = false;
                EditorMessage = "Create canceled.";
                _isCreatingNew = false;
            }

            return;
        }

        if (SelectedEnvelope is not null)
        {
            DraftName = SelectedEnvelope.Name;
            DraftMonthlyBudget = SelectedEnvelope.MonthlyBudget;
            DraftIsArchived = SelectedEnvelope.IsArchived;
            DraftHasGoal = SelectedEnvelope.HasGoal;
            DraftGoalTargetAmount = SelectedEnvelope.GoalTargetAmount ?? 0m;
            DraftGoalDueDate = SelectedEnvelope.GoalDueDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            DraftGoalStatus = string.IsNullOrWhiteSpace(SelectedEnvelope.GoalStatus) ? GoalStatuses[0] : SelectedEnvelope.GoalStatus!;
            GoalEditorMessage = SelectedEnvelope.GoalSummaryDisplay;
            EditorMessage = "Changes reverted.";
        }
    }

    private async Task SaveGoalAsync(Guid envelopeId, Guid? existingGoalId, CancellationToken cancellationToken)
    {
        if (!DraftHasGoal)
        {
            if (existingGoalId.HasValue)
            {
                await _envelopesDataService.DeleteGoalAsync(existingGoalId.Value, cancellationToken);
                GoalEditorMessage = "Goal removed.";
            }
            else
            {
                GoalEditorMessage = "Goal not configured.";
            }

            return;
        }

        if (DraftGoalTargetAmount <= 0m)
        {
            throw new InvalidOperationException("Goal target amount must be greater than zero.");
        }

        if (!DateOnly.TryParse(DraftGoalDueDate, out var dueDate))
        {
            throw new InvalidOperationException("Goal due date is invalid. Use yyyy-MM-dd format.");
        }

        if (!GoalStatuses.Contains(DraftGoalStatus, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Goal status is invalid.");
        }

        if (existingGoalId.HasValue)
        {
            await _envelopesDataService.UpdateGoalAsync(
                existingGoalId.Value,
                DraftGoalTargetAmount,
                dueDate,
                DraftGoalStatus,
                cancellationToken);
            GoalEditorMessage = "Goal updated.";
            return;
        }

        await _envelopesDataService.CreateGoalAsync(
            envelopeId,
            DraftGoalTargetAmount,
            dueDate,
            DraftGoalStatus,
            cancellationToken);
        GoalEditorMessage = "Goal created.";
    }
}
