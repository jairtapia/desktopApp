using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class RegisterPage : Page
{
    public RegisterViewModel ViewModel { get; }

    public RegisterPage()
    {
        ViewModel = App.GetService<RegisterViewModel>();
        InitializeComponent();
    }
}
