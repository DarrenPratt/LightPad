using LightPad.App.Infrastructure;
using LightPad.App.ViewModels;

namespace LightPad.App.Views;

public partial class TracePage : ContentPage
{
    private readonly TraceViewModel _viewModel;

    public TracePage()
        : this(ServiceProviderHelper.GetRequiredService<TraceViewModel>())
    {
    }

    public TracePage(TraceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }

    protected override async void OnDisappearing()
    {
        await _viewModel.OnDisappearingAsync();
        base.OnDisappearing();
    }
}
