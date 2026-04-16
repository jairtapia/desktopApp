using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Helpers;
using DesktopAssistant.Models;
using DesktopAssistant.Services;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DesktopAssistant.ViewModels;

public partial class TutorialViewModel : ObservableObject
{
    private readonly IAppScannerService _appScanner;
    private readonly IDevicePairingService _devicePairing;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    public partial int CurrentStep { get; set; }

    [ObservableProperty]
    public partial int TotalSteps { get; set; } = 4;

    [ObservableProperty]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    public partial int AppsFound { get; set; }

    [ObservableProperty]
    public partial bool ScanCompleted { get; set; }

    [ObservableProperty]
    public partial bool PermissionsGranted { get; set; }

    [ObservableProperty]
    public partial byte[]? QrCodeImage { get; set; }

    [ObservableProperty]
    public partial string PairCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LocalIp { get; set; } = string.Empty;

    public ObservableCollection<AppInfo> ScannedApps { get; } = new();

    public TutorialViewModel(
        IAppScannerService appScanner,
        IDevicePairingService devicePairing,
        NavigationService navigationService)
    {
        _appScanner = appScanner;
        _devicePairing = devicePairing;
        _navigationService = navigationService;
        CurrentStep = 0;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < TotalSteps - 1)
        {
            CurrentStep++;
            if (CurrentStep == 3) // Device pairing step
            {
                GenerateQrCode();
            }
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
        }
    }

    [RelayCommand]
    private void GrantPermissions()
    {
        PermissionsGranted = true;
    }

    [RelayCommand]
    private async Task ScanAppsAsync()
    {
        IsScanning = true;
        ScannedApps.Clear();

        var apps = await _appScanner.GetAllAppsAsync();

        foreach (var app in apps)
        {
            ScannedApps.Add(app);
        }

        AppsFound = apps.Count;
        IsScanning = false;
        ScanCompleted = true;
    }

    [RelayCommand]
    private void GenerateQrCode()
    {
        var pairInfo = _devicePairing.GeneratePairInfo();
        QrCodeImage = _devicePairing.GenerateQrCode(pairInfo);
        PairCode = TokenHelper.GenerateShortCode();
        LocalIp = pairInfo.LocalIp;
    }

    [RelayCommand]
    private void CompleteTutorial()
    {
        SecureStorage.SetTutorialCompleted(true);
        _navigationService.NavigateRoot(typeof(Views.ShellPage));
    }

    [RelayCommand]
    private void SkipTutorial()
    {
        SecureStorage.SetTutorialCompleted(true);
        _navigationService.NavigateRoot(typeof(Views.ShellPage));
    }
}
