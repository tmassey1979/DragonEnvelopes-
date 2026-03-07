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
        SelectedMemberRole = Roles[0];
        DraftInviteRole = Roles[0];
        DraftInviteExpiresInHours = "168";

        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddMemberCommand = new AsyncRelayCommand(AddMemberAsync);
        CreateInviteCommand = new AsyncRelayCommand(CreateInviteAsync);
        CancelInviteCommand = new AsyncRelayCommand(CancelSelectedInviteAsync);
        ResendInviteCommand = new AsyncRelayCommand(ResendSelectedInviteAsync);
        UpdateSelectedMemberRoleCommand = new AsyncRelayCommand(UpdateSelectedMemberRoleAsync);
        RemoveSelectedMemberCommand = new AsyncRelayCommand(RemoveSelectedMemberAsync);
        ResetDraftCommand = new RelayCommand(ResetDraft);

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand AddMemberCommand { get; }
    public IAsyncRelayCommand CreateInviteCommand { get; }
    public IAsyncRelayCommand CancelInviteCommand { get; }
    public IAsyncRelayCommand ResendInviteCommand { get; }
    public IAsyncRelayCommand UpdateSelectedMemberRoleCommand { get; }
    public IAsyncRelayCommand RemoveSelectedMemberCommand { get; }
    public IRelayCommand ResetDraftCommand { get; }
    public IReadOnlyList<string> RoleOptions { get; } = Roles;

    [ObservableProperty]
    private ObservableCollection<FamilyMemberItemViewModel> members = [];

    [ObservableProperty]
    private ObservableCollection<FamilyInviteItemViewModel> invites = [];

    [ObservableProperty]
    private FamilyMemberItemViewModel? selectedMember;

    [ObservableProperty]
    private FamilyInviteItemViewModel? selectedInvite;

    [ObservableProperty]
    private string draftKeycloakUserId = string.Empty;

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private string draftEmail = string.Empty;

    [ObservableProperty]
    private string draftRole = string.Empty;

    [ObservableProperty]
    private string selectedMemberRole = string.Empty;

    [ObservableProperty]
    private string draftInviteEmail = string.Empty;

    [ObservableProperty]
    private string draftInviteRole = string.Empty;

    [ObservableProperty]
    private string draftInviteExpiresInHours = "168";

    [ObservableProperty]
    private string editorMessage = "Add a family member to the active family.";

    [ObservableProperty]
    private string inviteMessage = "Create and manage pending invites for this family.";

    [ObservableProperty]
    private string lastInviteToken = string.Empty;

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
            SelectedMember = Members.FirstOrDefault();
            var invites = await _familyMembersDataService.GetInvitesAsync(cancellationToken);
            Invites = new ObservableCollection<FamilyInviteItemViewModel>(invites);
            SelectedInvite = Invites.FirstOrDefault();
            IsEmpty = Members.Count == 0 && Invites.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load family members: {ex.Message}";
            Members.Clear();
            Invites.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedMemberChanged(FamilyMemberItemViewModel? value)
    {
        SelectedMemberRole = value?.Role ?? Roles[0];
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

    private async Task CreateInviteAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftInviteEmail))
        {
            HasError = true;
            ErrorMessage = "Invite email is required.";
            return;
        }

        try
        {
            _ = new MailAddress(DraftInviteEmail.Trim());
        }
        catch
        {
            HasError = true;
            ErrorMessage = "Enter a valid invite email.";
            return;
        }

        if (!Roles.Contains(DraftInviteRole, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Invite role is invalid.";
            return;
        }

        if (!int.TryParse(DraftInviteExpiresInHours, out var expiresInHours) || expiresInHours < 1 || expiresInHours > 720)
        {
            HasError = true;
            ErrorMessage = "Invite expiration must be between 1 and 720 hours.";
            return;
        }

        try
        {
            var created = await _familyMembersDataService.CreateInviteAsync(
                DraftInviteEmail.Trim(),
                DraftInviteRole,
                expiresInHours,
                cancellationToken);

            InviteMessage = $"Invite created for '{created.Invite.Email}'.";
            LastInviteToken = created.InviteToken;
            DraftInviteEmail = string.Empty;
            DraftInviteRole = Roles[0];
            DraftInviteExpiresInHours = "168";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to create invite: {ex.Message}";
        }
    }

    private async Task UpdateSelectedMemberRoleAsync(CancellationToken cancellationToken)
    {
        if (SelectedMember is null)
        {
            HasError = true;
            ErrorMessage = "Select a member to update.";
            return;
        }

        if (!Roles.Contains(SelectedMemberRole, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Selected role is invalid.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var updatedMember = await _familyMembersDataService.UpdateMemberRoleAsync(
                SelectedMember.Id,
                SelectedMemberRole,
                cancellationToken);

            EditorMessage = $"Updated role for '{updatedMember.Name}' to '{updatedMember.Role}'.";
            await LoadAsync(cancellationToken);
            SelectedMember = Members.FirstOrDefault(member => member.Id == updatedMember.Id);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to update family member role: {ex.Message}";
        }
    }

    private async Task RemoveSelectedMemberAsync(CancellationToken cancellationToken)
    {
        if (SelectedMember is null)
        {
            HasError = true;
            ErrorMessage = "Select a member to remove.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var removedName = SelectedMember.Name;
            await _familyMembersDataService.RemoveMemberAsync(SelectedMember.Id, cancellationToken);
            EditorMessage = $"Removed family member '{removedName}'.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to remove family member: {ex.Message}";
        }
    }

    private async Task CancelSelectedInviteAsync(CancellationToken cancellationToken)
    {
        if (SelectedInvite is null)
        {
            HasError = true;
            ErrorMessage = "Select an invite to cancel.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var cancelled = await _familyMembersDataService.CancelInviteAsync(SelectedInvite.Id, cancellationToken);
            InviteMessage = $"Invite for '{cancelled.Email}' cancelled.";
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to cancel invite: {ex.Message}";
        }
    }

    private async Task ResendSelectedInviteAsync(CancellationToken cancellationToken)
    {
        if (SelectedInvite is null)
        {
            HasError = true;
            ErrorMessage = "Select an invite to resend.";
            return;
        }

        if (!SelectedInvite.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Only pending invites can be resent.";
            return;
        }

        if (!int.TryParse(DraftInviteExpiresInHours, out var expiresInHours) || expiresInHours < 1 || expiresInHours > 720)
        {
            HasError = true;
            ErrorMessage = "Invite expiration must be between 1 and 720 hours.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var resent = await _familyMembersDataService.ResendInviteAsync(SelectedInvite.Id, expiresInHours, cancellationToken);
            InviteMessage = $"Invite resent for '{resent.Invite.Email}'.";
            LastInviteToken = resent.InviteToken;
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to resend invite: {ex.Message}";
        }
    }

    private void ResetDraft()
    {
        DraftKeycloakUserId = string.Empty;
        DraftName = string.Empty;
        DraftEmail = string.Empty;
        DraftRole = Roles[0];
        SelectedMemberRole = SelectedMember?.Role ?? Roles[0];
        DraftInviteEmail = string.Empty;
        DraftInviteRole = Roles[0];
        DraftInviteExpiresInHours = "168";
        LastInviteToken = string.Empty;
    }
}
