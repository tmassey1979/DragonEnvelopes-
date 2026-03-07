using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Navigation;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class RouteRegistryClientRoutingTests
{
    [Fact]
    public async Task AccountsRoute_Uses_Ledger_Client()
    {
        var familyId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var familyClient = new TrackingBackendApiClient();
        var ledgerClient = new TrackingBackendApiClient();
        var familyContext = new TestFamilyContext(familyId);
        var registry = new RouteRegistry(familyClient, ledgerClient, new TestAuthService(), familyContext);

        var found = registry.TryGetRoute("/accounts", out var route);
        Assert.True(found);

        var viewModel = Assert.IsType<AccountsViewModel>(route.Content);
        await WaitForIdleAsync(viewModel);

        Assert.Contains(ledgerClient.GetRequests, path => path.StartsWith("accounts?familyId=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(familyClient.GetRequests, path => path.StartsWith("accounts?familyId=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FamilyMembersRoute_Uses_Family_Client()
    {
        var familyId = Guid.Parse("70000000-0000-0000-0000-000000000002");
        var familyClient = new TrackingBackendApiClient();
        var ledgerClient = new TrackingBackendApiClient();
        var familyContext = new TestFamilyContext(familyId);
        var registry = new RouteRegistry(familyClient, ledgerClient, new TestAuthService(), familyContext);

        var found = registry.TryGetRoute("/family-members", out var route);
        Assert.True(found);

        var viewModel = Assert.IsType<FamilyMembersViewModel>(route.Content);
        await WaitForIdleAsync(viewModel);

        Assert.Contains(familyClient.GetRequests, path => path.StartsWith($"families/{familyId}/members", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(familyClient.GetRequests, path => path.StartsWith($"families/{familyId}/invites", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(ledgerClient.GetRequests, path => path.StartsWith($"families/{familyId}/members", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RecurringBillsRoute_Uses_Ledger_Client()
    {
        var familyId = Guid.Parse("70000000-0000-0000-0000-000000000003");
        var familyClient = new TrackingBackendApiClient();
        var ledgerClient = new TrackingBackendApiClient();
        var familyContext = new TestFamilyContext(familyId);
        var registry = new RouteRegistry(familyClient, ledgerClient, new TestAuthService(), familyContext);

        var found = registry.TryGetRoute("/recurring-bills", out var route);
        Assert.True(found);

        var viewModel = Assert.IsType<RecurringBillsViewModel>(route.Content);
        await WaitForIdleAsync(viewModel);

        Assert.Contains(ledgerClient.GetRequests, path => path.StartsWith("recurring-bills?familyId=", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(familyClient.GetRequests, path => path.StartsWith("recurring-bills?familyId=", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WaitForIdleAsync(object viewModel, int timeoutMilliseconds = 6000)
    {
        var isLoadingProperty = viewModel.GetType().GetProperty("IsLoading");
        if (isLoadingProperty?.PropertyType != typeof(bool))
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        while (isLoadingProperty.GetValue(viewModel) is true)
        {
            if (stopwatch.ElapsedMilliseconds >= timeoutMilliseconds)
            {
                throw new TimeoutException("Timed out waiting for route view model to become idle.");
            }

            await Task.Delay(20);
        }

        await Task.Delay(20);
    }

    private sealed class TrackingBackendApiClient : IBackendApiClient
    {
        public List<string> GetRequests { get; } = [];
        public List<string> SendRequests { get; } = [];

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            SendRequests.Add(request.RequestUri?.ToString() ?? string.Empty);
            return Task.FromResult(CreateSuccessResponse("[]"));
        }

        public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            GetRequests.Add(relativePath);
            return Task.FromResult(CreateSuccessResponse("[]"));
        }

        private static HttpResponseMessage CreateSuccessResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class TestFamilyContext : IFamilyContext
    {
        public TestFamilyContext(Guid familyId)
        {
            FamilyId = familyId;
        }

        public Guid? FamilyId { get; private set; }

        public void SetFamilyId(Guid? familyId)
        {
            FamilyId = familyId;
        }
    }

    private sealed class TestAuthService : IAuthService
    {
        public Task<AuthSession?> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AuthSession?>(null);
        }

        public Task<AuthSignInResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSignInResult(false, false, "not used"));
        }

        public Task<AuthSignInResult> SignInWithPasswordAsync(
            string usernameOrEmail,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthSignInResult(false, false, "not used"));
        }

        public Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<string?> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }
    }
}
