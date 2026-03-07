using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using DragonEnvelopes.Desktop.Api;
using DragonEnvelopes.Desktop.Auth;
using DragonEnvelopes.Desktop.Navigation;
using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task RefreshFamilyBoundViewModelsAsync_TriggersDashboardReload()
    {
        var dashboardDataService = new CountingDashboardDataService();
        var dashboard = new DashboardViewModel(dashboardDataService, autoLoad: false);
        var navigationService = new FakeNavigationService(
        [
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "D",
                TopBarSubtitle: "Dashboard",
                Content: dashboard)
        ]);

        var viewModel = new MainWindowViewModel(
            navigationService,
            new FakeAuthService(),
            new FakeBackendApiClient(),
            new FakeFamilyContext(),
            new FakeFamilySelectionStore(),
            new FakeOperationStatusCenter());

        var refreshMethod = typeof(MainWindowViewModel).GetMethod(
            "RefreshFamilyBoundViewModelsAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(refreshMethod);

        var task = refreshMethod!.Invoke(viewModel, parameters: []) as Task;
        Assert.NotNull(task);
        await task!;

        Assert.Equal(1, dashboardDataService.GetWorkspaceCallCount);
    }

    private sealed class CountingDashboardDataService : IDashboardDataService
    {
        public int GetWorkspaceCallCount { get; private set; }

        public Task<DashboardWorkspaceData> GetWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            GetWorkspaceCallCount += 1;
            return Task.FromResult(new DashboardWorkspaceData(
                AccountCount: 0,
                NetWorth: 0m,
                CashBalance: 0m,
                MonthlySpend: 0m,
                RemainingBudget: 0m,
                BudgetHealthPercent: 0m,
                RecentTransactions: []));
        }
    }

    private sealed class FakeNavigationService : INavigationService
    {
        private readonly Dictionary<string, RouteDefinition> _routes;

        public FakeNavigationService(IReadOnlyList<RouteDefinition> routes)
        {
            Routes = routes;
            _routes = routes.ToDictionary(static route => route.Key, StringComparer.OrdinalIgnoreCase);
            CurrentRouteKey = "/";
            CurrentTitle = "None";
            CurrentSubtitle = "None";
        }

        public string CurrentRouteKey { get; private set; }

        public string CurrentTitle { get; private set; }

        public string CurrentSubtitle { get; private set; }

        public object? CurrentContent { get; private set; }

        public IReadOnlyCollection<RouteDefinition> Routes { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool Navigate(string routeKey)
        {
            if (!_routes.TryGetValue(routeKey, out var route))
            {
                return false;
            }

            CurrentRouteKey = route.Key;
            CurrentTitle = route.Label;
            CurrentSubtitle = route.TopBarSubtitle;
            CurrentContent = route.Content;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentRouteKey)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTitle)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSubtitle)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentContent)));
            return true;
        }
    }

    private sealed class FakeAuthService : IAuthService
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

    private sealed class FakeBackendApiClient : IBackendApiClient
    {
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class FakeFamilyContext : IFamilyContext
    {
        public Guid? FamilyId { get; private set; }

        public void SetFamilyId(Guid? familyId)
        {
            FamilyId = familyId;
        }
    }

    private sealed class FakeFamilySelectionStore : IFamilySelectionStore
    {
        public Task<Guid?> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task SaveAsync(Guid familyId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOperationStatusCenter : IOperationStatusCenter
    {
        public ObservableCollection<OperationToastItemViewModel> Toasts { get; } = [];

        public int ActiveOperationCount => 0;

        public bool HasActiveOperations => false;

        public string ActiveOperationSummary => "Idle";

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public IDisposable BeginOperation(string description)
        {
            return new NoopDisposable();
        }

        public void ReportInfo(string message, bool isTransient = true)
        {
        }

        public void ReportSuccess(string message, bool isTransient = true)
        {
        }

        public void ReportError(string message, bool isTransient = false)
        {
        }

        public void Dismiss(Guid toastId)
        {
        }

        public void ClearTransient()
        {
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
