using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
        : this(ServiceProviderHelper.GetRequiredService<MainViewModel>())
    {
    }

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
