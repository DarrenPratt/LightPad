using LightPad.App.Models;

namespace LightPad.App.ViewModels;

public sealed class TraceViewModel : BaseViewModel
{
    public TraceViewModel(TraceSessionState sessionState)
    {
        SessionState = sessionState;
    }

    public TraceSessionState SessionState { get; }

    public string StatusText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SessionState.ActiveImage.FilePath))
            {
                return "Trace session state is wired. Image import, pan, and zoom will land in Phase 3.";
            }

            return $"Loaded image: {SessionState.ActiveImage.FilePath}";
        }
    }
}
