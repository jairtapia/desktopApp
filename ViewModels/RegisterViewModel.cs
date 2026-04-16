using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopAssistant.Services;

namespace DesktopAssistant.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly NavigationService _navigationService;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool HasError { get; set; }

    public RegisterViewModel(IAuthService authService, NavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email)
            || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please fill in all fields";
            HasError = true;
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            HasError = true;
            return;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var result = await _authService.RegisterAsync(Email, Password, Name);

        IsLoading = false;

        if (result.Success)
        {
            _navigationService.NavigateRoot(typeof(Views.TutorialPage));
        }
        else
        {
            ErrorMessage = result.Message ?? "Registration failed. Please try again.";
            HasError = true;
        }
    }

    [RelayCommand]
    private void GoToLogin()
    {
        _navigationService.NavigateRoot(typeof(Views.LoginPage));
    }
}
