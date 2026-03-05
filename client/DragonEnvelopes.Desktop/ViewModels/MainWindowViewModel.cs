using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DragonEnvelopes.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel()
    {
        NavigationItems =
        [
            new NavigationItemViewModel(
                key: "dashboard",
                label: "Dashboard",
                glyph: "\uE80F",
                content: new ShellContentViewModel(
                    "Budget Health Overview",
                    "Track allocation health, available cash, and month-to-date progress for your family.",
                    "Dashboard widgets will appear here",
                    "Connect real account and envelope data to populate KPI cards.")),
            new NavigationItemViewModel(
                key: "envelopes",
                label: "Envelopes",
                glyph: "\uE713",
                content: new ShellContentViewModel(
                    "Envelope Planning",
                    "Organize spending buckets and monthly targets across your household categories.",
                    "Envelope list is not loaded yet",
                    "Once envelope APIs are wired, this region will host list and edit views.")),
            new NavigationItemViewModel(
                key: "transactions",
                label: "Transactions",
                glyph: "\uE8A7",
                content: new ShellContentViewModel(
                    "Transaction Activity",
                    "Review posted spending, categorize activity, and route expenses into envelopes.",
                    "Transaction feed is not connected",
                    "Upcoming tasks will bind this region to API-backed transaction pages."))
        ];

        NavigateCommand = new RelayCommand<NavigationItemViewModel?>(Navigate);
        Navigate(NavigationItems[0]);
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public IRelayCommand<NavigationItemViewModel?> NavigateCommand { get; }

    [ObservableProperty]
    private string topBarTitle = "Dashboard";

    [ObservableProperty]
    private string topBarSubtitle = "Family budget shell";

    [ObservableProperty]
    private ShellContentViewModel? currentContent;

    private void Navigate(NavigationItemViewModel? selectedItem)
    {
        if (selectedItem is null)
        {
            return;
        }

        foreach (var item in NavigationItems)
        {
            item.IsSelected = ReferenceEquals(item, selectedItem);
        }

        TopBarTitle = selectedItem.Label;
        TopBarSubtitle = "Shell region ready for page binding";
        CurrentContent = selectedItem.Content;
    }
}
