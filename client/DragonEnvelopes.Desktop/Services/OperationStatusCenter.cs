using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using DragonEnvelopes.Desktop.ViewModels;
using System.Windows.Threading;

namespace DragonEnvelopes.Desktop.Services;

public sealed partial class OperationStatusCenter : ObservableObject, IOperationStatusCenter
{
    private readonly Dictionary<Guid, string> _activeOperations = [];
    private readonly DispatcherTimer _cleanupTimer;
    private readonly TimeSpan _transientLifetime = TimeSpan.FromSeconds(8);

    public OperationStatusCenter()
    {
        Toasts.CollectionChanged += OnToastsCollectionChanged;
        _cleanupTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _cleanupTimer.Tick += OnCleanupTick;
        _cleanupTimer.Start();
    }

    public ObservableCollection<OperationToastItemViewModel> Toasts { get; } = [];

    public int ActiveOperationCount => _activeOperations.Count;
    public bool HasActiveOperations => ActiveOperationCount > 0;
    public bool HasToasts => Toasts.Count > 0;
    public string ActiveOperationSummary => ActiveOperationCount switch
    {
        0 => "Idle",
        1 => $"1 action running: {_activeOperations.Values.First()}",
        _ => $"{ActiveOperationCount} actions running"
    };

    public IDisposable BeginOperation(string description)
    {
        var id = Guid.NewGuid();
        _activeOperations[id] = string.IsNullOrWhiteSpace(description) ? "Working" : description.Trim();
        NotifyActiveOperationChanged();
        return new OperationScope(this, id);
    }

    public void ReportInfo(string message, bool isTransient = true) => AddToast(OperationToastLevel.Info, message, isTransient);
    public void ReportSuccess(string message, bool isTransient = true) => AddToast(OperationToastLevel.Success, message, isTransient);
    public void ReportError(string message, bool isTransient = true) => AddToast(OperationToastLevel.Error, message, isTransient);

    public void Dismiss(Guid toastId)
    {
        var toast = Toasts.FirstOrDefault(item => item.Id == toastId);
        if (toast is not null)
        {
            Toasts.Remove(toast);
        }
    }

    public void ClearTransient()
    {
        Toasts.Clear();
    }

    private void AddToast(OperationToastLevel level, string message, bool isTransient)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var trimmedMessage = message.Trim();
        if (Toasts.Count > 0)
        {
            var latest = Toasts[0];
            var isDuplicate = latest.Level == level
                && string.Equals(latest.Message, trimmedMessage, StringComparison.Ordinal)
                && DateTimeOffset.UtcNow - latest.CreatedAtUtc < TimeSpan.FromSeconds(3);
            if (isDuplicate)
            {
                return;
            }
        }

        Toasts.Insert(0, new OperationToastItemViewModel(level, trimmedMessage, isTransient));
        while (Toasts.Count > 6)
        {
            Toasts.RemoveAt(Toasts.Count - 1);
        }
    }

    private void CompleteOperation(Guid operationId)
    {
        if (_activeOperations.Remove(operationId))
        {
            NotifyActiveOperationChanged();
        }
    }

    private void NotifyActiveOperationChanged()
    {
        OnPropertyChanged(nameof(ActiveOperationCount));
        OnPropertyChanged(nameof(HasActiveOperations));
        OnPropertyChanged(nameof(ActiveOperationSummary));
    }

    private void OnCleanupTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.UtcNow;
        for (var i = Toasts.Count - 1; i >= 0; i--)
        {
            var toast = Toasts[i];
            if (toast.IsTransient && now - toast.CreatedAtUtc > _transientLifetime)
            {
                Toasts.RemoveAt(i);
            }
        }
    }

    private void OnToastsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasToasts));
    }

    private sealed class OperationScope : IDisposable
    {
        private readonly OperationStatusCenter _owner;
        private readonly Guid _operationId;
        private bool _isDisposed;

        public OperationScope(OperationStatusCenter owner, Guid operationId)
        {
            _owner = owner;
            _operationId = operationId;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _owner.CompleteOperation(_operationId);
        }
    }
}
