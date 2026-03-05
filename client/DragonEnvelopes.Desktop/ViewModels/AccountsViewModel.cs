using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class AccountsViewModel : ObservableObject
{
    private readonly IAccountsDataService _accountsDataService;

    public AccountsViewModel(IAccountsDataService accountsDataService)
    {
        _accountsDataService = accountsDataService;
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        AddAccountPlaceholderCommand = new RelayCommand(() =>
        {
            PlaceholderMessage = "Account create/edit actions will be enabled in a follow-up story.";
        });

        _ = LoadCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadCommand { get; }

    public IRelayCommand AddAccountPlaceholderCommand { get; }

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
    private string placeholderMessage = "Future account actions (create/edit/archive) are scaffolded here.";

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        PlaceholderMessage = "Future account actions (create/edit/archive) are scaffolded here.";

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
}
