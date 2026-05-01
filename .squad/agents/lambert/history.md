# Project Context

- **Owner:** Jynx_Protocol
- **Project:** LightPad
- **Stack:** .NET MAUI (C#), Windows Surface, Android tablets, optional iPad later
- **Description:** A cross-platform digital lightbox app for tracing, drawing, and animation workflows.
- **Created:** 2026-04-30T17:38:50.646Z

## Learnings

- Initial QA ownership assigned for test planning and coverage.

### 2026-05-01T18:35:50 — Testing Gap Analysis and Issue Creation

**Key Findings:**
- **No unit tests exist** in the project; critical ViewModels (TraceViewModel, LightboxViewModel, AnimationViewModel, BaseViewModel) have untested state management and property binding logic
- **No integration tests** for platform services (IScreenWakeService, IImagePickerService); cross-platform deployment risk on Windows Surface and Android tablets
- **No regression test suite** for core drawing workflows (zoom, pan, rotation, opacity, lock/unlock, gesture handling); high risk of silent failures during refactoring
- **No E2E test plan** documented; manual testing is inconsistent and edge cases are untested

**Architecture Observations:**
- BaseViewModel uses MVVM pattern with INotifyPropertyChanged and SetProperty<T> helper—good foundation for unit testing with mocking
- ViewModels have complex state management: TraceViewModel manages zoom bounds (0.5–4.0), offset tracking, rotation, opacity, and lock state
- Service layer clearly abstracted via interfaces (ISettingsService, IImagePickerService, IScreenWakeService)—design supports testability
- Models are simple POCOs (TraceImageState, LightColorPreset, etc.)—easy to test
- SettingsService uses MAUI Preferences.Default—needs mocking strategy for tests
- No existing CI/CD pipeline configuration observed yet (check .github/workflows)

**Created Issues:**
- **#14**: Create unit test suite for ViewModels (type:chore, squad:lambert) — xUnit + Moq for property binding and state validation
- **#16**: Create integration tests for platform-specific services (type:chore, squad:lambert) — ScreenWakeService, ImagePickerService on Windows and Android
- **#18**: Build regression test suite for critical drawing workflows (type:chore, squad:lambert) — gesture handling, zoom/pan/rotate/opacity, lock state
- **#19**: Create E2E test plan and validation checklist for cross-platform scenarios (type:spike, squad:lambert) — Windows Surface + Android tablet workflows

**Recommendations for Implementation:**
1. Start with unit tests (issue #14) using xUnit and Moq; establish mocking patterns for services
2. Add integration tests (issue #16) against actual platform APIs; document permission/setup requirements
3. Build regression suite (issue #18) to protect core workflows from refactoring regressions
4. Execute E2E spike (issue #19) to document test scenarios before broader automation

**Risk Areas Identified:**
- Screen wake behavior critical for drawing workflows (mid-session sleep = data loss UX risk)
- Gesture conflicts under high-frequency input (stylus on Surface, touch on Android)
- Zoom/pan/rotation state consistency across platforms
- Permission issues on Android (camera, storage, device power)
- Platform-specific rendering edge cases with SkiaSharp

## Team Updates

### 2026-05-01T17:35:50Z (Scribe Decision Merge)
- Scribe synchronized cross-team decisions into active decisions ledger:
  - **Session Persistence Strategy** (Ripley) — app-scoped local storage for offline-capable session recovery
  - **Platform Risk Assessment** (Bishop) — 5 critical platform/performance risks sequenced for v1 planning
  - **UX Touch Workflow** (Hicks) — 5 UX friction points prioritized for polish sprint
- All 12 GitHub issues (created by Ripley, Hicks, Bishop, Lambert) now have corresponding decisions in ledger.
- Decision inbox cleared (3 items processed). Team consensus model now operational.
