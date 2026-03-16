using System;
using System.Threading.Tasks;

namespace TeachAssistApp.Helpers;

public interface INavigationService
{
    event Action<string>? OnNavigate;
    void NavigateTo(string viewName);
    Task NavigateToAsync(string viewName);
}

public class NavigationService : INavigationService
{
    public event Action<string>? OnNavigate;

    public void NavigateTo(string viewName)
    {
        OnNavigate?.Invoke(viewName);
    }

    public Task NavigateToAsync(string viewName)
    {
        OnNavigate?.Invoke(viewName);
        return Task.CompletedTask;
    }
}
