using CoworkingMobile.ViewModels;

namespace CoworkingMobile.Pages;

public partial class MainPage : ContentPage
{
    public CleaningDashboardViewModel ViewModel { get; }

    public MainPage(CleaningDashboardViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var token = await SecureStorage.Default.GetAsync("authToken");
        if (string.IsNullOrEmpty(token))
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        await ViewModel.InitializeAsync();
    }
}