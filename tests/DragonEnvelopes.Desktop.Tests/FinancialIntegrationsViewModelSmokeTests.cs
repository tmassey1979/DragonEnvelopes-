using System.Diagnostics;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class FinancialIntegrationsViewModelSmokeTests
{
    [Fact]
    public async Task LoadCommand_PopulatesWorkspaceState()
    {
        var harness = CreateHarness();

        await EnsureLoadedAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.True(harness.ViewModel.PlaidConnected);
        Assert.True(harness.ViewModel.StripeConnected);
        Assert.Single(harness.ViewModel.AvailableAccounts);
        Assert.Single(harness.ViewModel.AvailableEnvelopes);
        Assert.Single(harness.ViewModel.PlaidAccountLinks);
        Assert.Single(harness.ViewModel.FamilyFinancialAccounts);
        Assert.Single(harness.ViewModel.PlaidReconciliationAccounts);
        Assert.Single(harness.ViewModel.FailedNotificationDispatchEvents);
        Assert.Single(harness.ViewModel.Cards);
        Assert.NotNull(harness.ViewModel.SelectedEnvelope);
        Assert.NotNull(harness.ViewModel.SelectedCard);
        Assert.NotNull(harness.ViewModel.SelectedFailedNotificationDispatchEvent);
        Assert.Equal("trace-test-002", harness.ViewModel.ProviderHealthTraceId);
        Assert.Equal(2, harness.ViewModel.ProviderTimelineEvents.Count);
        Assert.Equal("Financial integrations loaded.", harness.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task RefreshStatus_AndPlaidSyncCommands_UpdateStatusAndSummary()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        await harness.ViewModel.RefreshStatusCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal("Integration status refreshed.", harness.ViewModel.StatusMessage);

        await harness.ViewModel.SyncPlaidTransactionsCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Contains("Pulled 14, inserted 12, deduped 1, unmapped 1", harness.ViewModel.PlaidSyncSummary);
        Assert.Equal("Plaid transaction sync completed.", harness.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task LaunchNativePlaidLinkCommand_CreatesTokenAndExchangesPublicToken()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        harness.ViewModel.PlaidLinkToken = string.Empty;
        harness.PlaidLinkService.NextResult = DragonEnvelopes.Desktop.Services.DesktopPlaidLinkResult.Success("public-token-123");

        await harness.ViewModel.LaunchNativePlaidLinkCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.Equal(1, harness.FinancialDataService.CreatePlaidLinkTokenCallCount);
        Assert.Equal(1, harness.FinancialDataService.ExchangePlaidPublicTokenCallCount);
        Assert.Equal("Plaid Link completed and token exchanged.", harness.ViewModel.StatusMessage);
        Assert.Equal(string.Empty, harness.ViewModel.PlaidPublicToken);
        Assert.Equal(string.Empty, harness.ViewModel.PlaidLinkToken);
        Assert.Equal("-", harness.ViewModel.PlaidLinkTokenExpiresAt);
    }

    [Fact]
    public async Task CreateStripeSetupIntentCommand_ValidatesInput_AndExecutesAction()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        harness.ViewModel.StripeSetupEmail = "invalid-email";
        await harness.ViewModel.CreateStripeSetupIntentCommand.ExecuteAsync(null);

        Assert.True(harness.ViewModel.HasError);
        Assert.Equal("A valid Stripe customer email is required.", harness.ViewModel.ErrorMessage);
        Assert.Equal(0, harness.FinancialDataService.CreateStripeSetupIntentCallCount);

        harness.ViewModel.StripeSetupEmail = "owner@dragon.test";
        harness.ViewModel.StripeSetupName = "Dragon Family";

        await harness.ViewModel.CreateStripeSetupIntentCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.CreateStripeSetupIntentCallCount);
        Assert.Equal("seti_test_001", harness.ViewModel.StripeSetupIntentId);
        Assert.Equal("cus_test_001", harness.ViewModel.StripeSetupCustomerId);
        Assert.Equal("seti_secret_test_001", harness.ViewModel.StripeClientSecret);
        Assert.Equal("Stripe setup intent created.", harness.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task RewrapProviderSecretsCommand_UpdatesSummaryAndStatus()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        await harness.ViewModel.RewrapProviderSecretsCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.RewrapProviderSecretsCallCount);
        Assert.Contains("fields touched 3", harness.ViewModel.ProviderSecretRewrapSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Provider secret rewrap completed.", harness.ViewModel.StatusMessage);
    }

    [Fact]
    public async Task CardCommands_AndControlFlow_ExecuteWithDeterministicStateTransitions()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        Assert.NotNull(harness.ViewModel.SelectedCard);
        Assert.Equal("Active", harness.ViewModel.SelectedCard!.Status);

        await harness.ViewModel.FreezeSelectedCardCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.FreezeCardCallCount);
        Assert.Equal("Card frozen.", harness.ViewModel.StatusMessage);

        await harness.ViewModel.UnfreezeSelectedCardCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.UnfreezeCardCallCount);
        Assert.Equal("Card unfrozen.", harness.ViewModel.StatusMessage);

        await harness.ViewModel.CancelSelectedCardCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.CancelCardCallCount);
        Assert.Equal("Card canceled.", harness.ViewModel.StatusMessage);

        harness.ViewModel.CardControlDailyLimit = "30";
        harness.ViewModel.CardControlAllowedCategories = "grocery_stores, convenience_stores";
        harness.ViewModel.CardControlAllowedMerchants = "Target; Aldi";

        await harness.ViewModel.SaveCardControlsCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal("Card controls saved.", harness.ViewModel.StatusMessage);
        Assert.NotEmpty(harness.ViewModel.CardControlAuditEntries);

        harness.FinancialDataService.EvaluateSpendResponse = new EvaluateEnvelopeCardSpendResponse(
            IsAllowed: true,
            DenialReason: null,
            RemainingDailyLimit: 7.50m);
        harness.ViewModel.CardSpendMerchantName = "Target";
        harness.ViewModel.CardSpendMerchantCategory = "grocery_stores";
        harness.ViewModel.CardSpendAmount = "12.50";
        harness.ViewModel.CardSpendTodayAmount = "10.00";

        await harness.ViewModel.EvaluateCardSpendCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal("Card spend evaluation complete.", harness.ViewModel.StatusMessage);
        Assert.Contains("Allowed.", harness.ViewModel.CardSpendEvaluationResult);
    }

    [Fact]
    public async Task FailingCommand_ExposesActionSpecificErrorState()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        harness.FinancialDataService.ThrowOnRefreshPlaidBalances = true;

        await harness.ViewModel.RefreshPlaidBalancesCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.True(harness.ViewModel.HasError);
        Assert.Equal("Refreshing Plaid balances... failed.", harness.ViewModel.StatusMessage);
        Assert.Contains("simulated Plaid balance refresh failure", harness.ViewModel.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RetryFailedNotificationDispatchEventCommand_RetriesSelectedEvent()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        Assert.NotNull(harness.ViewModel.SelectedFailedNotificationDispatchEvent);

        await harness.ViewModel.RetrySelectedFailedNotificationDispatchEventCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Equal(1, harness.FinancialDataService.RetryFailedNotificationDispatchEventCallCount);
        Assert.Equal("Notification dispatch retried successfully.", harness.ViewModel.StatusMessage);
        Assert.Empty(harness.ViewModel.FailedNotificationDispatchEvents);
    }

    [Fact]
    public async Task ReplaySelectedTimelineNotificationDispatchEventCommand_ReplaysSelectedFailedTimelineEvent()
    {
        var harness = CreateHarness();
        await EnsureLoadedAsync(harness.ViewModel);

        var replayableTimelineEvent = Assert.Single(
            harness.ViewModel.ProviderTimelineEvents.Where(static evt => evt.CanReplayNotification));
        harness.ViewModel.SelectedProviderTimelineEvent = replayableTimelineEvent;

        await harness.ViewModel.ReplaySelectedTimelineNotificationDispatchEventCommand.ExecuteAsync(null);
        await WaitForIdleAsync(harness.ViewModel);

        Assert.False(harness.ViewModel.HasError);
        Assert.Contains("Timeline replay result: Sent", harness.ViewModel.NotificationRetrySummary);
        Assert.Equal("Timeline notification replay completed successfully.", harness.ViewModel.StatusMessage);
        Assert.Equal(1, harness.FinancialDataService.RetryFailedNotificationDispatchEventCallCount);
        Assert.DoesNotContain(
            harness.ViewModel.ProviderTimelineEvents,
            evt => evt.NotificationDispatchEventId == replayableTimelineEvent.NotificationDispatchEventId
                   && evt.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase));
    }

    private static TestHarness CreateHarness()
    {
        var familyId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var accountId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var envelopeId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var financialAccountId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var cardId = Guid.Parse("00000000-0000-0000-0000-000000000040");

        var financialDataService = new FakeFinancialIntegrationDataService(
            familyId,
            accountId,
            envelopeId,
            financialAccountId,
            cardId);
        var accountsDataService = new FakeAccountsDataService(accountId);
        var envelopesDataService = new FakeEnvelopesDataService(envelopeId);
        var plaidLinkService = new FakeDesktopPlaidLinkService();

        var viewModel = new FinancialIntegrationsViewModel(
            financialDataService,
            accountsDataService,
            envelopesDataService,
            plaidLinkService);

        return new TestHarness(viewModel, financialDataService, plaidLinkService);
    }

    private static async Task EnsureLoadedAsync(FinancialIntegrationsViewModel viewModel)
    {
        await WaitForIdleAsync(viewModel);
        await viewModel.LoadCommand.ExecuteAsync(null);
        await WaitForIdleAsync(viewModel);
    }

    private static async Task WaitForIdleAsync(FinancialIntegrationsViewModel viewModel, int timeoutMilliseconds = 6000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (viewModel.IsLoading)
        {
            if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
            {
                throw new TimeoutException("Timed out waiting for financial integrations view model to become idle.");
            }

            await Task.Delay(20);
        }

        await Task.Delay(20);
    }

    private sealed record TestHarness(
        FinancialIntegrationsViewModel ViewModel,
        FakeFinancialIntegrationDataService FinancialDataService,
        FakeDesktopPlaidLinkService PlaidLinkService);
}
