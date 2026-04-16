using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class LoginPage : Page
{
    public LoginViewModel ViewModel { get; }

    public LoginPage()
    {
        ViewModel = App.GetService<LoginViewModel>();
        InitializeComponent();
    }
}
