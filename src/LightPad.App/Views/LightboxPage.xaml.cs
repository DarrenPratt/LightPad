using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class LightboxPage : ContentPage
{
    public LightboxPage()
        : this(ServiceProviderHelper.GetRequiredService<LightboxViewModel>())
    {
    }

    public LightboxPage(LightboxViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
