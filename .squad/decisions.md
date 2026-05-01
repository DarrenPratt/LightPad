# Squad Decisions

## Active Decisions

### Initial MAUI project structure
- **Date:** 2026-04-30T17:44:30.414Z
- **Owner:** Ripley
- **Context:** LightPad needs a clean, implementation-ready starting point for Windows and Android, with iPad planned later.
- **Decision:** Use a standard MAUI single-project app at `src\LightPad.App` behind `LightPad.sln`, organized into `Views`, `ViewModels`, `Models`, and `Services`.
- **Consequences:** Team can begin feature slicing immediately with stable navigation and MVVM structure; iPad support can be added by expanding target frameworks while keeping the existing `Platforms\iOS` scaffold.

### LightPad v1 execution sequencing
- **Date:** 2026-04-30T18:56:47.023Z
- **Owner:** Ripley
- **Context:** The team needs an execution-ordered plan to move the current MAUI scaffold to a usable v1 on Windows Surface and Android tablets, with iPad readiness preserved.
- **Decision:** Execute v1 in phased slices: architecture/contracts, plain lightbox, trace mode, safe controls/settings, quality/CI hardening, and release readiness; treat animation/onion skin as post-v1 backlog.
- **Consequences:** Delivery stays aligned with MVP must-haves from the design doc, minimizes cross-platform risk early, and preserves a clean path to iPad enablement by keeping platform abstractions and iOS compile readiness in each phase.

### Platform Risk Assessment – May 2026
- **Date:** 2026-05-01T18:35:50.743+01:00
- **Owner:** Bishop
- **Context:** Code review of LightPad v1 MAUI scaffold identified 5 critical platform/performance risks blocking professional Surface Pen and Android tablet workflows. All risks have GitHub issues created (#11, #12, #13, #15, #17).
- **Decision:** Address in v1 planning: (1) Image memory management (#11) + App lifecycle (#13) as critical path (prevents crashes); (2) Input latency (#12) + Stylus data (#15) as high-value (enables professional workflows); (3) Performance instrumentation (#17) as quality enabler. All 5 scoped as follow-up features unless team decides to include before v1 release.
- **Consequences:** Estimated 2-3 weeks to address all risks. Enables professional Surface Pen + Android workflows. Requires expanded service layer (IPointerEventService, IImageMemoryService, IFrameTimingService) and lifecycle hooks in App.xaml.cs.

### UX Touch Workflow Friction Points – May 2026
- **Date:** 2026-05-01T18:35:50.743+01:00
- **Owner:** Hicks
- **Context:** UX review identified 5 friction points in touch workflows: gesture discovery, state management (undo), haptic feedback, control timing configurability, quick-access light presets.
- **Decision:** Prioritize by workflow impact: (1) High priority: gesture hints (#2) and quick presets (#10) (reduce confusion and workflow interruption); (2) Medium: haptics (#6) and control timing (#8) (improve reliability and ergonomics); (3) Nice-to-have: undo/redo (#4).
- **Consequences:** Improved user confidence and workflow speed on Surface and Android tablets. Consider bundling gesture hints + control improvements into a "UX Polish" sprint.

### Session Persistence Strategy – May 2026
- **Date:** 2026-05-01T18:35:50.743+01:00
- **Owner:** Ripley
- **Context:** Current session state (trace images, positions, zoom, animation frames) exists only in memory. Long workflows lose progress on crash, suspension, or power loss—unacceptable for artist workflows. Evaluated app-scoped local storage vs cloud sync vs platform preferences.
- **Decision:** Use app-scoped local file storage. Serialize TraceSessionState and AnimationSessionState to JSON; save to FileSystem.AppDataDirectory/sessions/; auto-save on state changes; load most recent session on startup; cap history to last 5 sessions.
- **Consequences:** Deterministic, offline-capable, user-controlled persistence. Aligns with v1 MVP scope. Easy to upgrade to cloud sync later if user demand justifies it. Enables artists to resume work safely across app crashes and device power loss.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
