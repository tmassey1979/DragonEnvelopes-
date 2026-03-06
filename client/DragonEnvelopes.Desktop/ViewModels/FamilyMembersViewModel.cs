using System.Collections.ObjectModel;
using System.Net.Mail;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class FamilyMembersViewModel : ObservableObject
{
    private static readonly string[] Roles = ["Parent", "Adult", "Teen", "Child"];
    private readonly IFamilyMembersDataService _familyMembersDataService;

    public FamilyMembersViewModel(IFamilyMembersDataService familyMembersDataService)
    {
        _familyMembersDataService = familyMembersDataService;
        DraftRole = Roles[0];

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddMemberCommand = new AsyncRelayCommand(AddMemberAsync);
        ResetDraftCommand = new RelayCommand(ResetDraft);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand AddMemberCommand { get; }
    public IRelayCommand ResetDraftCommand { get; }
    public IReadOnlyList<string> RoleOptions { get; } = Roles;

    [ObservableProperty]
    private ObservableCollection<FamilyMemberItemViewModel> members = [];

    [ObservableProperty]
    private string draftKeycloakUserId = string.Empty;

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private string draftEmail = string.Empty;

    [ObservableProperty]
    private string draftRole = string.Empty;

    [ObservableProperty]
    private string editorMessage = "Add a family member to the active family.";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var members = await _familyMembersDataService.GetMembersAsync(cancellationToken);
            Members = new ObservableCollection<FamilyMemberItemViewModel>(members);
            IsEmpty = Members.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load family members: {ex.Message}";
            Members.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddMemberAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftKeycloakUserId))
        {
            HasError = true;
            ErrorMessage = "Keycloak User Id is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DraftName))
        {
            HasError = true;
            ErrorMessage = "Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DraftEmail))
        {
            HasError = true;
            ErrorMessage = "Email is required.";
            return;
        }

        try
        {
            _ = new MailAddress(DraftEmail.Trim());
        }
        catch
        {
            HasError = true;
            ErrorMessage = "Enter a valid email.";
            return;
        }

        if (!Roles.Contains(DraftRole, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Role is invalid.";
            return;
        }

        try
        {
            var member = await _familyMembersDataService.AddMemberAsync(
                DraftKeycloakUserId.Trim(),
                DraftName.Trim(),
                DraftEmail.Trim(),
                DraftRole,
                cancellationToken);

            EditorMessage = $"Added family member '{member.Name}'.";
            ResetDraft();
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to add family member: {ex.Message}";
        }
    }

    private void ResetDraft()
    {
        DraftKeycloakUserId = string.Empty;
        DraftName = string.Empty;
        DraftEmail = string.Empty;
        DraftRole = Roles[0];
    }
}
