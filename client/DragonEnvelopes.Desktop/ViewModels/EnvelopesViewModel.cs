using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class EnvelopesViewModel : ObservableObject
{
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
            EditorMessage = "Select an envelope from the list.";
            return;
        }

        EditorTitle = "Edit Envelope";
        DraftName = envelope.Name;
        DraftMonthlyBudget = envelope.MonthlyBudget;
        DraftIsArchived = envelope.IsArchived;
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
            EditorMessage = "Changes reverted.";
        }
    }
}
