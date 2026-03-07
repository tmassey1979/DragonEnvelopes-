using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class TransactionsViewModel : ObservableObject
{
    private readonly ITransactionsDataService _transactionsDataService;
    private IReadOnlyList<TransactionListItemViewModel> _allTransactions = [];
    private string _sortBy = "Date";
    private bool _sortAscending;
    private const string CreatePanelTitle = "Create Transaction";
    private const string EditPanelTitle = "Edit Transaction";

    public TransactionsViewModel(ITransactionsDataService transactionsDataService)
    {
        _transactionsDataService = transactionsDataService;
        LoadAccountsCommand = new AsyncRelayCommand(LoadAccountsAsync);
        ReloadTransactionsCommand = new AsyncRelayCommand(LoadTransactionsAsync);
        SortByCommand = new RelayCommand<string>(SortBy);
        AddSplitRowCommand = new RelayCommand(AddSplitRow);
        RemoveSplitRowCommand = new RelayCommand<TransactionSplitDraftViewModel?>(RemoveSplitRow);
        BeginEditSelectedTransactionCommand = new RelayCommand(BeginEditSelectedTransaction);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SubmitTransactionCommand = new AsyncRelayCommand(SubmitTransactionAsync);
        ResetEditorCommand = new RelayCommand(ResetEditor);
        _ = LoadAccountsCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadAccountsCommand { get; }
    public IAsyncRelayCommand ReloadTransactionsCommand { get; }
    public IRelayCommand<string> SortByCommand { get; }
    public IRelayCommand AddSplitRowCommand { get; }
    public IRelayCommand<TransactionSplitDraftViewModel?> RemoveSplitRowCommand { get; }
    public IRelayCommand BeginEditSelectedTransactionCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IAsyncRelayCommand SubmitTransactionCommand { get; }
    public IRelayCommand ResetEditorCommand { get; }

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> accounts = [];

    [ObservableProperty]
    private AccountListItemViewModel? selectedAccount;

    [ObservableProperty]
    private ObservableCollection<EnvelopeOptionViewModel> envelopes = [];

    [ObservableProperty]
    private Guid? draftEnvelopeId;

    [ObservableProperty]
    private ObservableCollection<TransactionListItemViewModel> transactions = [];

    [ObservableProperty]
    private TransactionListItemViewModel? selectedTransaction;

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

    [ObservableProperty]
    private string draftDescription = string.Empty;

    [ObservableProperty]
    private string draftMerchant = string.Empty;

    [ObservableProperty]
    private decimal draftAmount;

    [ObservableProperty]
    private string draftCategory = string.Empty;

    [ObservableProperty]
    private string draftOccurredOn = DateTime.UtcNow.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private bool useSplitEditor;

    [ObservableProperty]
    private ObservableCollection<TransactionSplitDraftViewModel> splitDrafts = [];

    [ObservableProperty]
    private decimal splitTotal;

    [ObservableProperty]
    private bool isSplitTotalValid = true;

    [ObservableProperty]
    private string editorStatusMessage = "Create a transaction, optionally with splits.";

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private string editorPanelTitle = CreatePanelTitle;

    [ObservableProperty]
    private string submitButtonText = "Create";

    [ObservableProperty]
    private bool replaceAllocationOnEdit;

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

            var envelopeItems = await _transactionsDataService.GetEnvelopesAsync(cancellationToken);
            Envelopes = new ObservableCollection<EnvelopeOptionViewModel>(envelopeItems);
            DraftEnvelopeId = Envelopes.FirstOrDefault()?.Id;

            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load transaction workspace: {ex.Message}";
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

        if (IsEditMode)
        {
            ExitEditMode();
        }

        _ = ReloadTransactionsCommand.ExecuteAsync(null);
    }

    partial void OnMerchantFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnCategoryFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnIncludeUncategorizedChanged(bool value) => ApplyFiltersAndSort();
    partial void OnDraftAmountChanged(decimal value) => RecalculateSplitTotal();

    partial void OnUseSplitEditorChanged(bool value)
    {
        if (value && SplitDrafts.Count == 0)
        {
            AddSplitRow();
        }

        if (value && IsEditMode)
        {
            ReplaceAllocationOnEdit = true;
        }

        RecalculateSplitTotal();
    }

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

    private void AddSplitRow()
    {
        var row = new TransactionSplitDraftViewModel
        {
            EnvelopeId = Envelopes.FirstOrDefault()?.Id,
            Amount = 0m,
            Category = string.IsNullOrWhiteSpace(DraftCategory) ? string.Empty : DraftCategory,
            Notes = string.Empty
        };
        row.PropertyChanged += OnSplitDraftChanged;
        SplitDrafts.Add(row);
        RecalculateSplitTotal();
    }

    private void RemoveSplitRow(TransactionSplitDraftViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        row.PropertyChanged -= OnSplitDraftChanged;
        SplitDrafts.Remove(row);
        RecalculateSplitTotal();
    }

    private void OnSplitDraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TransactionSplitDraftViewModel.Amount))
        {
            RecalculateSplitTotal();
        }
    }

    private void RecalculateSplitTotal()
    {
        SplitTotal = decimal.Round(SplitDrafts.Sum(static row => row.Amount), 2, MidpointRounding.AwayFromZero);
        var roundedAmount = decimal.Round(DraftAmount, 2, MidpointRounding.AwayFromZero);
        IsSplitTotalValid = !UseSplitEditor || SplitTotal == roundedAmount;
    }

    private void BeginEditSelectedTransaction()
    {
        if (SelectedTransaction is null)
        {
            HasError = true;
            ErrorMessage = "Select a transaction to edit.";
            return;
        }

        HasError = false;
        ErrorMessage = string.Empty;

        IsEditMode = true;
        EditorPanelTitle = EditPanelTitle;
        SubmitButtonText = "Save";
        DraftDescription = SelectedTransaction.Description;
        DraftMerchant = SelectedTransaction.Merchant;
        DraftAmount = SelectedTransaction.Amount;
        DraftCategory = SelectedTransaction.Category ?? string.Empty;
        DraftOccurredOn = SelectedTransaction.OccurredAt.ToString("yyyy-MM-dd");
        DraftEnvelopeId = SelectedTransaction.EnvelopeId;

        foreach (var row in SplitDrafts)
        {
            row.PropertyChanged -= OnSplitDraftChanged;
        }

        UseSplitEditor = false;
        SplitDrafts.Clear();

        if (SelectedTransaction.HasSplits)
        {
            foreach (var split in SelectedTransaction.Splits)
            {
                var row = new TransactionSplitDraftViewModel
                {
                    EnvelopeId = split.EnvelopeId,
                    Amount = split.Amount,
                    Category = split.Category ?? string.Empty,
                    Notes = split.Notes ?? string.Empty
                };
                row.PropertyChanged += OnSplitDraftChanged;
                SplitDrafts.Add(row);
            }

            UseSplitEditor = true;
            ReplaceAllocationOnEdit = true;
        }
        else
        {
            UseSplitEditor = false;
            ReplaceAllocationOnEdit = false;
        }

        RecalculateSplitTotal();
        EditorStatusMessage = $"Editing transaction {SelectedTransaction.OccurredDateDisplay} {SelectedTransaction.Merchant}.";
    }

    private void CancelEdit()
    {
        ExitEditMode();
        ResetEditor();
        EditorStatusMessage = "Edit canceled.";
    }

    private void ExitEditMode()
    {
        IsEditMode = false;
        EditorPanelTitle = CreatePanelTitle;
        SubmitButtonText = "Create";
        ReplaceAllocationOnEdit = false;
        SelectedTransaction = null;
    }

    private async Task SubmitTransactionAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedAccount is null)
        {
            HasError = true;
            ErrorMessage = "Select an account.";
            return;
        }

        if (string.IsNullOrWhiteSpace(DraftMerchant) || string.IsNullOrWhiteSpace(DraftDescription))
        {
            HasError = true;
            ErrorMessage = "Merchant and description are required.";
            return;
        }

        if (DraftAmount == 0m)
        {
            HasError = true;
            ErrorMessage = "Amount cannot be zero.";
            return;
        }

        if (!DateTime.TryParse(DraftOccurredOn, out var occurredDate))
        {
            HasError = true;
            ErrorMessage = "Occurred date is invalid.";
            return;
        }

        if (UseSplitEditor)
        {
            if (SplitDrafts.Count == 0)
            {
                HasError = true;
                ErrorMessage = "Add at least one split row.";
                return;
            }

            if (!IsSplitTotalValid)
            {
                HasError = true;
                ErrorMessage = "Split total must equal transaction amount.";
                return;
            }

            if (SplitDrafts.Any(static row => !row.EnvelopeId.HasValue))
            {
                HasError = true;
                ErrorMessage = "Each split row must have an envelope.";
                return;
            }
        }

        try
        {
            if (IsEditMode)
            {
                if (SelectedTransaction is null)
                {
                    HasError = true;
                    ErrorMessage = "Select a transaction to edit.";
                    return;
                }

                var replaceAllocation = UseSplitEditor || ReplaceAllocationOnEdit;
                await _transactionsDataService.UpdateTransactionAsync(
                    SelectedTransaction.Id,
                    DraftDescription.Trim(),
                    DraftMerchant.Trim(),
                    DraftCategory,
                    replaceAllocation,
                    replaceAllocation && !UseSplitEditor ? DraftEnvelopeId : null,
                    replaceAllocation && UseSplitEditor ? SplitDrafts.ToArray() : null,
                    cancellationToken);

                await LoadTransactionsAsync(cancellationToken);
                ExitEditMode();
                ResetEditor();
                EditorStatusMessage = "Transaction updated.";
            }
            else
            {
                await _transactionsDataService.CreateTransactionAsync(
                    SelectedAccount.Id,
                    DraftAmount,
                    DraftDescription.Trim(),
                    DraftMerchant.Trim(),
                    new DateTimeOffset(occurredDate.Date, TimeSpan.Zero),
                    DraftCategory,
                    UseSplitEditor ? null : DraftEnvelopeId,
                    UseSplitEditor ? SplitDrafts.ToArray() : null,
                    cancellationToken);

                EditorStatusMessage = "Transaction created.";
                ResetEditor();
                await LoadTransactionsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = IsEditMode
                ? $"Unable to update transaction: {ex.Message}"
                : $"Unable to create transaction: {ex.Message}";
        }
    }

    private void ResetEditor()
    {
        if (IsEditMode)
        {
            if (SelectedTransaction is not null)
            {
                BeginEditSelectedTransaction();
                EditorStatusMessage = "Edit values reset.";
                return;
            }

            ExitEditMode();
        }

        DraftDescription = string.Empty;
        DraftMerchant = string.Empty;
        DraftAmount = 0m;
        DraftCategory = string.Empty;
        DraftOccurredOn = DateTime.UtcNow.ToString("yyyy-MM-dd");
        DraftEnvelopeId = Envelopes.FirstOrDefault()?.Id;
        foreach (var row in SplitDrafts)
        {
            row.PropertyChanged -= OnSplitDraftChanged;
        }

        SplitDrafts.Clear();
        UseSplitEditor = false;
        SplitTotal = 0m;
        IsSplitTotalValid = true;
    }
}
