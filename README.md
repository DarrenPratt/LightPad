# LightPad

LightPad is a .NET MAUI app scaffold for a cross-platform digital lightbox (Windows + Android first, iPad later).

## Repository structure

- `LightPad.sln` - solution entry point
- `src\LightPad.App` - MAUI single-project app
  - `Views\` - starter screens and navigation targets
  - `ViewModels\` - MVVM view models
  - `Models\` - app state/data models
  - `Services\` - platform-agnostic app services
  - `Platforms\Android`, `Platforms\Windows`, `Platforms\iOS` - platform launch scaffolding

## Build

From `D:\Projects\LightPad`:

```powershell
dotnet build LightPad.sln -f net10.0-windows10.0.19041.0
```

## Run (Windows)

```powershell
dotnet run --project src\LightPad.App\LightPad.App.csproj -f net10.0-windows10.0.19041.0
```

## Android note

Android target scaffolding is included in the project. Building Android requires a local Android SDK + Java installation in addition to the MAUI workload.
