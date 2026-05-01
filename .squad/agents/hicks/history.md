# Project Context

- **Owner:** Jynx_Protocol
- **Project:** LightPad
- **Stack:** .NET MAUI (C#), Windows Surface, Android tablets, optional iPad later
- **Description:** A cross-platform digital lightbox app for tracing, drawing, and animation workflows.
- **Created:** 2026-04-30T17:38:50.646Z

## Learnings

- Initial UI ownership assigned for MAUI interaction and experience design.
- **Architecture:** MVVM structure with solid separation between Views (XAML), ViewModels, Models, and Services. SkiaSharp used for advanced image rendering (zoom, rotation) in Trace/Animation modes.
- **Touch-first design considerations:** LightboxPage and TracePage use tap gesture recognition for lock toggling. Auto-hide pattern implemented but timing may need refinement. Surface Pen and stylus support require careful gesture handling to avoid accidental triggers during precision work.
- Trace rotation is driven by TraceViewModel.RotationAngle, persisted in TraceImageState.Rotation, and rendered via SkiaSharp canvas rotation in TracePage.
- **UX gaps identified (2026-05-01):** 
  - No visual gesture hints on first launch (users discover pinch/zoom/rotate by trial)
  - Missing undo/redo in Trace mode for image state changes
  - No haptic feedback for lock/unlock transitions (subtle on touch devices)
  - Control auto-hide timing lacks user configurability
  - No quick-access preset swatches in Trace mode for rapid light adjustments
- **GitHub issues created:** #2 (gestures), #4 (undo/redo), #6 (haptics), #8 (auto-hide), #10 (presets)
- **Project state:** MVP features mostly complete; focus now on UX polish and discoverability for touch workflows.

## Team Updates

### 2026-05-01T17:35:50Z (Scribe Decision Merge)
- Scribe synchronized **UX Touch Workflow Friction Points** decision into active decisions ledger.
- Decision confirmed: prioritize gesture hints (#2) + quick presets (#10) as high-value; haptics (#6) + control timing (#8) as medium; undo/redo (#4) as nice-to-have.
- Cross-team decisions now merged: Session Persistence Strategy (Ripley), Platform Risk Assessment (Bishop) also entered ledger.
- Decision inbox cleared (3 items processed).
