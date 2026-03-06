using System.Collections.ObjectModel;
using System.ComponentModel;
using DragonEnvelopes.Desktop.ViewModels;

namespace DragonEnvelopes.Desktop.Services;

public interface IOperationStatusCenter : INotifyPropertyChanged
{
    ObservableCollection<OperationToastItemViewModel> Toasts { get; }
    int ActiveOperationCount { get; }
    bool HasActiveOperations { get; }
    string ActiveOperationSummary { get; }

    IDisposable BeginOperation(string description);
    void ReportInfo(string message, bool isTransient = true);
    void ReportSuccess(string message, bool isTransient = true);
    void ReportError(string message, bool isTransient = false);
    void Dismiss(Guid toastId);
    void ClearTransient();
}
