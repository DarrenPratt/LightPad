using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class TracePage : ContentPage
{
    public TracePage()
        : this(ServiceProviderHelper.GetRequiredService<TraceViewModel>())
    {
    }

    public TracePage(TraceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
