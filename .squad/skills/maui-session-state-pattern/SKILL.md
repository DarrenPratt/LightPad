# MAUI Session State Pattern

**Date:** 2026-05-01T18:35:50.743+01:00
**Owner:** Ripley
**Applied In:** LightPad (TraceSessionState, AnimationSessionState, LightboxSettingsState)

## Problem

Cross-platform MAUI apps often need to preserve complex UI state (image positions, zoom, user selections) across navigation, background suspension, or app restarts. MAUI's built-in `Preferences` API is too limited for rich state objects, and ViewModel properties alone don't survive process death.

## Solution

Create **singleton session state classes** that:
1. Encapsulate all stateful data for a feature (e.g., TraceSessionState for trace mode)
2. Are registered in DI as `AddSingleton<T>()`
3. Wrap immutable model objects (e.g., `TraceImageState`)
4. Are injected into ViewModels, which bind UI to session state
5. Can be serialized/deserialized for persistence

## Pattern Structure

```csharp
// Models/TraceImageState.cs - immutable data contract
public class TraceImageState
{
    public string FilePath { get; init; }
    public double OffsetX { get; init; }
    public double OffsetY { get; init; }
    public double Zoom { get; init; }
    public double Opacity { get; init; }
    public double RotationDegrees { get; init; }
}

// Models/TraceSessionState.cs - mutable session container (singleton)
public class TraceSessionState
{
    public TraceImageState ActiveImage { get; private set; }
    
    public void UpdateImage(TraceImageState newState)
    {
        ActiveImage = newState;
        OnSessionChanged?.Invoke();
    }
    
    public event Action OnSessionChanged;
}

// ViewModels/TraceViewModel.cs
public class TraceViewModel : BaseViewModel
{
    private readonly TraceSessionState _sessionState;
    
    public TraceViewModel(TraceSessionState sessionState, ...)
    {
        _sessionState = sessionState;
        // Bind UI commands to session state mutations
    }
}

// MauiProgram.cs - register as singleton
builder.Services.AddSingleton<TraceSessionState>();
builder.Services.AddTransient<TraceViewModel>();
```

## Benefits

- **State survives navigation**: ViewModels are transient, but session state is singleton → doesn't reset on page navigation
- **Single source of truth**: No duplicate state in ViewModels or Views
- **Serializable**: Easy to persist to storage or export
- **Testable**: Inject mock session state into ViewModels
- **Scales well**: Add new session state classes as features grow (e.g., AnimationSessionState, LayerSessionState)

## Usage Notes

- Mark session state classes as `sealed` to prevent accidental inheritance
- Use immutable models (records or init-only properties) for data classes
- Expose change events (e.g., `OnSessionChanged`) for reactive UI updates
- For complex nested state, consider using a state management library (Redux, Flux) in later slices

## Example: Auto-Save Pattern

```csharp
public class TraceViewModel : BaseViewModel
{
    public TraceViewModel(TraceSessionState sessionState, ...)
    {
        _sessionState = sessionState;
        _sessionState.OnSessionChanged += AutoSaveSession;
    }
    
    private async void AutoSaveSession()
    {
        var json = JsonConvert.SerializeObject(_sessionState);
        await File.WriteAllTextAsync(sessionFilePath, json);
    }
}
```

## References

- LightPad: `src/LightPad.App/Models/TraceSessionState.cs`
- MAUI DI: https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/dependency-injection
