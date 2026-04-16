using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class DevPage : Page
{
    public DevViewModel ViewModel { get; }

    public DevPage()
    {
        ViewModel = App.GetService<DevViewModel>();
        InitializeComponent();
    }
}
