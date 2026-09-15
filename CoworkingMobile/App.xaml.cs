using Microsoft.Extensions.DependencyInjection;

namespace CoworkingMobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);
        
        CheckAuth(shell);
        
        return window;
    }

    private async void CheckAuth(Shell shell)
    {
        var token = await SecureStorage.Default.GetAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            await shell.GoToAsync("//login");
        }
    }
}