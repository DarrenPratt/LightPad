using LightPad.App.Infrastructure;
using LightPad.App.Models;
using LightPad.App.Services;
using LightPad.App.ViewModels;
using LightPad.App.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace LightPad.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<ISettingsService, SettingsService>();
		builder.Services.AddSingleton<IImagePickerService, ImagePickerService>();
		builder.Services.AddSingleton<IScreenWakeService, ScreenWakeService>();
		builder.Services.AddSingleton<TraceSessionState>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<LightboxViewModel>();
		builder.Services.AddTransient<TraceViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<AnimationViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<LightboxPage>();
		builder.Services.AddTransient<TracePage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<AnimationPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		ServiceProviderHelper.Services = app.Services;
		return app;
	}
}
