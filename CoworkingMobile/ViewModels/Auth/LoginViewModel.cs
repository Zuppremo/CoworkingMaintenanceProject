using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CoworkingMobile.Services;

namespace CoworkingMobile.ViewModels.Auth;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly AuthService authService;

    private string email = string.Empty;
    public string Email
    {
        get => email;
        set { email = value; OnPropertyChanged(); }
    }

    private string password = string.Empty;
    public string Password
    {
        get => password;
        set { password = value; OnPropertyChanged(); }
    }

    private bool isBusy;
    public bool IsBusy
    {
        get => isBusy;
        set { isBusy = value; OnPropertyChanged(); }
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(AuthService authService)
    {
        this.authService = authService;
        LoginCommand = new Command(async () => await OnLoginAsync());
    }

    private async Task OnLoginAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Shell.Current.DisplayAlert("Error", "Por favor ingresa correo y contraseña.", "OK");
            return;
        }

        IsBusy = true;

        var success = await authService.LoginAsync(Email, Password);

        IsBusy = false;

        if (success)
        {
            await Shell.Current.GoToAsync("//main");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "Credenciales incorrectas o problemas de conexión.", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
