using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using TeachAssistApp.Helpers;

namespace TeachAssistApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableObject? _currentViewModel;

    [ObservableProperty]
    private string _currentView = "Login";

    public MainViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.OnNavigate += NavigateToView;

        // Start at login view
        CurrentView = "Login";
    }

    partial void OnCurrentViewChanged(string value)
    {
        // Update current view model based on navigation
        // This will be handled by the view's data context
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        _navigationService.NavigateTo(viewName);
        CurrentView = viewName;
    }

    private void NavigateToView(string viewName)
    {
        CurrentView = viewName;
    }
}
