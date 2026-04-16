using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = App.GetService<DashboardViewModel>();
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadDataCommand.ExecuteAsync(null);
    }
}
