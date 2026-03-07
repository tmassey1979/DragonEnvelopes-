using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text;
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

    [Theory]
    [InlineData("Adult")]
    [InlineData("Teen")]
    [InlineData("Child")]
    public async Task NonParentRoles_Disable_ParentRoute_And_AutoPostCommand(string role)
    {
        var recurringDataService = new FakeRecurringBillsDataService();
        var recurringViewModel = new RecurringBillsViewModel(recurringDataService, new RecurringExecutionCsvExporter());
        var familyId = Guid.Parse("e0000000-0000-0000-0000-000000000001");

        var navigationService = new FakeNavigationService(
        [
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "D",
                TopBarSubtitle: "Dashboard",
                Content: new ShellContentViewModel("Dashboard", "Summary", "Empty", "Body", [])),
            new RouteDefinition(
                Key: "/family-members",
                Label: "Family",
                Glyph: "F",
                TopBarSubtitle: "Family",
                Content: new ShellContentViewModel("Family", "Summary", "Empty", "Body", []),
                RequiredRole: "Parent"),
            new RouteDefinition(
                Key: "/recurring-bills",
                Label: "Recurring",
                Glyph: "R",
                TopBarSubtitle: "Recurring",
                Content: recurringViewModel)
        ]);

        var viewModel = new MainWindowViewModel(
            navigationService,
            new FakeAuthService(signInSucceeds: true, subject: "role-user"),
            new RoleAwareBackendApiClient([role], familyId),
            new FakeFamilyContext(),
            new FakeFamilySelectionStore(),
            new FakeOperationStatusCenter());

        var signIn = await viewModel.SignInWithPasswordAsync("role-user", "password");

        Assert.True(signIn.Succeeded);
        Assert.False(viewModel.IsParentUser);
        Assert.False(viewModel.NavigationItems.Single(item => item.Key == "/family-members").IsEnabled);
        Assert.False(recurringViewModel.CanRunAutoPostNow);
        Assert.False(recurringViewModel.RunAutoPostNowCommand.CanExecute(null));

        await recurringViewModel.RunAutoPostNowCommand.ExecuteAsync(null);
        Assert.Equal(0, recurringDataService.RunAutoPostCallCount);
    }

    [Fact]
    public async Task ParentRole_Enables_ParentRoute_And_AutoPostCommand()
    {
        var recurringDataService = new FakeRecurringBillsDataService();
        var recurringViewModel = new RecurringBillsViewModel(recurringDataService, new RecurringExecutionCsvExporter());
        var familyId = Guid.Parse("e1000000-0000-0000-0000-000000000001");

        var navigationService = new FakeNavigationService(
        [
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "D",
                TopBarSubtitle: "Dashboard",
                Content: new ShellContentViewModel("Dashboard", "Summary", "Empty", "Body", [])),
            new RouteDefinition(
                Key: "/family-members",
                Label: "Family",
                Glyph: "F",
                TopBarSubtitle: "Family",
                Content: new ShellContentViewModel("Family", "Summary", "Empty", "Body", []),
                RequiredRole: "Parent"),
            new RouteDefinition(
                Key: "/recurring-bills",
                Label: "Recurring",
                Glyph: "R",
                TopBarSubtitle: "Recurring",
                Content: recurringViewModel)
        ]);

        var viewModel = new MainWindowViewModel(
            navigationService,
            new FakeAuthService(signInSucceeds: true, subject: "parent-user"),
            new RoleAwareBackendApiClient(["Parent"], familyId),
            new FakeFamilyContext(),
            new FakeFamilySelectionStore(),
            new FakeOperationStatusCenter());

        var signIn = await viewModel.SignInWithPasswordAsync("parent-user", "password");

        Assert.True(signIn.Succeeded);
        Assert.True(viewModel.IsParentUser);
        Assert.True(viewModel.NavigationItems.Single(item => item.Key == "/family-members").IsEnabled);
        Assert.True(recurringViewModel.CanRunAutoPostNow);
        Assert.True(recurringViewModel.RunAutoPostNowCommand.CanExecute(null));
    }

    [Fact]
    public async Task AdminRole_Is_Treated_As_Elevated_For_Gating()
    {
        var recurringViewModel = new RecurringBillsViewModel(new FakeRecurringBillsDataService(), new RecurringExecutionCsvExporter());
        var familyId = Guid.Parse("e2000000-0000-0000-0000-000000000001");

        var navigationService = new FakeNavigationService(
        [
            new RouteDefinition(
                Key: "/dashboard",
                Label: "Dashboard",
                Glyph: "D",
                TopBarSubtitle: "Dashboard",
                Content: new ShellContentViewModel("Dashboard", "Summary", "Empty", "Body", [])),
            new RouteDefinition(
                Key: "/family-members",
                Label: "Family",
                Glyph: "F",
                TopBarSubtitle: "Family",
                Content: new ShellContentViewModel("Family", "Summary", "Empty", "Body", []),
                RequiredRole: "Parent"),
            new RouteDefinition(
                Key: "/recurring-bills",
                Label: "Recurring",
                Glyph: "R",
                TopBarSubtitle: "Recurring",
                Content: recurringViewModel)
        ]);

        var viewModel = new MainWindowViewModel(
            navigationService,
            new FakeAuthService(signInSucceeds: true, subject: "admin-user"),
            new RoleAwareBackendApiClient(["Admin"], familyId),
            new FakeFamilyContext(),
            new FakeFamilySelectionStore(),
            new FakeOperationStatusCenter());

        var signIn = await viewModel.SignInWithPasswordAsync("admin-user", "password");

        Assert.True(signIn.Succeeded);
        Assert.True(viewModel.IsParentUser);
        Assert.True(viewModel.NavigationItems.Single(item => item.Key == "/family-members").IsEnabled);
        Assert.True(recurringViewModel.CanRunAutoPostNow);
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
                GoalCount: 0,
                GoalsOnTrackCount: 0,
                GoalsBehindCount: 0,
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
        private readonly bool _signInSucceeds;
        private readonly string _subject;

        public FakeAuthService(bool signInSucceeds = false, string subject = "test-user")
        {
            _signInSucceeds = signInSucceeds;
            _subject = subject;
        }

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
            if (!_signInSucceeds)
            {
                return Task.FromResult(new AuthSignInResult(false, false, "not used"));
            }

            var session = new AuthSession
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                Subject = _subject
            };
            return Task.FromResult(new AuthSignInResult(true, false, "signed-in", session));
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

    private sealed class RoleAwareBackendApiClient : IBackendApiClient
    {
        private readonly IReadOnlyList<string> _roles;
        private readonly Guid _familyId;

        public RoleAwareBackendApiClient(IReadOnlyList<string> roles, Guid familyId)
        {
            _roles = roles;
            _familyId = familyId;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken cancellationToken = default)
        {
            if (string.Equals(relativePath, "auth/me", StringComparison.OrdinalIgnoreCase))
            {
                var rolesJson = string.Join(',', _roles.Select(static role => $"\"{role}\""));
                var payload = $"{{\"username\":\"role-user\",\"roles\":[{rolesJson}],\"familyIds\":[\"{_familyId}\"]}}";
                return Task.FromResult(CreateJsonResponse(payload));
            }

            if (relativePath.StartsWith("families/", StringComparison.OrdinalIgnoreCase))
            {
                var payload = $"{{\"id\":\"{_familyId}\",\"name\":\"Role Test Family\",\"createdAt\":\"{DateTimeOffset.UtcNow:O}\",\"members\":[]}}";
                return Task.FromResult(CreateJsonResponse(payload));
            }

            return Task.FromResult(CreateJsonResponse("{}"));
        }

        private static HttpResponseMessage CreateJsonResponse(string json)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FakeRecurringBillsDataService : IRecurringBillsDataService
    {
        public int RunAutoPostCallCount { get; private set; }

        public Task<IReadOnlyList<RecurringBillItemViewModel>> GetBillsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecurringBillItemViewModel>>([]);
        }

        public Task<RecurringBillItemViewModel> CreateBillAsync(
            string name,
            string merchant,
            decimal amount,
            string frequency,
            int dayOfMonth,
            DateOnly startDate,
            DateOnly? endDate,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<RecurringBillItemViewModel> UpdateBillAsync(
            Guid id,
            string name,
            string merchant,
            decimal amount,
            string frequency,
            int dayOfMonth,
            DateOnly startDate,
            DateOnly? endDate,
            bool isActive,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteBillAsync(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<RecurringBillProjectionItemViewModel>> GetProjectionAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecurringBillProjectionItemViewModel>>([]);
        }

        public Task<IReadOnlyList<RecurringBillExecutionItemViewModel>> GetExecutionHistoryAsync(
            Guid recurringBillId,
            int take = 25,
            string? result = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<RecurringBillExecutionItemViewModel>>([]);
        }

        public Task<RecurringAutoPostRunResultViewModel> RunAutoPostAsync(
            DateOnly? dueDate = null,
            CancellationToken cancellationToken = default)
        {
            RunAutoPostCallCount += 1;
            return Task.FromResult(new RecurringAutoPostRunResultViewModel(
                DueDate: dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                DueBillCount: 0,
                PostedCount: 0,
                SkippedCount: 0,
                FailedCount: 0,
                AlreadyProcessedCount: 0,
                Executions: []));
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
