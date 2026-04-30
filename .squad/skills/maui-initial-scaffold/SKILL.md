---
name: "maui-initial-scaffold"
description: "Create a clean initial MAUI repo layout for cross-platform delivery"
domain: "architecture"
confidence: "high"
source: "earned"
---

## Context
Use this when starting a new MAUI app that needs immediate implementation readiness without prematurely building features.

## Patterns
- Keep a single MAUI app project under `src\{AppName}.App` and a root solution file.
- Start with domain-oriented folders: `Views`, `ViewModels`, `Models`, `Services`.
- Target current delivery platforms first (Windows + Android), but retain iOS platform scaffolding for later expansion.
- Document one-command build/run paths in `README.md`.
- Add .NET/MAUI ignore rules early (`bin/`, `obj/`, `.vs/`) before first commit.

## Examples
- `LightPad.sln`
- `src\LightPad.App\LightPad.App.csproj`
- `src\LightPad.App\Views`, `ViewModels`, `Models`, `Services`

## Anti-Patterns
- Scattering app code at repository root.
- Adding fake business logic to “prove” structure.
- Leaving repo without baseline build instructions.
