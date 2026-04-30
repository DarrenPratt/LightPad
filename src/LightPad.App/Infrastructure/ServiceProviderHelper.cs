using Microsoft.Extensions.DependencyInjection;

namespace LightPad.App.Infrastructure;

internal static class ServiceProviderHelper
{
    public static IServiceProvider Services { get; set; } = default!;

    public static T GetRequiredService<T>()
        where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
}
