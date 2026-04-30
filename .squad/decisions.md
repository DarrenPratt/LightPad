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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
