using CoworkingMobile.ViewModels;

namespace CoworkingMobile.Pages;

public partial class MainPage : ContentPage
{
    private readonly CleaningDashboardViewModel viewModel;

    public MainPage(CleaningDashboardViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.InitializeAsync();
    }
}