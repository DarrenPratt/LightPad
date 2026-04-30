using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class AnimationPage : ContentPage
{
    public AnimationPage()
        : this(ServiceProviderHelper.GetRequiredService<AnimationViewModel>())
    {
    }

    public AnimationPage(AnimationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
