using CoworkingMobile.Models;
using CoworkingMobile.PageModels;

namespace CoworkingMobile.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}