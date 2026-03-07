using DragonEnvelopes.Desktop.Services;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class EnvelopesViewModelGoalTests
{
    [Fact]
    public async Task SaveCommand_WithGoalEnabled_CreatesGoal()
    {
        var service = new FakeEnvelopesDataService();
        var viewModel = new EnvelopesViewModel(service);
        await WaitForIdleAsync(viewModel);

        viewModel.DraftHasGoal = true;
        viewModel.DraftGoalTargetAmount = 900m;
        viewModel.DraftGoalDueDate = "2026-12-01";
        viewModel.DraftGoalStatus = "Active";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, service.CreateGoalCallCount);
        Assert.False(viewModel.HasError);
        Assert.NotNull(viewModel.SelectedEnvelope);
        Assert.True(viewModel.SelectedEnvelope!.HasGoal);
    }

    [Fact]
    public async Task SaveCommand_WithExistingGoalAndGoalDisabled_DeletesGoal()
    {
        var service = new FakeEnvelopesDataService(hasInitialGoal: true);
        var viewModel = new EnvelopesViewModel(service);
        await WaitForIdleAsync(viewModel);

        viewModel.DraftHasGoal = false;
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, service.DeleteGoalCallCount);
        Assert.False(viewModel.HasError);
        Assert.NotNull(viewModel.SelectedEnvelope);
        Assert.False(viewModel.SelectedEnvelope!.HasGoal);
    }

    [Fact]
    public async Task SaveCommand_WithExistingGoalAndGoalEnabled_UpdatesGoal()
    {
        var service = new FakeEnvelopesDataService(hasInitialGoal: true);
        var viewModel = new EnvelopesViewModel(service);
        await WaitForIdleAsync(viewModel);

        viewModel.DraftHasGoal = true;
        viewModel.DraftGoalTargetAmount = 1200m;
        viewModel.DraftGoalDueDate = "2027-01-15";
        viewModel.DraftGoalStatus = "Completed";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, service.UpdateGoalCallCount);
        Assert.False(viewModel.HasError);
        Assert.NotNull(viewModel.SelectedEnvelope);
        Assert.True(viewModel.SelectedEnvelope!.HasGoal);
        Assert.Equal("Completed", viewModel.SelectedEnvelope.GoalStatus);
    }

    private static async Task WaitForIdleAsync(EnvelopesViewModel viewModel, int timeoutMs = 4000)
    {
        var start = DateTime.UtcNow;
        while (viewModel.IsLoading)
        {
            if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Timed out waiting for envelopes view model to finish loading.");
            }

            await Task.Delay(20);
        }
    }

    private sealed class FakeEnvelopesDataService : IEnvelopesDataService
    {
        private readonly Guid _envelopeId = Guid.Parse("96000000-0000-0000-0000-000000000001");
        private readonly Dictionary<Guid, GoalState> _goalsByEnvelopeId = [];

        public FakeEnvelopesDataService(bool hasInitialGoal = false)
        {
            if (hasInitialGoal)
            {
                _goalsByEnvelopeId[_envelopeId] = new GoalState(
                    Guid.Parse("96000000-0000-0000-0000-000000000002"),
                    500m,
                    new DateOnly(2026, 11, 1),
                    "Active");
            }
        }

        public int CreateGoalCallCount { get; private set; }
        public int UpdateGoalCallCount { get; private set; }
        public int DeleteGoalCallCount { get; private set; }

        public Task<IReadOnlyList<EnvelopeListItemViewModel>> GetEnvelopesAsync(CancellationToken cancellationToken = default)
        {
            _goalsByEnvelopeId.TryGetValue(_envelopeId, out var goal);
            IReadOnlyList<EnvelopeListItemViewModel> list =
            [
                new EnvelopeListItemViewModel(
                    _envelopeId,
                    "Emergency",
                    300m,
                    150m,
                    isArchived: false,
                    goalId: goal?.GoalId,
                    goalTargetAmount: goal?.TargetAmount,
                    goalDueDate: goal?.DueDate,
                    goalStatus: goal?.Status,
                    goalProgressPercent: goal is null ? null : 40m,
                    goalProjectionStatus: goal is null ? null : "OnTrack",
                    goalDueStatus: goal is null ? "NoGoal" : "OnSchedule")
            ];

            return Task.FromResult(list);
        }

        public Task<EnvelopeListItemViewModel> CreateEnvelopeAsync(
            string name,
            decimal monthlyBudget,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EnvelopeListItemViewModel(_envelopeId, name, monthlyBudget, 0m, isArchived: false));
        }

        public Task<EnvelopeListItemViewModel> UpdateEnvelopeAsync(
            Guid envelopeId,
            string name,
            decimal monthlyBudget,
            bool isArchived,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EnvelopeListItemViewModel(envelopeId, name, monthlyBudget, 150m, isArchived));
        }

        public Task<EnvelopeListItemViewModel> ArchiveEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new EnvelopeListItemViewModel(envelopeId, "Emergency", 300m, 150m, isArchived: true));
        }

        public Task CreateGoalAsync(
            Guid envelopeId,
            decimal targetAmount,
            DateOnly dueDate,
            string status,
            CancellationToken cancellationToken = default)
        {
            CreateGoalCallCount += 1;
            _goalsByEnvelopeId[envelopeId] = new GoalState(Guid.NewGuid(), targetAmount, dueDate, status);
            return Task.CompletedTask;
        }

        public Task UpdateGoalAsync(
            Guid goalId,
            decimal targetAmount,
            DateOnly dueDate,
            string status,
            CancellationToken cancellationToken = default)
        {
            UpdateGoalCallCount += 1;
            var envelopeId = _goalsByEnvelopeId
                .Single(pair => pair.Value.GoalId == goalId)
                .Key;
            _goalsByEnvelopeId[envelopeId] = new GoalState(goalId, targetAmount, dueDate, status);
            return Task.CompletedTask;
        }

        public Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
        {
            DeleteGoalCallCount += 1;
            var match = _goalsByEnvelopeId.FirstOrDefault(pair => pair.Value.GoalId == goalId);
            if (match.Key != Guid.Empty)
            {
                _goalsByEnvelopeId.Remove(match.Key);
            }

            return Task.CompletedTask;
        }

        private sealed record GoalState(Guid GoalId, decimal TargetAmount, DateOnly DueDate, string Status);
    }
}
