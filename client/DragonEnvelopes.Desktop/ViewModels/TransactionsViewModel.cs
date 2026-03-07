using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class TransactionsViewModel : ObservableObject, IRoleAwareWorkspaceViewModel
{
    private readonly ITransactionsDataService _transactionsDataService;
    private IReadOnlyList<TransactionListItemViewModel> _allTransactions = [];
    private IReadOnlyList<TransactionListItemViewModel> _allDeletedTransactions = [];
    private IReadOnlyList<ApprovalRequestItemViewModel> _allApprovalRequests = [];
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
        DeleteSelectedTransactionCommand = new AsyncRelayCommand(DeleteSelectedTransactionAsync);
        RestoreSelectedDeletedTransactionCommand = new AsyncRelayCommand(RestoreSelectedDeletedTransactionAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        SubmitTransactionCommand = new AsyncRelayCommand(SubmitTransactionAsync);
        SubmitEnvelopeTransferCommand = new AsyncRelayCommand(SubmitEnvelopeTransferAsync);
        ResetEditorCommand = new RelayCommand(ResetEditor);
        RefreshApprovalQueueCommand = new AsyncRelayCommand(LoadApprovalQueueAsync);
        ApproveSelectedApprovalRequestCommand = new AsyncRelayCommand(ApproveSelectedApprovalRequestAsync, CanResolveSelectedApprovalRequest);
        DenySelectedApprovalRequestCommand = new AsyncRelayCommand(DenySelectedApprovalRequestAsync, CanResolveSelectedApprovalRequest);
        _ = LoadAccountsCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadAccountsCommand { get; }
    public IAsyncRelayCommand ReloadTransactionsCommand { get; }
    public IRelayCommand<string> SortByCommand { get; }
    public IRelayCommand AddSplitRowCommand { get; }
    public IRelayCommand<TransactionSplitDraftViewModel?> RemoveSplitRowCommand { get; }
    public IRelayCommand BeginEditSelectedTransactionCommand { get; }
    public IAsyncRelayCommand DeleteSelectedTransactionCommand { get; }
    public IAsyncRelayCommand RestoreSelectedDeletedTransactionCommand { get; }
    public IRelayCommand CancelEditCommand { get; }
    public IAsyncRelayCommand SubmitTransactionCommand { get; }
    public IAsyncRelayCommand SubmitEnvelopeTransferCommand { get; }
    public IRelayCommand ResetEditorCommand { get; }
    public IAsyncRelayCommand RefreshApprovalQueueCommand { get; }
    public IAsyncRelayCommand ApproveSelectedApprovalRequestCommand { get; }
    public IAsyncRelayCommand DenySelectedApprovalRequestCommand { get; }

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
    private ObservableCollection<TransactionListItemViewModel> deletedTransactions = [];

    [ObservableProperty]
    private TransactionListItemViewModel? selectedDeletedTransaction;

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
    private string fromDateFilter = string.Empty;

    [ObservableProperty]
    private string toDateFilter = string.Empty;

    [ObservableProperty]
    private string deletedWindowDays = "30";

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
    private Guid? transferFromEnvelopeId;

    [ObservableProperty]
    private Guid? transferToEnvelopeId;

    [ObservableProperty]
    private decimal transferAmount;

    [ObservableProperty]
    private string transferNotes = string.Empty;

    [ObservableProperty]
    private string transferStatusMessage = "Initiate envelope-to-envelope transfers.";

    [ObservableProperty]
    private string dateFilterErrorMessage = string.Empty;

    [ObservableProperty]
    private bool isEditMode;

    [ObservableProperty]
    private string editorPanelTitle = CreatePanelTitle;

    [ObservableProperty]
    private string submitButtonText = "Create";

    [ObservableProperty]
    private bool replaceAllocationOnEdit;

    [ObservableProperty]
    private bool isParentUser;

    [ObservableProperty]
    private ObservableCollection<ApprovalRequestItemViewModel> pendingApprovalRequests = [];

    [ObservableProperty]
    private ApprovalRequestItemViewModel? selectedPendingApprovalRequest;

    [ObservableProperty]
    private string approvalQueueStatusMessage = "Pending approval requests will appear here.";

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
            TransferFromEnvelopeId = Envelopes.FirstOrDefault()?.Id;
            TransferToEnvelopeId = Envelopes.Skip(1).FirstOrDefault()?.Id ?? Envelopes.FirstOrDefault()?.Id;

            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load transaction workspace: {ex.Message}";
            Accounts.Clear();
            Transactions.Clear();
            PendingApprovalRequests.Clear();
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
            DeletedTransactions.Clear();
            PendingApprovalRequests.Clear();
            _allApprovalRequests = [];
            IsEmpty = true;
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            _allTransactions = await _transactionsDataService.GetTransactionsAsync(SelectedAccount.Id, cancellationToken);
            await LoadDeletedTransactionsCoreAsync(cancellationToken);
            await LoadApprovalQueueCoreAsync(cancellationToken);
            ApplyFiltersAndSort();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load transactions: {ex.Message}";
            Transactions.Clear();
            DeletedTransactions.Clear();
            PendingApprovalRequests.Clear();
            _allApprovalRequests = [];
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

    partial void OnSelectedPendingApprovalRequestChanged(ApprovalRequestItemViewModel? value)
    {
        ApproveSelectedApprovalRequestCommand.NotifyCanExecuteChanged();
        DenySelectedApprovalRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsParentUserChanged(bool value)
    {
        ApproveSelectedApprovalRequestCommand.NotifyCanExecuteChanged();
        DenySelectedApprovalRequestCommand.NotifyCanExecuteChanged();

        if (!value)
        {
            ApprovalQueueStatusMessage = "Pending approval requests are view-only for non-parent users.";
        }
    }

    partial void OnMerchantFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnCategoryFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnFromDateFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnToDateFilterChanged(string value) => ApplyFiltersAndSort();
    partial void OnIncludeUncategorizedChanged(bool value) => ApplyFiltersAndSort();
    partial void OnDeletedWindowDaysChanged(string value) => _ = ReloadTransactionsCommand.ExecuteAsync(null);
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
        var hasFrom = TryParseFilterDate(FromDateFilter, "From", out var fromDate, out var fromError);
        var hasTo = TryParseFilterDate(ToDateFilter, "To", out var toDate, out var toError);

        if (!hasFrom)
        {
            DateFilterErrorMessage = fromError ?? "From date filter is invalid.";
        }
        else if (!hasTo)
        {
            DateFilterErrorMessage = toError ?? "To date filter is invalid.";
        }
        else if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            DateFilterErrorMessage = "From date must be on or before To date.";
        }
        else
        {
            DateFilterErrorMessage = string.Empty;
        }

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

        if (DateFilterErrorMessage.Length == 0)
        {
            if (fromDate.HasValue)
            {
                query = query.Where(item => DateOnly.FromDateTime(item.OccurredAt.UtcDateTime.Date) >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(item => DateOnly.FromDateTime(item.OccurredAt.UtcDateTime.Date) <= toDate.Value);
            }
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

    private static bool TryParseFilterDate(
        string value,
        string label,
        out DateOnly? parsedDate,
        out string? error)
    {
        parsedDate = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!DateOnly.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            error = $"{label} date must use yyyy-MM-dd format.";
            return false;
        }

        parsedDate = parsed;
        return true;
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

    private async Task DeleteSelectedTransactionAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedTransaction is null)
        {
            HasError = true;
            ErrorMessage = "Select a transaction to delete.";
            return;
        }

        var transactionId = SelectedTransaction.Id;
        try
        {
            await _transactionsDataService.DeleteTransactionAsync(transactionId, cancellationToken);

            if (IsEditMode)
            {
                ExitEditMode();
                ResetEditor();
            }

            await LoadTransactionsAsync(cancellationToken);
            SelectedTransaction = null;
            EditorStatusMessage = "Transaction deleted.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to delete transaction: {ex.Message}";
        }
    }

    private async Task RestoreSelectedDeletedTransactionAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedDeletedTransaction is null)
        {
            HasError = true;
            ErrorMessage = "Select a deleted transaction to restore.";
            return;
        }

        var transactionId = SelectedDeletedTransaction.Id;
        try
        {
            await _transactionsDataService.RestoreTransactionAsync(transactionId, cancellationToken);
            await LoadTransactionsAsync(cancellationToken);
            SelectedTransaction = Transactions.FirstOrDefault(item => item.Id == transactionId);
            SelectedDeletedTransaction = DeletedTransactions.FirstOrDefault();
            EditorStatusMessage = "Deleted transaction restored.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to restore transaction: {ex.Message}";
        }
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
                var createResult = await _transactionsDataService.CreateTransactionAsync(
                    SelectedAccount.Id,
                    DraftAmount,
                    DraftDescription.Trim(),
                    DraftMerchant.Trim(),
                    new DateTimeOffset(occurredDate.Date, TimeSpan.Zero),
                    DraftCategory,
                    UseSplitEditor ? null : DraftEnvelopeId,
                    UseSplitEditor ? SplitDrafts.ToArray() : null,
                    cancellationToken);

                if (createResult.RequiresApproval)
                {
                    EditorStatusMessage = "Transaction submitted for parent approval.";
                    if (createResult.ApprovalRequest is not null)
                    {
                        SelectedPendingApprovalRequest = createResult.ApprovalRequest;
                    }

                    ResetEditor();
                    await LoadApprovalQueueCoreAsync(cancellationToken);
                    return;
                }

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

    private async Task SubmitEnvelopeTransferAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedAccount is null)
        {
            HasError = true;
            ErrorMessage = "Select an account before creating an envelope transfer.";
            return;
        }

        if (!TransferFromEnvelopeId.HasValue || !TransferToEnvelopeId.HasValue)
        {
            HasError = true;
            ErrorMessage = "Select both source and destination envelopes.";
            return;
        }

        if (TransferFromEnvelopeId.Value == TransferToEnvelopeId.Value)
        {
            HasError = true;
            ErrorMessage = "Source and destination envelopes must be different.";
            return;
        }

        if (TransferAmount <= 0m)
        {
            HasError = true;
            ErrorMessage = "Transfer amount must be greater than zero.";
            return;
        }

        try
        {
            await _transactionsDataService.CreateEnvelopeTransferAsync(
                SelectedAccount.Id,
                TransferFromEnvelopeId.Value,
                TransferToEnvelopeId.Value,
                TransferAmount,
                DateTimeOffset.UtcNow,
                TransferNotes,
                cancellationToken);

            TransferStatusMessage = "Envelope transfer created.";
            TransferAmount = 0m;
            TransferNotes = string.Empty;
            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to create envelope transfer: {ex.Message}";
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

    private async Task LoadDeletedTransactionsCoreAsync(CancellationToken cancellationToken)
    {
        var days = ParseDeletedWindowDays();
        _allDeletedTransactions = await _transactionsDataService.GetDeletedTransactionsAsync(days, cancellationToken);
        DeletedTransactions = new ObservableCollection<TransactionListItemViewModel>(
            _allDeletedTransactions
                .OrderByDescending(static transaction => transaction.DeletedAtUtc)
                .ThenByDescending(static transaction => transaction.OccurredAt));
        SelectedDeletedTransaction = DeletedTransactions.FirstOrDefault();
    }

    private async Task LoadApprovalQueueAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            await LoadApprovalQueueCoreAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load approval queue: {ex.Message}";
            PendingApprovalRequests.Clear();
            _allApprovalRequests = [];
            ApplyApprovalStatusBadges();
        }
    }

    private async Task LoadApprovalQueueCoreAsync(CancellationToken cancellationToken)
    {
        var selectedApprovalId = SelectedPendingApprovalRequest?.Id;
        _allApprovalRequests = await _transactionsDataService.GetApprovalRequestsAsync(status: null, take: 100, cancellationToken);

        PendingApprovalRequests = new ObservableCollection<ApprovalRequestItemViewModel>(
            _allApprovalRequests
                .Where(static approval => approval.IsPending)
                .OrderByDescending(static approval => approval.CreatedAtUtc));

        SelectedPendingApprovalRequest = selectedApprovalId.HasValue
            ? PendingApprovalRequests.FirstOrDefault(item => item.Id == selectedApprovalId.Value) ?? PendingApprovalRequests.FirstOrDefault()
            : PendingApprovalRequests.FirstOrDefault();

        ApprovalQueueStatusMessage = PendingApprovalRequests.Count switch
        {
            0 when IsParentUser => "No pending approval requests.",
            0 => "Pending approval requests are view-only for non-parent users.",
            1 => "1 request is pending approval.",
            _ => $"{PendingApprovalRequests.Count} requests are pending approval."
        };

        ApproveSelectedApprovalRequestCommand.NotifyCanExecuteChanged();
        DenySelectedApprovalRequestCommand.NotifyCanExecuteChanged();
        ApplyApprovalStatusBadges();
    }

    private void ApplyApprovalStatusBadges()
    {
        var approvedLookup = _allApprovalRequests
            .Where(static approval =>
                approval.ApprovedTransactionId.HasValue &&
                string.Equals(approval.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            .GroupBy(static approval => approval.ApprovedTransactionId!.Value)
            .ToDictionary(static group => group.Key, static group => "Approved");

        foreach (var transaction in _allTransactions)
        {
            transaction.SetApprovalStatus(
                approvedLookup.TryGetValue(transaction.Id, out var status)
                    ? status
                    : null);
        }
    }

    private bool CanResolveSelectedApprovalRequest()
    {
        return IsParentUser && SelectedPendingApprovalRequest is { IsPending: true };
    }

    private async Task ApproveSelectedApprovalRequestAsync(CancellationToken cancellationToken)
    {
        if (!IsParentUser)
        {
            HasError = true;
            ErrorMessage = "Parent role is required to approve requests.";
            return;
        }

        if (SelectedPendingApprovalRequest is null)
        {
            HasError = true;
            ErrorMessage = "Select a pending approval request.";
            return;
        }

        try
        {
            var resolved = await _transactionsDataService.ApproveApprovalRequestAsync(
                SelectedPendingApprovalRequest.Id,
                cancellationToken: cancellationToken);
            ApprovalQueueStatusMessage = $"Approved request for {resolved.AmountDisplay} at {resolved.Merchant}.";
            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to approve request: {ex.Message}";
        }
    }

    private async Task DenySelectedApprovalRequestAsync(CancellationToken cancellationToken)
    {
        if (!IsParentUser)
        {
            HasError = true;
            ErrorMessage = "Parent role is required to deny requests.";
            return;
        }

        if (SelectedPendingApprovalRequest is null)
        {
            HasError = true;
            ErrorMessage = "Select a pending approval request.";
            return;
        }

        try
        {
            var resolved = await _transactionsDataService.DenyApprovalRequestAsync(
                SelectedPendingApprovalRequest.Id,
                cancellationToken: cancellationToken);
            ApprovalQueueStatusMessage = $"Denied request for {resolved.AmountDisplay} at {resolved.Merchant}.";
            await LoadTransactionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to deny request: {ex.Message}";
        }
    }

    public void ApplyRoleContext(bool isParentUser)
    {
        IsParentUser = isParentUser;
    }

    private int ParseDeletedWindowDays()
    {
        if (!int.TryParse(DeletedWindowDays, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedDays))
        {
            return 30;
        }

        return Math.Clamp(parsedDays, 1, 90);
    }
}
