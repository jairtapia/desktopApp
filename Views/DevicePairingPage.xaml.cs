using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using DesktopAssistant.ViewModels;

namespace DesktopAssistant.Views;

public sealed partial class DevicePairingPage : Page
{
    public DevicePairingViewModel ViewModel { get; }

    public DevicePairingPage()
    {
        ViewModel = App.GetService<DevicePairingViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DevicePairingViewModel.QrCodeImage) && ViewModel.QrCodeImage != null)
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

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Optionally auto-generate QR on page load
    }
}
