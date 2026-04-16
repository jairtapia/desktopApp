using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Services;
using DesktopAssistant.Helpers;

namespace DesktopAssistant.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IWebSocketService _webSocketService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    public LoginViewModel(IAuthService authService, IWebSocketService webSocketService, NavigationService navigationService)
    {
        _authService = authService;
        _webSocketService = webSocketService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter email and password";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var result = await _authService.LoginAsync(Email, Password);

        IsLoading = false;

        if (result.Success && result.Data != null)
        {
            // Initialize WebSocket connection
            await _webSocketService.ConnectAsync(SecureStorage.GetWsBaseUrl(), result.Data.Token);

            // Check if tutorial is completed
            if (!Helpers.SecureStorage.IsTutorialCompleted())
            {
                _navigationService.NavigateRoot(typeof(Views.TutorialPage));
            }
            else
            {
                _navigationService.NavigateRoot(typeof(Views.ShellPage));
            }
        }
        else
        {
            ErrorMessage = result.Message ?? "Login failed. Please try again.";
            HasError = true;
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        _navigationService.NavigateRoot(typeof(Views.RegisterPage));
    }
}
