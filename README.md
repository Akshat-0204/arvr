# MerchAR — Android Augmented Reality Merchandise Viewer

[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20ARCore-brightgreen.svg)]()
[![Engine](https://img.shields.io/badge/Engine-Unity%206%20%28URP%2017.0.x%29-blue.svg)]()
[![Language](https://img.shields.io/badge/Language-C%23%20%7C%20Python-orange.svg)]()
[![Offline](https://img.shields.io/badge/Network-100%25%20Offline-success.svg)]()

**MerchAR** is a lightweight, mobile-first Android application developed in Unity 6 using Universal Render Pipeline (URP), AR Foundation, and Google ARCore. It provides users with an offline, interactive 3D merchandise showroom that smoothly transitions into real-world Augmented Reality plane tracking and placement.

---

## Key Features

1. **3D Interactive Merchandise Viewer**:
   - High-fidelity 3D T-shirt and Streetwear Hoodie models.
   - Smooth 360-degree turntable rotation with inertia and damping (`ProductRotator.cs`).
   - Dual-finger pinch-to-zoom (mobile) and scroll wheel zoom (Editor) (`ProductScaler.cs`).
   - One-touch synchronized view reset and product switching.
2. **Real-time Color Customization**:
   - 6 curated e-commerce colors: **Black, White, Red, Blue, Green, Purple**.
   - Direct material customization via `MaterialPropertyBlock` (preserves project assets and prevents memory leaks).
3. **Augmented Reality Experience**:
   - Phone camera SLAM tracking via Google ARCore.
   - Real-time horizontal plane detection with animated visual reticle indicator.
   - Tap-to-place merchandise on tables or floors.
   - Intuitive touch gestures: single-finger drag to rotate, dual-finger pinch to scale (`ARObjectManipulator.cs`).
   - Automatic plane polygon hiding once an item is placed for clean viewing.
   - One-tap removal and graceful return to 3D showroom.
4. **Standalone 3D Asset Generation (No Blender Required)**:
   - Includes **pure-Python standalone generator** (`generate_apparel_obj.py`) producing clean Wavefront `.obj` files with normals and UVs out of the box.
   - Includes in-engine Unity procedural C# mesh generator (`ProceduralApparelGenerator.cs`).
   - Also includes headless Blender Python scripts (`generate_tshirt.py`, `generate_hoodie.py`) for FBX generation.
5. **Zero-Configuration Ready**:
   - Complete `Assets/Scenes/MainScene.unity` with full hierarchy and all Inspector fields wired.
   - `SceneBootstrap.cs` runtime auto-wiring fallback that constructs and links all singletons, rigs, UI, and event systems dynamically if needed.
   - Complete `ProjectSettings/` (Graphics, Quality, Player, XR Plugin Management, Editor Build Settings).
   - All `.meta` files tracked in version control with synchronized Unity 6 GUIDs.

---

## Project Structure

```text
unity/
├── Assets/
│   ├── Materials/
│   │   ├── M_TShirt_Default.mat      # URP Lit material for T-shirt
│   │   ├── M_Hoodie_Default.mat      # URP Lit material for Hoodie
│   │   ├── M_AR_Reticle.mat          # Proper URP Transparent material for reticle
│   │   └── M_AR_Plane.mat            # Proper URP Transparent material for plane
│   ├── Models/
│   │   └── Generated/
│   │       ├── TShirt.obj            # Generated 3D T-Shirt model (704 tris)
│   │       ├── TShirt.mtl            # Material definition
│   │       ├── Hoodie.obj            # Generated 3D Hoodie model (1064 tris)
│   │       └── Hoodie.mtl            # Material definition
│   ├── Prefabs/
│   │   ├── TShirt_Prefab.prefab      # AR-ready prefab with ARObjectManipulator
│   │   └── Hoodie_Prefab.prefab      # AR-ready prefab with ARObjectManipulator
│   ├── Scenes/
│   │   └── MainScene.unity           # Fully assembled & Inspector-wired scene
│   ├── Settings/
│   │   ├── URP-HighFidelity.asset    # Universal Render Pipeline asset
│   │   └── URP-HighFidelity-Renderer.asset # URP Renderer with ARBackgroundRendererFeature
│   ├── Textures/
│   │   ├── AppIcon.png               # Android high-res adaptive app icon
│   │   ├── SplashScreen.png          # Branded launch splash screen
│   │   ├── T_AR_Reticle.png          # High-contrast reticle HUD texture
│   │   └── T_AR_PlaneGrid.png        # AR surface detection dot-grid texture
│   └── Scripts/
│       ├── AR/
│       │   ├── ARPlacementManager.cs     # Surface raycast hits, reticle, spawning
│       │   ├── ARPlaneController.cs      # Plane mesh visualizer toggle
│       │   └── ARObjectManipulator.cs    # Touch rotation & pinch scale in AR
│       ├── Product/
│       │   ├── ProductController.cs          # Active merchandise catalog & switcher
│       │   ├── ProductRotator.cs             # 360 viewer rotation with inertia
│       │   ├── ProductScaler.cs              # Pinch zoom & scaling logic
│       │   ├── ProductColorManager.cs        # 6-color palette via MaterialPropertyBlock
│       │   └── ProceduralApparelGenerator.cs # Unity in-engine mesh generator fallback
│       ├── UI/
│       │   ├── UIManager.cs          # Master UI coordinator & AR HUD
│       │   ├── ProductSelector.cs    # Product switching UI bindings
│       │   └── ColorSelector.cs      # Color swatch UI bindings
│       └── Core/
│           ├── AppManager.cs         # Master state machine, camera & permissions
│           └── SceneBootstrap.cs     # Runtime hierarchy and wiring guarantor
├── Documentation/
│   ├── ARCHITECTURE.md               # Subsystem breakdown & architectural diagrams
│   ├── UNITY_SETUP.md                # GameObject hierarchy & Inspector wiring guide
│   ├── ANDROID_BUILD.md              # Android platform & ARCore build guide
│   └── TROUBLESHOOTING.md            # Diagnoses for ARCore, tracking, and shaders
├── Packages/
│   └── manifest.json                 # URP 17.0.x, AR Foundation 6.0.x, XR Management 4.5.x
├── ProjectSettings/
│   ├── ProjectVersion.txt            # Unity Editor version synchronization (Unity 6)
│   ├── GraphicsSettings.asset        # Default URP Pipeline configured
│   ├── QualitySettings.asset         # URP assigned across quality levels
│   ├── PlayerSettings.asset          # Android IL2CPP, ARM64, min API 24, com.merchar.viewer
│   ├── EditorBuildSettings.asset     # MainScene.unity registered in build index
│   └── XRGeneralSettingsPerBuildTarget.asset # Google ARCore XR plugin enabled
└── Tools/
    ├── PythonStandalone/
    │   └── generate_apparel_obj.py   # Pure Python OBJ generator (no Blender needed)
    └── Blender/
        ├── generate_tshirt.py        # Blender Python script for FBX export
        └── generate_hoodie.py        # Blender Python script for FBX export
```

---

## Unity & Package Configuration

- **Target Engine**: Unity 6 (`6000.0.x` LTS). Editor versions are kept strictly in sync via `ProjectSettings/ProjectVersion.txt`.
- **Package Manifest**:
  - `com.unity.render-pipelines.universal`: `17.0.3`
  - `com.unity.xr.arfoundation`: `6.0.5`
  - `com.unity.xr.arcore`: `6.0.5`
  - `com.unity.xr.management`: `4.5.4`
  - `com.unity.inputsystem`: `1.7.0`
  - `com.unity.ugui`: `2.0.0`
  - Deprecated and unused modules (cloth, terrain, vehicles, wind, vr) removed.
- **Active Input Handling**: Set to **"Both"** (`activeInputHandler: 2`) in `PlayerSettings.asset`, allowing both legacy mobile touch raycasting and modern Input System events to operate concurrently.
- **Version Control & `.meta` Files**: All assets, folders, prefabs, materials, and scripts include matching `.meta` files with explicit, stable GUIDs so the project opens immediately without broken references or import dialogs.

---

## Quickstart Guide

### 1. 3D Model Generation
The repository includes pre-generated, optimized 3D models in `Assets/Models/Generated/`:
- `TShirt.obj` (416 vertices, 704 triangles)
- `Hoodie.obj` (628 vertices, 1064 triangles)

To regenerate them at any time without Blender:
```bash
python Tools/PythonStandalone/generate_apparel_obj.py
```

### 2. Opening in Unity Editor
1. Open Unity Hub and add this project directory (`arvr`).
2. Select **Unity 6 (6000.0.x LTS)**.
3. Open `Assets/Scenes/MainScene.unity`.
4. All GameObjects, rigs, Canvas UI, and EventSystem are pre-assembled and wired. If loaded dynamically, `SceneBootstrap.cs` will verify and link any missing component references at runtime.

### 3. Build & Run on Android
1. Open **File > Build Settings** (or press `Ctrl+Shift+B`).
2. Verify `MainScene` is listed and checked in **Scenes In Build**.
3. Target Platform is pre-configured for **Android**:
   - Package Name: `com.merchar.viewer`
   - Minimum API: `API Level 24 (Android 7.0 Nougat)`
   - Scripting Backend: `IL2CPP`
   - Target Architecture: `ARM64`
4. Connect your Android device with USB debugging enabled, then click **Build and Run**.

---

## Acceptance Criteria Checklist

- [x] Fixed `manifest.json`: URP 17.0.x, AR Foundation 6.0.x, ARCore 6.0.x, XR Management 4.5.x, Input System, unused modules removed.
- [x] Created `Assets/Scenes/MainScene.unity` matching `UNITY_SETUP.md` with every Inspector field wired.
- [x] Created `SceneBootstrap.cs` runtime bootstrap script guaranteeing automated scene assembly and component wiring.
- [x] Created URP Pipeline Asset (`URP-HighFidelity.asset`) and Renderer (`URP-HighFidelity-Renderer.asset`) with `ARBackgroundRendererFeature`.
- [x] Assigned URP asset in `GraphicsSettings.asset` and `QualitySettings.asset`.
- [x] Configured Google ARCore in `XRGeneralSettingsPerBuildTarget.asset`.
- [x] Configured Active Input Handling to "Both" (`activeInputHandler: 2`) in `PlayerSettings.asset`.
- [x] Configured Android build settings: IL2CPP, ARM64, min API 24, package name `com.merchar.viewer`.
- [x] Replaced `FindObjectOfType` with `FindFirstObjectByType` across all AR scripts.
- [x] Fixed `AppManager` singleton preservation (singletons stay active in `Awake()`, child visuals and components toggle instead).
- [x] Fixed Instantiate-from-inactive-scene-object bug (`SetActive(true)` on clone and dedicated real prefabs).
- [x] Synchronized `ResetView` with `ProductRotator` and `ProductScaler`.
- [x] `OnPlaneDetectionStatusChanged` now fires only when the state changes.
- [x] Recreated `M_AR_Reticle.mat` and `M_AR_Plane.mat` as proper URP Transparent materials.
- [x] Created `Assets/Prefabs/TShirt_Prefab.prefab` and `Hoodie_Prefab.prefab` with `ARObjectManipulator`.
- [x] Verified `EventSystem` with `StandaloneInputModule` and `GraphicRaycaster` on Canvas.
- [x] Added Android app icon, splash screen, and reticle/plane textures in `Assets/Textures/`.
- [x] Included all `ProjectSettings` configuration files.
- [x] Added `.meta` files for all assets and updated documentation.
