using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        InitializeComponent();
    }
}
