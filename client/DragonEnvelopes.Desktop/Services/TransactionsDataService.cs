using System.Text.Json;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Approvals;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public sealed class TransactionsDataService : ITransactionsDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IBackendApiClient _apiClient;
    private readonly IFamilyContext _familyContext;

    public TransactionsDataService(IBackendApiClient apiClient, IFamilyContext familyContext)
    {
        _apiClient = apiClient;
        _familyContext = familyContext;
    }

    public async Task<IReadOnlyList<AccountListItemViewModel>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"accounts?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Accounts request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var accounts = await JsonSerializer.DeserializeAsync<List<AccountResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return accounts
            .OrderBy(static account => account.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static account => new AccountListItemViewModel(
                account.Id,
                account.Name,
                account.Type,
                account.Balance.ToString("$#,##0.00")))
            .ToArray();
    }

    public async Task<IReadOnlyList<TransactionListItemViewModel>> GetTransactionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var envelopesResponse = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!envelopesResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)envelopesResponse.StatusCode}.");
        }

        await using var envelopesStream = await envelopesResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(envelopesStream, SerializerOptions, cancellationToken)
            ?? [];
        var envelopeLookup = envelopes.ToDictionary(static envelope => envelope.Id, static envelope => envelope.Name);

        using var response = await _apiClient.GetAsync($"transactions?accountId={accountId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Transactions request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var transactions = await JsonSerializer.DeserializeAsync<List<TransactionResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return transactions.Select(transaction => MapTransaction(transaction, envelopeLookup)).ToArray();
    }

    public async Task<IReadOnlyList<TransactionListItemViewModel>> GetDeletedTransactionsAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var envelopesResponse = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!envelopesResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)envelopesResponse.StatusCode}.");
        }

        await using var envelopesStream = await envelopesResponse.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(envelopesStream, SerializerOptions, cancellationToken)
            ?? [];
        var envelopeLookup = envelopes.ToDictionary(static envelope => envelope.Id, static envelope => envelope.Name);

        var boundedDays = Math.Clamp(days, 1, 90);
        using var response = await _apiClient.GetAsync(
            $"transactions/deleted?familyId={familyId}&days={boundedDays}",
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Deleted transactions request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var transactions = await JsonSerializer.DeserializeAsync<List<TransactionResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];

        return transactions.Select(transaction => MapTransaction(transaction, envelopeLookup)).ToArray();
    }

    public async Task<IReadOnlyList<EnvelopeOptionViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        using var response = await _apiClient.GetAsync($"envelopes?familyId={familyId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelopes request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var envelopes = await JsonSerializer.DeserializeAsync<List<EnvelopeResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return envelopes
            .Where(static envelope => !envelope.IsArchived)
            .OrderBy(static envelope => envelope.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static envelope => new EnvelopeOptionViewModel(envelope.Id, envelope.Name))
            .ToArray();
    }

    public async Task<CreateTransactionSubmissionResult> CreateTransactionAsync(
        Guid accountId,
        decimal amount,
        string description,
        string merchant,
        DateTimeOffset occurredAt,
        string? category,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default)
    {
        var payload = new CreateTransactionRequest(
            accountId,
            amount,
            description,
            merchant,
            occurredAt,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            envelopeId,
            splits is null
                ? null
                : splits.Select(static split => new TransactionSplitRequest(
                        split.EnvelopeId!.Value,
                        split.Amount,
                        string.IsNullOrWhiteSpace(split.Category) ? null : split.Category.Trim(),
                        string.IsNullOrWhiteSpace(split.Notes) ? null : split.Notes.Trim()))
                    .ToArray());

        using var request = new HttpRequestMessage(HttpMethod.Post, "transactions")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var approvalRequest = await JsonSerializer.DeserializeAsync<ApprovalRequestResponse>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidOperationException("Approval request response payload was empty.");
            return new CreateTransactionSubmissionResult(
                true,
                MapApprovalRequest(approvalRequest));
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Create transaction failed with status {(int)response.StatusCode}.");
        }

        return new CreateTransactionSubmissionResult(false, null);
    }

    public async Task UpdateTransactionAsync(
        Guid transactionId,
        string description,
        string merchant,
        string? category,
        bool replaceAllocation,
        Guid? envelopeId,
        IReadOnlyList<TransactionSplitDraftViewModel>? splits,
        CancellationToken cancellationToken = default)
    {
        var payload = new UpdateTransactionRequest(
            description,
            merchant,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            replaceAllocation,
            replaceAllocation && (splits is null || splits.Count == 0) ? envelopeId : null,
            replaceAllocation && splits is { Count: > 0 }
                ? splits.Select(static split => new TransactionSplitRequest(
                        split.EnvelopeId!.Value,
                        split.Amount,
                        string.IsNullOrWhiteSpace(split.Category) ? null : split.Category.Trim(),
                        string.IsNullOrWhiteSpace(split.Notes) ? null : split.Notes.Trim()))
                    .ToArray()
                : null);

        using var request = new HttpRequestMessage(HttpMethod.Put, $"transactions/{transactionId}")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Update transaction failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task DeleteTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"transactions/{transactionId}");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Delete transaction failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task RestoreTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"transactions/{transactionId}/restore");
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Restore transaction failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task CreateEnvelopeTransferAsync(
        Guid accountId,
        Guid fromEnvelopeId,
        Guid toEnvelopeId,
        decimal amount,
        DateTimeOffset occurredAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var payload = new CreateEnvelopeTransferRequest(
            familyId,
            accountId,
            fromEnvelopeId,
            toEnvelopeId,
            amount,
            occurredAt,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());

        using var request = new HttpRequestMessage(HttpMethod.Post, "transactions/envelope-transfers")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Envelope transfer failed with status {(int)response.StatusCode}.");
        }
    }

    public async Task<IReadOnlyList<ApprovalRequestItemViewModel>> GetApprovalRequestsAsync(
        string? status = null,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var familyId = RequireFamilyId();
        var normalizedTake = Math.Clamp(take, 1, 200);
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        var query = normalizedStatus is null
            ? $"approvals/requests?familyId={familyId}&take={normalizedTake}"
            : $"approvals/requests?familyId={familyId}&status={Uri.EscapeDataString(normalizedStatus)}&take={normalizedTake}";

        using var response = await _apiClient.GetAsync(query, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Approval requests request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var approvals = await JsonSerializer.DeserializeAsync<List<ApprovalRequestResponse>>(stream, SerializerOptions, cancellationToken)
            ?? [];
        return approvals.Select(MapApprovalRequest).ToArray();
    }

    public async Task<ApprovalRequestItemViewModel> ApproveApprovalRequestAsync(
        Guid requestId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new ResolveApprovalRequestRequest(string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"approvals/requests/{requestId}/approve")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Approve request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var approval = await JsonSerializer.DeserializeAsync<ApprovalRequestResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Approve request response payload was empty.");
        return MapApprovalRequest(approval);
    }

    public async Task<ApprovalRequestItemViewModel> DenyApprovalRequestAsync(
        Guid requestId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new ResolveApprovalRequestRequest(string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"approvals/requests/{requestId}/deny")
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };
        using var response = await _apiClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Deny request failed with status {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var approval = await JsonSerializer.DeserializeAsync<ApprovalRequestResponse>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidOperationException("Deny request response payload was empty.");
        return MapApprovalRequest(approval);
    }

    private Guid RequireFamilyId()
    {
        if (!_familyContext.FamilyId.HasValue)
        {
            throw new InvalidOperationException("No family is selected for the current session.");
        }

        return _familyContext.FamilyId.Value;
    }

    private static TransactionListItemViewModel MapTransaction(
        TransactionResponse transaction,
        IReadOnlyDictionary<Guid, string> envelopeLookup)
    {
        return new TransactionListItemViewModel(
            transaction.Id,
            transaction.AccountId,
            transaction.OccurredAt,
            transaction.Merchant,
            transaction.Description,
            transaction.Amount,
            transaction.Category,
            transaction.EnvelopeId,
            transaction.EnvelopeId.HasValue && envelopeLookup.TryGetValue(transaction.EnvelopeId.Value, out var envelopeName)
                ? envelopeName
                : "-",
            transaction.Splits.Select(static split => new TransactionSplitSnapshotViewModel(
                    split.EnvelopeId,
                    split.Amount,
                    split.Category,
                    split.Notes))
                .ToArray(),
            transaction.DeletedAtUtc,
            transaction.DeletedByUserId,
            transaction.TransferId,
            transaction.TransferCounterpartyEnvelopeId,
            transaction.TransferDirection,
            transaction.TransferCounterpartyEnvelopeId.HasValue
                && envelopeLookup.TryGetValue(transaction.TransferCounterpartyEnvelopeId.Value, out var counterpartyEnvelopeName)
                ? counterpartyEnvelopeName
                : null);
    }

    private static ApprovalRequestItemViewModel MapApprovalRequest(ApprovalRequestResponse approval)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(approval.Status)
            ? "Pending"
            : approval.Status.Trim();
        var requestNotes = string.IsNullOrWhiteSpace(approval.RequestNotes)
            ? string.Empty
            : approval.RequestNotes.Trim();
        var resolutionNotes = string.IsNullOrWhiteSpace(approval.ResolutionNotes)
            ? string.Empty
            : approval.ResolutionNotes.Trim();
        var requestedByRole = string.IsNullOrWhiteSpace(approval.RequestedByRole)
            ? "Unknown"
            : approval.RequestedByRole.Trim();
        var resolvedByRole = string.IsNullOrWhiteSpace(approval.ResolvedByRole)
            ? null
            : approval.ResolvedByRole.Trim();

        return new ApprovalRequestItemViewModel(
            approval.Id,
            approval.FamilyId,
            approval.AccountId,
            approval.RequestedByUserId,
            requestedByRole,
            approval.Amount,
            approval.Amount.ToString("$#,##0.00"),
            approval.Description,
            approval.Merchant,
            approval.OccurredAt,
            approval.OccurredAt.ToString("yyyy-MM-dd"),
            approval.Category,
            approval.EnvelopeId,
            normalizedStatus,
            normalizedStatus,
            requestNotes,
            resolutionNotes,
            resolvedByRole,
            approval.CreatedAtUtc,
            approval.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            approval.ResolvedAtUtc,
            approval.ApprovedTransactionId);
    }
}
