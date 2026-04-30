# Squad Decisions

## Active Decisions

### Initial MAUI project structure
- **Context:** LightPad needed an immediate, minimal MAUI foundation aligned with the app design and ready for iterative feature work.
- **Decision:** Use a single MAUI app project in `src\LightPad.App`, tracked by `LightPad.slnx`, and establish `Views`, `ViewModels`, `Models`, and `Services` folders from day one.
- **Rationale:** This keeps the startup footprint small while making room for MVVM-based growth without a later folder/layout migration.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
