# LightPad iPad Readiness Checklist

This repo now includes an active `net10.0-ios` target, but that alone does not make the app ready for iPad distribution.

## What is already in place

- MAUI single-project app structure
- `Platforms/iOS` startup files
- iPad-capable device family and orientations in `Platforms/iOS/Info.plist`
- App-level code that is mostly platform-agnostic

## What still needs to happen

### 1. Build and validate on macOS

You still need a Mac build path for:

- Xcode compatibility validation
- MAUI iOS workload installation
- simulator/device builds
- archive/export for TestFlight or App Store submission

Suggested commands on macOS:

```bash
dotnet workload install maui
dotnet restore LightPad.sln
dotnet build src/LightPad.App/LightPad.App.csproj -f net10.0-ios
```

### 2. Add Apple signing and distribution setup

This repo does not yet contain the operational setup for:

- Apple Developer account configuration
- App ID / bundle registration
- provisioning profiles
- signing certificates
- TestFlight/App Store release workflow

At minimum you should document and automate:

- bundle identifier ownership for `com.jynxprotocol.lightpad`
- release signing configuration
- App Store Connect app creation
- TestFlight upload flow

### 3. Finish Apple compliance metadata

Current status:

- `PrivacyInfo.xcprivacy` now declares `UserDefaults` access for `Preferences`

Still required before release:

- review whether file import behavior needs additional user-facing purpose strings
- verify App Store privacy answers in App Store Connect
- verify encryption declaration if applicable
- add app listing privacy/support URLs

### 4. Do a real iPad layout pass

The current UI works as a general MAUI layout, but it is still designed around the existing Surface/Android-first screens.

Pages that need explicit tablet review:

- `Views/MainPage.xaml`
- `Views/TracePage.xaml`
- `Views/AnimationPage.xaml`
- `Views/SettingsPage.xaml`

Recommended iPad improvements:

- use wider two-pane or three-zone layouts where appropriate
- avoid long fixed button rows that become cramped in some orientations
- adapt controls for portrait vs landscape
- respect safe areas consistently
- test split view and stage manager window sizes

### 5. Validate touch, Pencil, and gesture behavior

The app relies heavily on:

- pan gestures
- pinch gestures
- repeated taps
- large overlay controls

You should verify on a real iPad:

- finger gestures do not conflict with scroll containers
- Apple Pencil interaction behaves acceptably
- lock/unlock affordances still work with paper on screen
- gesture hints and overlays scale correctly on 11-inch and 13-inch devices

### 6. Add iOS/iPad CI or release automation

Current repo automation is Windows-focused. To ship iPad builds reliably, add a macOS workflow for:

- restore
- build
- signing
- archive/export
- optional TestFlight upload

### 7. Produce App Store assets and metadata

Before release, you still need:

- iPad screenshots
- app description/subtitle/keywords
- support URL
- privacy policy URL
- age rating/category review

## Recommended next implementation work in this repo

1. Add idiom- or width-aware layout behavior for the main, trace, animation, and settings pages.
2. Add safe-area and orientation review for every fullscreen page.
3. Add a macOS GitHub Actions workflow for `net10.0-ios` validation.
4. Add release notes/docs for Apple signing and TestFlight.
