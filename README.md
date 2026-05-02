# LightPad

LightPad is a .NET MAUI app scaffold for a cross-platform digital lightbox with Android, Windows, and iOS targets in the project.

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

## Build (iPad/iPhone)

The project now includes a real `net10.0-ios` target, but building or publishing it still requires:

- macOS
- Xcode
- the .NET MAUI iOS workload
- Apple signing/provisioning

Example from a Mac:

```powershell
dotnet build src/LightPad.App/LightPad.App.csproj -f net10.0-ios
```

## Run (Windows)

```powershell
dotnet run --project src\LightPad.App\LightPad.App.csproj -f net10.0-windows10.0.19041.0
```

## Android note

Android target scaffolding is included in the project. Building Android requires a local Android SDK + Java installation in addition to the MAUI workload.

## iPad note

See [docs/ipad_readiness_checklist.md](docs/ipad_readiness_checklist.md) for the remaining work to make the current app genuinely iPad-ready rather than only iOS-targetable.
