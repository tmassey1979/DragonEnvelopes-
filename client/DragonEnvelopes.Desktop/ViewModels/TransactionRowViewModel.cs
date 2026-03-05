using System.Windows.Input;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class TransactionRowViewModel
{
    public TransactionRowViewModel(
        string date,
        string merchant,
        string amount,
        string envelope,
        string category,
        bool isSelected = false,
        bool isEdited = false,
        bool isFlagged = false,
        ICommand? editCommand = null,
        ICommand? splitCommand = null)
    {
        Date = date;
        Merchant = merchant;
        Amount = amount;
        Envelope = envelope;
        Category = category;
        IsSelected = isSelected;
        IsEdited = isEdited;
        IsFlagged = isFlagged;
        EditCommand = editCommand;
        SplitCommand = splitCommand;
    }

    public string Date { get; }

    public string Merchant { get; }

    public string Amount { get; }

    public string Envelope { get; }

    public string Category { get; }

    public bool IsSelected { get; }

    public bool IsEdited { get; }

    public bool IsFlagged { get; }

    public ICommand? EditCommand { get; }

    public ICommand? SplitCommand { get; }
}
