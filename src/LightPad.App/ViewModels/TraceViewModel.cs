using System.Threading.Tasks;
using LightPad.App.Models;
using LightPad.App.Services;

namespace LightPad.App.ViewModels;

public sealed class TraceViewModel : BaseViewModel
{
    private readonly IScreenWakeService _screenWakeService;

    public TraceViewModel(TraceSessionState sessionState, IScreenWakeService screenWakeService)
    {
        SessionState = sessionState;
        _screenWakeService = screenWakeService;
    }

    public TraceSessionState SessionState { get; }

    public string StatusText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SessionState.ActiveImage.FilePath))
            {
                return "Trace session state is wired and screen-awake is ready. Image import, pan, and zoom will land in Phase 3.";
            }

            return $"Loaded image: {SessionState.ActiveImage.FilePath}";
        }
    }

    public async Task OnAppearingAsync()
    {
        await _screenWakeService.ActivateAsync(nameof(TraceViewModel));
    }

    public async Task OnDisappearingAsync()
    {
        await _screenWakeService.DeactivateAsync(nameof(TraceViewModel));
    }
}
