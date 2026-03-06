using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DragonEnvelopes.Desktop.Services;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class ImportsViewModel : ObservableObject
{
    private readonly IImportsDataService _importsDataService;

    public ImportsViewModel(IImportsDataService importsDataService)
    {
        _importsDataService = importsDataService;
        Delimiter = ",";

        LoadAccountsCommand = new AsyncRelayCommand(LoadAccountsAsync);
        PreviewCommand = new AsyncRelayCommand(PreviewAsync);
        CommitCommand = new AsyncRelayCommand(CommitAsync);
        ResetCommand = new RelayCommand(Reset);

        _ = LoadAccountsCommand.ExecuteAsync(null);
    }

    public IAsyncRelayCommand LoadAccountsCommand { get; }
    public IAsyncRelayCommand PreviewCommand { get; }
    public IAsyncRelayCommand CommitCommand { get; }
    public IRelayCommand ResetCommand { get; }

    [ObservableProperty]
    private ObservableCollection<AccountListItemViewModel> accounts = [];

    [ObservableProperty]
    private AccountListItemViewModel? selectedAccount;

    [ObservableProperty]
    private string csvContent = string.Empty;

    [ObservableProperty]
    private string delimiter = ",";

    [ObservableProperty]
    private bool includeDedupedRows;

    [ObservableProperty]
    private ObservableCollection<ImportPreviewRowViewModel> previewRows = [];

    [ObservableProperty]
    private string previewSummary = "Load CSV content and run preview.";

    [ObservableProperty]
    private string commitSummary = "No commit has been run.";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isEmpty;

    private async Task LoadAccountsAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var accounts = await _importsDataService.GetAccountsAsync(cancellationToken);
            Accounts = new ObservableCollection<AccountListItemViewModel>(accounts);
            SelectedAccount = Accounts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Unable to load accounts for import: {ex.Message}";
            Accounts.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PreviewAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedAccount is null)
        {
            HasError = true;
            ErrorMessage = "Select an account.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CsvContent))
        {
            HasError = true;
            ErrorMessage = "CSV content is required.";
            return;
        }

        if (!string.IsNullOrEmpty(Delimiter) && Delimiter.Length != 1)
        {
            HasError = true;
            ErrorMessage = "Delimiter must be exactly one character.";
            return;
        }

        IsLoading = true;
        try
        {
            var preview = await _importsDataService.PreviewAsync(
                SelectedAccount.Id,
                CsvContent,
                string.IsNullOrWhiteSpace(Delimiter) ? null : Delimiter,
                headerMappings: null,
                cancellationToken);

            PreviewRows = new ObservableCollection<ImportPreviewRowViewModel>(
                preview.Rows.Select(static row => new ImportPreviewRowViewModel(
                    row.RowNumber,
                    row.OccurredOn,
                    row.Amount,
                    row.Merchant,
                    row.Description,
                    row.Category,
                    row.IsDuplicate,
                    row.Errors)));

            PreviewSummary = $"Parsed: {preview.Parsed}, Valid: {preview.Valid}, Deduped: {preview.Deduped}, Rows: {PreviewRows.Count}.";
            IsEmpty = PreviewRows.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Preview failed: {ex.Message}";
            PreviewRows.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CommitAsync(CancellationToken cancellationToken)
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (SelectedAccount is null)
        {
            HasError = true;
            ErrorMessage = "Select an account.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CsvContent))
        {
            HasError = true;
            ErrorMessage = "CSV content is required.";
            return;
        }

        var acceptedRows = PreviewRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Errors) == false)
            .Where(row => IncludeDedupedRows || !row.IsDuplicate)
            .Select(static row => row.RowNumber)
            .ToArray();

        IsLoading = true;
        try
        {
            var result = await _importsDataService.CommitAsync(
                SelectedAccount.Id,
                CsvContent,
                string.IsNullOrWhiteSpace(Delimiter) ? null : Delimiter,
                headerMappings: null,
                acceptedRows,
                cancellationToken);

            CommitSummary = $"Parsed: {result.Parsed}, Valid: {result.Valid}, Deduped: {result.Deduped}, Inserted: {result.Inserted}, Failed: {result.Failed}.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Commit failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Reset()
    {
        CsvContent = string.Empty;
        Delimiter = ",";
        IncludeDedupedRows = false;
        PreviewRows.Clear();
        PreviewSummary = "Load CSV content and run preview.";
        CommitSummary = "No commit has been run.";
        IsEmpty = true;
    }
}
