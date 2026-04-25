using Microsoft.UI.Xaml.Controls;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class SyncPreviewPage : Page
{
    public SyncPreviewViewModel ViewModel { get; }

    public SyncPreviewPage()
    {
        ViewModel = App.GetService<SyncPreviewViewModel>();
        InitializeComponent();
    }
}
