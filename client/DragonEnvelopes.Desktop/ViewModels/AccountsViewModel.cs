using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class AccountsViewModel : ObservableObject
{
    private static readonly string[] AccountTypes = ["Checking", "Savings", "Cash", "Credit"];
    private readonly IAccountsDataService _accountsDataService;

    public AccountsViewModel(IAccountsDataService accountsDataService)
    {
        _accountsDataService = accountsDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveAccountCommand = new AsyncRelayCommand(SaveAccountAsync);
        ResetAccountDraftCommand = new RelayCommand(ResetDraft);
        DraftType = AccountTypes[0];

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand SaveAccountCommand { get; }
    public IRelayCommand ResetAccountDraftCommand { get; }
    public IReadOnlyList<string> AccountTypeOptions { get; } = AccountTypes;

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> accounts = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string editorMessage = "Create a new account for the active family.";

    [ObservableProperty]
    private string draftName = string.Empty;

    [ObservableProperty]
    private string draftType = string.Empty;

    [ObservableProperty]
    private decimal draftOpeningBalance;

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        EditorMessage = "Create a new account for the active family.";

        try
        {
            var results = await _accountsDataService.GetAccountsAsync(cancellationToken);
            Accounts = new ObservableCollection<AccountListItemViewModel>(results);
            IsEmpty = Accounts.Count == 0;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Account load canceled.";
            HasError = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Unable to load accounts: {ex.Message}";
            HasError = true;
            IsEmpty = true;
            Accounts.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SaveAccountAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DraftName))
        {
            HasError = true;
            ErrorMessage = "Account name is required.";
            return;
        }

        if (!AccountTypes.Contains(DraftType, StringComparer.OrdinalIgnoreCase))
        {
            HasError = true;
            ErrorMessage = "Account type is invalid.";
            return;
        }

        if (DraftOpeningBalance < 0m)
        {
            HasError = true;
            ErrorMessage = "Opening balance cannot be negative.";
            return;
        }

        try
        {
            await _accountsDataService.CreateAccountAsync(
                DraftName.Trim(),
                DraftType,
                DraftOpeningBalance,
                cancellationToken);

            EditorMessage = $"Account '{DraftName.Trim()}' created.";
            ResetDraft();
            await LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to create account: {ex.Message}";
        }
    }

    private void ResetDraft()
    {
        DraftName = string.Empty;
        DraftType = AccountTypes[0];
        DraftOpeningBalance = 0m;
    }
}
