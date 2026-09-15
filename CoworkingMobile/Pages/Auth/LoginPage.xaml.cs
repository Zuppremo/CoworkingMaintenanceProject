using CoworkingMobile.ViewModels.Auth;

namespace CoworkingMobile.Pages.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
