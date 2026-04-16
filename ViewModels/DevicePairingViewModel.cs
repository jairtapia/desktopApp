using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Helpers;
using DesktopAssistant.Models;
using DesktopAssistant.Services;

namespace DesktopAssistant.ViewModels;

public partial class DevicePairingViewModel : ObservableObject
{
    private readonly IDevicePairingService _pairingService;
    private readonly NavigationService _navigationService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    public partial byte[]? QrCodeImage { get; set; }

    [ObservableProperty]
    public partial string PairCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LocalIp { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DeviceName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsWaitingForDevice { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Generate a QR code to pair your device";

    [ObservableProperty]
    public partial int ExpiresInSeconds { get; set; } = 300;

    public DevicePairingViewModel(
        IDevicePairingService pairingService,
        NavigationService navigationService,
        IAuthService authService)
    {
        _pairingService = pairingService;
        _navigationService = navigationService;
        _authService = authService;
        DeviceName = NetworkHelper.GetHostName();
    }

    [RelayCommand]
    private async Task GenerateQrCode()
    {
        var pairInfo = _pairingService.GeneratePairInfo();
        QrCodeImage = _pairingService.GenerateQrCode(pairInfo);
        
        string code = TokenHelper.GenerateShortCode();
        PairCode = code;
        
        StatusMessage = "Syncing code with server...";
        bool registered = await _authService.RegisterPairingCodeAsync(code);
        
        if (registered)
        {
            LocalIp = pairInfo.LocalIp;
            IsWaitingForDevice = true;
            StatusMessage = "Scan this QR code with your mobile device";
            ExpiresInSeconds = 300;
        }
        else
        {
            StatusMessage = "Error: Could not sync with server. Check connection.";
        }
    }

    [RelayCommand]
    private void RefreshQrCode()
    {
        GenerateQrCode();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
