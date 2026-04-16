using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class TutorialPage : Page
{
    public TutorialViewModel ViewModel { get; }

    public TutorialPage()
    {
        ViewModel = App.GetService<TutorialViewModel>();
        InitializeComponent();

        // Subscribe to QR code changes
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TutorialViewModel.QrCodeImage) && ViewModel.QrCodeImage != null)
            {
                LoadQrImage(ViewModel.QrCodeImage);
            }
        };
    }

    private async void LoadQrImage(byte[] pngBytes)
    {
        var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        stream.Seek(0);

        var bitmap = new BitmapImage();
        await bitmap.SetSourceAsync(stream);
        QrCodeImage.Source = bitmap;
    }
}
