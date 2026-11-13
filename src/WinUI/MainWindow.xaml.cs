using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HushLittlePC.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "Hush Little PC";
        Navigation.SelectedItem = Navigation.MenuItems[0];
        ContentFrame.Navigate(typeof(SettingsPage));
    }

    private void Navigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item)
        {
            return;
        }

        var pageType = item.Tag?.ToString() switch
        {
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => null
        };

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
