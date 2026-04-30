# MAUI Lightbox App Design (Surface + Android)

## Overview

This project is a cross-platform **digital lightbox application** built using **.NET MAUI**, designed to run on:

- Windows Surface devices
- Android tablets
- (Optionally iPad later)

The goal is to replicate and enhance a physical lightbox for tracing, drawing, and animation workflows.

---

## App Concept

**Working Names:**
- LumiTrace
- LightPad
- TraceBox

---

## Core Features

### 1. Lightbox Mode

A full-screen light surface with adjustable properties:

- Brightness control (simulated via overlay)
- Colour temperature slider
- Presets: White / Warm / Cool / Custom colour
- Fullscreen immersive mode
- Screen lock to prevent accidental touches

---

### 2. Image Tracing Mode

Allows users to load and trace over images.

**Features:**

- Import image from device
- Pinch to zoom
- Drag to reposition
- Rotate image
- Opacity slider
- Lock image position
- Optional grid overlay

---

### 3. Animation / Onion Skin Mode

Useful for frame-by-frame tracing or animation.

**Features:**

- Load multiple frames
- Previous/Next frame navigation
- Onion skin overlay (previous frame faded)
- Adjustable frame opacity

---

### 4. Safe Screen Controls

Optimised for real-world use with paper on screen:

- Large lock/unlock button
- Auto-hide UI after inactivity
- Tap-and-hold to unlock
- Disable gestures when locked
- Keep screen awake (no sleep)

---

## UI Design (Modern Layout)

### Main Menu

```
[ Plain Lightbox ]

[ Trace Image ]

[ Animation Frames ]

[ Settings ]
```

---

### Tracing Screen Layout

```
+------------------------------------------------+
|                                                |
|              Image / Light Area                |
|                                                |
|                                                |
+------------------------------------------------+
| Brightness | Opacity | Lock | Rotate | Grid     |
+------------------------------------------------+
```

**Behaviour:**
- When locked → controls disappear
- Fullscreen lightbox mode activated

---

## Technology Stack

### Core

- **.NET MAUI** (cross-platform UI)
- **MVVM architecture**

### Libraries

- **CommunityToolkit.Maui**
- **SkiaSharp** (image rendering, zoom, rotation, overlays)

### Storage

- `Preferences` for user settings

---

## Project Structure

```
LightboxApp
│
├── Views
│   ├── MainPage.xaml
│   ├── LightboxPage.xaml
│   ├── TracePage.xaml
│   └── SettingsPage.xaml
│
├── ViewModels
│   ├── MainViewModel.cs
│   ├── LightboxViewModel.cs
│   └── TraceViewModel.cs
│
├── Services
│   ├── ImagePickerService.cs
│   ├── ScreenWakeService.cs
│   └── SettingsService.cs
│
├── Models
│   └── TraceImageState.cs
│
└── Resources
```

---

## MVP Scope (Version 1)

Focus on delivering a usable lightbox quickly:

### Must-Have

- Fullscreen white lightbox
- Brightness / colour control (overlay-based)
- Load and display image
- Pan and zoom image
- Opacity slider
- Lock screen mode
- Keep screen awake

### Nice-to-Have (Later)

- Onion skin animation
- Grid overlay
- Multi-image layers
- Save/load sessions

---

## Platform Considerations

### Brightness Control

- MAUI cannot reliably control **hardware brightness** cross-platform
- Use **overlay-based brightness simulation** initially
- Platform-specific implementations can be added later:
  - Windows (Surface)
  - Android APIs

---

### Touch & Pen Input

- Ensure gestures work well with:
  - Touch
  - Surface Pen
  - Android stylus

---

### Screen Behaviour

- Prevent screen sleep during use
- Disable accidental gestures when locked

---

## Future Enhancements

- Multi-layer tracing system
- Export traced images
- Cloud sync (optional)
- Calibration mode for accurate scaling
- Dark mode UI

---

## Summary

This approach provides:

- A **modern cross-platform solution**
- Immediate usability on Surface
- Easy path to Android tablets
- Flexibility to expand into a more advanced drawing/animation tool

---

**Recommendation:**
Start with MAUI + SkiaSharp MVP, validate usability on Surface, then expand features for Android deployment.

