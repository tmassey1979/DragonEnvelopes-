using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionsDataService _transactionsDataService;
    private IReadOnlyList<TransactionListItemViewModel> _allTransactions = [];
    private string _sortBy = "Date";
    private bool _sortAscending = false;

    public TransactionsViewModel(ITransactionsDataService transactionsDataService)
    {
        _transactionsDataService = transactionsDataService;
        LoadAccountsCommand = new AsyncRelayCommand(LoadAccountsAsync);
        ReloadTransactionsCommand = new AsyncRelayCommand(LoadTransactionsAsync);
        SortByCommand = new RelayCommand<string>(SortBy);
        _ = LoadAccountsCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadAccountsCommand { get; }
    public IAsyncRelayCommand ReloadTransactionsCommand { get; }
    public IRelayCommand<string> SortByCommand { get; }

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> accounts = [];

    [ObservableProperty]
    private AccountListItemViewModel? selectedAccount;

    [ObservableProperty]
    private ObservableCollection<TransactionListItemViewModel> transactions = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    [ObservableProperty]
    private string merchantFilter = string.Empty;

    [ObservableProperty]
    private string categoryFilter = string.Empty;

    [ObservableProperty]
    private bool includeUncategorized = true;

    [ObservableProperty]
    private string sortIndicator = "Date ↓";

    private async Task LoadAccountsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var accountItems = await _transactionsDataService.GetAccountsAsync(cancellationToken);
            Accounts = new ObservableCollection<AccountListItemViewModel>(accountItems);
            SelectedAccount = Accounts.FirstOrDefault();
            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load accounts: {ex.Message}";
            Accounts.Clear();
            Transactions.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTransactionsAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is null)
        {
            Transactions.Clear();
            IsEmpty = true;
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            _allTransactions = await _transactionsDataService.GetTransactionsAsync(SelectedAccount.Id, cancellationToken);
            ApplyFiltersAndSort();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load transactions: {ex.Message}";
            Transactions.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedAccountChanged(AccountListItemViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        _ = ReloadTransactionsCommand.ExecuteAsync(null);
    }

    partial void OnMerchantFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnCategoryFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnIncludeUncategorizedChanged(bool value) => ApplyFiltersAndSort();

    private void SortBy(string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
        {
            return;
        }

        if (string.Equals(_sortBy, column, StringComparison.OrdinalIgnoreCase))
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortBy = column;
            _sortAscending = column is "Merchant" or "Category";
        }

        SortIndicator = $"{_sortBy} {(_sortAscending ? '↑' : '↓')}";
        ApplyFiltersAndSort();
    }

    private void ApplyFiltersAndSort()
    {
        IEnumerable<TransactionListItemViewModel> query = _allTransactions;

        if (!string.IsNullOrWhiteSpace(MerchantFilter))
        {
            query = query.Where(item =>
                item.Merchant.Contains(MerchantFilter, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(MerchantFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(CategoryFilter))
        {
            query = query.Where(item =>
                item.CategoryDisplay.Contains(CategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!IncludeUncategorized)
        {
            query = query.Where(item => !string.Equals(item.CategoryDisplay, "Uncategorized", StringComparison.OrdinalIgnoreCase));
        }

        query = (_sortBy, _sortAscending) switch
        {
            ("Merchant", true) => query.OrderBy(item => item.Merchant, StringComparer.OrdinalIgnoreCase),
            ("Merchant", false) => query.OrderByDescending(item => item.Merchant, StringComparer.OrdinalIgnoreCase),
            ("Amount", true) => query.OrderBy(item => item.Amount),
            ("Amount", false) => query.OrderByDescending(item => item.Amount),
            ("Category", true) => query.OrderBy(item => item.CategoryDisplay, StringComparer.OrdinalIgnoreCase),
            ("Category", false) => query.OrderByDescending(item => item.CategoryDisplay, StringComparer.OrdinalIgnoreCase),
            ("Date", true) => query.OrderBy(item => item.OccurredAt),
            _ => query.OrderByDescending(item => item.OccurredAt)
        };

        Transactions = new ObservableCollection<TransactionListItemViewModel>(query);
        IsEmpty = Transactions.Count == 0;
    }
}
