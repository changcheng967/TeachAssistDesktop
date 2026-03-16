using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TeachAssistApp.Services;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ITeachAssistService _teachAssistService;
    private readonly ICredentialService _credentialService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(
        ITeachAssistService teachAssistService,
        ICredentialService credentialService,
        INavigationService navigationService)
    {
        _teachAssistService = teachAssistService;
        _credentialService = credentialService;
        _navigationService = navigationService;

        // Load saved credentials on initialization
        LoadSavedCredentials();
    }

    public async Task LoadSavedCredentialsAsync()
    {
        var (username, password) = await _credentialService.GetCredentialsAsync();
        if (username != null && password != null)
        {
            Username = username;
            Password = password;
            RememberMe = true;
        }
        else
        {
            // Clear fields if no saved credentials (after logout)
            Username = string.Empty;
            Password = string.Empty;
            RememberMe = false;
        }
        ErrorMessage = null;
    }

    private async void LoadSavedCredentials()
    {
        await LoadSavedCredentialsAsync();
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both username and password.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var success = await _teachAssistService.LoginAsync(Username, Password);

            if (success)
            {
                // Save credentials if remember me is checked
                await _credentialService.SaveCredentialsAsync(Username, Password, RememberMe);

                // Navigate to dashboard
                _navigationService.NavigateTo("Dashboard");
            }
            else
            {
                // Show the service's error message
                ErrorMessage = _teachAssistService.LastError ?? "Invalid username or password. Please try again.";
                System.Diagnostics.Debug.WriteLine($"Login failed. Error: {_teachAssistService.LastError}");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Login exception: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = null;
    }
}
