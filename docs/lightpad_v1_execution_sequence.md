# LightPad v1 Execution Sequence

## Purpose

This document turns the design notes in `maui_lightbox_app_design.md` into an execution order for shipping **LightPad v1**.

The current repository is a scaffold:

- navigation exists
- `SettingsService` exists for persisted brightness and colour temperature
- `LightboxPage`, `TracePage`, and `SettingsPage` are still placeholders
- trace state model exists, but trace workflows and platform services do not

Because of that, v1 should be built in layers, starting with the smallest usable workflow on Windows and then hardening the shared app flow before Android validation.

## v1 Outcome

Ship a usable MVP with:

- fullscreen white lightbox
- overlay-based brightness and colour controls
- image import and display
- pan and zoom for a single trace image
- image opacity control
- lock mode to reduce accidental touches
- keep-screen-awake behavior

Do not block v1 on:

- onion skin mode
- grid overlay
- multi-image layers
- save/load sessions

## Sequence

### Phase 0: Stabilize app shell and shared app state

Status: Complete on 2026-04-30.

Goal: make the scaffold ready for feature work without changing behavior much.

Work:

- confirm route registration and page navigation in `AppShell`
- add missing view models for `LightboxPage`, `TracePage`, and `SettingsPage`
- extend `ISettingsService` and `SettingsService` only for settings needed by v1
- define a small shared state shape for lightbox settings and trace session state

Likely files:

- `src/LightPad.App/AppShell.xaml`
- `src/LightPad.App/AppShell.xaml.cs`
- `src/LightPad.App/ViewModels/`
- `src/LightPad.App/Services/ISettingsService.cs`
- `src/LightPad.App/Services/SettingsService.cs`
- `src/LightPad.App/Models/TraceImageState.cs`

Exit criteria:

- every top-level page has a view model
- settings read/write works through a single service boundary
- app navigation is stable enough to support iterative feature work

### Phase 1: Deliver the plain lightbox first

Status: Complete on 2026-04-30.

Goal: ship the smallest valuable workflow.

Work:

- implement `LightboxPage` as a real fullscreen light surface
- support white, warm, cool, and custom colour presets
- simulate brightness using overlay logic rather than hardware brightness
- wire brightness and colour temperature to persisted settings
- add a minimal lock/unlock mechanism

Likely files:

- `src/LightPad.App/Views/LightboxPage.xaml`
- `src/LightPad.App/Views/LightboxPage.xaml.cs`
- `src/LightPad.App/ViewModels/LightboxViewModel.cs`
- `src/LightPad.App/Services/ISettingsService.cs`
- `src/LightPad.App/Services/SettingsService.cs`

Exit criteria:

- user can open lightbox mode from the home screen
- screen visually behaves as a lightbox
- brightness and colour choices persist across app restarts
- lock mode prevents accidental control changes

### Phase 2: Add screen-awake and immersive behavior

Status: Complete on 2026-04-30.

Goal: make lightbox mode practical during real use.

Work:

- introduce a `ScreenWakeService` abstraction
- implement Windows and Android keep-awake behavior
- add auto-hide controls for immersive use
- define unlock behavior that works with touch-heavy usage

Likely files:

- `src/LightPad.App/Services/IScreenWakeService.cs`
- `src/LightPad.App/Services/ScreenWakeService.cs` or platform-specific implementations
- `src/LightPad.App/Platforms/Windows/`
- `src/LightPad.App/Platforms/Android/`
- `src/LightPad.App/Views/LightboxPage.xaml`
- `src/LightPad.App/ViewModels/LightboxViewModel.cs`

Exit criteria:

- app prevents screen sleep while active in lightbox/trace workflows
- controls can be hidden during use
- unlock behavior is deliberate and not easy to trigger accidentally

### Phase 3: Build single-image trace mode

Status: Complete on 2026-04-30.

Goal: complete the second major v1 workflow.

Work:

- add image import from device storage
- render a single trace image in `TracePage`
- support pan and zoom
- support opacity changes
- support image position lock
- keep the implementation single-image only for v1

Likely files:

- `src/LightPad.App/Views/TracePage.xaml`
- `src/LightPad.App/Views/TracePage.xaml.cs`
- `src/LightPad.App/ViewModels/TraceViewModel.cs`
- `src/LightPad.App/Models/TraceImageState.cs`
- `src/LightPad.App/Services/IImagePickerService.cs`
- `src/LightPad.App/Services/ImagePickerService.cs`

Notes:

- if MAUI controls are too limiting for smooth gesture behavior, introduce `SkiaSharp` here rather than earlier
- avoid bringing in animation/onion-skin logic during this phase

Exit criteria:

- user can load one image
- user can move and scale it reliably with touch or mouse
- opacity can be adjusted live
- image lock mode prevents unintended movement

### Phase 4: Unify settings and trace controls

Status: Complete on 2026-04-30.

Goal: make the two main workflows feel like one product rather than two demos.

Work:

- implement the `SettingsPage` for persisted defaults
- decide which settings are global defaults vs page-local session state
- align control language and layout between lightbox and trace mode
- ensure trace mode can reuse lightbox brightness/colour settings where appropriate

Likely files:

- `src/LightPad.App/Views/SettingsPage.xaml`
- `src/LightPad.App/Views/SettingsPage.xaml.cs`
- `src/LightPad.App/ViewModels/SettingsViewModel.cs`
- `src/LightPad.App/Services/ISettingsService.cs`
- `src/LightPad.App/Services/SettingsService.cs`

Exit criteria:

- settings page controls real persisted behavior
- duplicate control logic is reduced
- default values are clear and predictable

### Phase 5: Windows-first usability pass

Goal: make the MVP actually usable on the primary validation platform.

Work:

- validate layout on Surface-size screens
- test touch and pen interactions
- verify lock flow, hidden controls, and wake-lock behavior
- tighten spacing, control sizes, and orientation behavior
- fix any interaction conflicts between gestures and locked state

Focus areas:

- `Views/`
- `ViewModels/`
- Windows platform project files as needed

Exit criteria:

- Windows build is stable for repeated manual use
- touch-first interactions are acceptable
- there are no obvious workflow blockers for tracing on Surface hardware

### Phase 6: Android validation and parity fixes

Goal: confirm the shared implementation survives the second target platform.

Work:

- build and run on an Android tablet
- validate file picking, gestures, wake-lock behavior, and fullscreen behavior
- fix platform-specific layout and lifecycle issues
- keep feature scope fixed; this phase is for parity, not expansion

Focus areas:

- `src/LightPad.App/Platforms/Android/`
- any shared gesture or lifecycle code discovered during validation

Exit criteria:

- Android can complete the same v1 workflows as Windows
- any platform-specific differences are documented and acceptable

## Recommended Backlog Order

If work is being done issue-by-issue, the order should be:

1. Shared view models and settings boundary
2. Real `LightboxPage`
3. Screen wake + lock/immersive behavior
4. Real `TracePage` with single-image import
5. `SettingsPage`
6. Windows usability pass
7. Android parity pass

## Explicit Deferrals

Keep these out of the v1 critical path:

- `AnimationPage` implementation beyond placeholder navigation
- onion skin overlays
- grid overlay
- rotation controls if pan/zoom is not yet solid
- multi-frame and multi-layer state
- export and cloud features

## Definition of Done for v1

LightPad v1 is ready when:

- a user can open the app and immediately use plain lightbox mode
- a user can import one reference image and trace over it
- brightness/colour defaults persist
- lock mode and keep-awake behavior support real tracing use
- Windows is validated first and Android is at functional parity

At that point, `AnimationPage` and advanced overlays can move into v2 planning.
