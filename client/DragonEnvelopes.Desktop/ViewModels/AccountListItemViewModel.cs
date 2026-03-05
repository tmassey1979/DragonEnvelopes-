namespace DragonEnvelopes.Desktop.ViewModels;

public sealed class AccountListItemViewModel
{
    public AccountListItemViewModel(Guid id, string name, string type, string balance)
    {
        Id = id;
        Name = name;
        Type = type;
        Balance = balance;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Type { get; }

    public string Balance { get; }
}
