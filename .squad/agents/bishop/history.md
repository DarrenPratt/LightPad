# Project Context

- **Owner:** Jynx_Protocol
- **Project:** LightPad
- **Stack:** .NET MAUI (C#), Windows Surface, Android tablets, optional iPad later
- **Description:** A cross-platform digital lightbox app for tracing, drawing, and animation workflows.
- **Created:** 2026-04-30T17:38:50.646Z

## Learnings

- Initial platform ownership assigned for integration and performance work.
- **2026-05-01:** Code review identified 5 critical platform/performance risks:
  1. Image memory management (OOM risk on tablets with large reference images)
  2. Touch/stylus input latency optimization (1-5ms latency critical for Surface Pen)
  3. App lifecycle resource management (suspend/resume/terminate cleanup)
  4. Pen/stylus pressure and tilt data capture (required for professional workflows)
  5. Performance instrumentation framework (no current metrics/telemetry)
- **Architecture observations:**
  - SkiaSharp 3.119.2 handles complex rendering (pan, zoom, rotate, opacity) but no performance metrics
  - ScreenWakeService properly abstracts platform wake-lock APIs (DisplayRequest Windows, WindowManagerFlags Android)
  - ImagePickerService caches to FileSystem.CacheDirectory but lacks memory bounds or LRU eviction
  - Animation mode supports onion-skin overlays (multiple frame rendering) but no tested on low-RAM devices
  - MVVM structure clean; state containers (TraceImageState, AnimationSessionState) are POCOs
- **Key file paths:**
  - Services: `src/LightPad.App/Services/` (IScreenWakeService, IImagePickerService, ISettingsService)
  - Platform-specific: `src/LightPad.App/Platforms/Windows` (Package.appxmanifest, app.manifest)
  - Views use large SkiaSharp canvases in LightboxPage, TracePage, AnimationPage
- **Design doc alignment:** MVP focuses on basic lightbox + trace + lock controls. Performance and advanced input not yet scoped.

## Team Updates

### 2026-05-01T17:35:50Z (Scribe Decision Merge)
- Scribe synchronized **Platform Risk Assessment** decision into active decisions ledger.
- Decision confirmed: sequenced critical path (image memory #11 + lifecycle #13), high-value (input latency #12 + stylus data #15), and quality (instrumentation #17).
- Estimated 2-3 weeks to address all risks. Requires expanded service layer (IPointerEventService, IImageMemoryService, IFrameTimingService) and lifecycle hooks.
- Cross-team decisions now merged: Session Persistence Strategy (Ripley), UX Touch Workflow (Hicks) also entered ledger.
- Decision inbox cleared (3 items processed).
