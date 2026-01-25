using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TeachAssistApp.Views;

public partial class LoginView : Page
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LoginViewModel viewModel)
        {
            viewModel.Password = ((PasswordBox)sender).Password;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is ViewModels.LoginViewModel viewModel)
            {
                viewModel.LoginCommand.Execute(null);
            }
        }
    }
}
