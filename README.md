# MerchAR — Android Augmented Reality Merchandise Viewer

[![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20ARCore-brightgreen.svg)]()
[![Engine](https://img.shields.io/badge/Engine-Unity%206%20%28URP%29-blue.svg)]()
[![Language](https://img.shields.io/badge/Language-C%23%20%7C%20Python-orange.svg)]()
[![Offline](https://img.shields.io/badge/Network-100%25%20Offline-success.svg)]()

**MerchAR** is a lightweight, mobile-first Android application developed in Unity 6 using Universal Render Pipeline (URP), AR Foundation, and Google ARCore. It provides users with an offline, interactive 3D merchandise showroom that smoothly transitions into real-world Augmented Reality plane tracking and placement.

---

## Key Features

1. **3D Interactive Merchandise Viewer**:
   - High-fidelity 3D T-shirt and Streetwear Hoodie models.
   - Smooth 360-degree turntable rotation with inertia and damping.
   - Dual-finger pinch-to-zoom (mobile) and scroll wheel zoom (Editor).
   - One-touch view reset and product switching.
2. **Real-time Color Customization**:
   - 6 curated e-commerce colors: **Black, White, Red, Blue, Green, Purple**.
   - Direct material customization via `MaterialPropertyBlock` (preserves project assets and prevents memory leaks).
3. **Augmented Reality Experience**:
   - Phone camera SLAM tracking via Google ARCore.
   - Real-time horizontal plane detection with animated visual reticle indicator.
   - Tap-to-place merchandise on tables or floors.
   - Intuitive touch gestures: single-finger drag to rotate, dual-finger pinch to scale.
   - Automatic plane polygon hiding once an item is placed for clean viewing.
   - One-tap removal and graceful return to 3D showroom.
4. **Standalone 3D Asset Generation (No Blender Required)**:
   - Includes **pure-Python standalone generator** (`generate_apparel_obj.py`) producing clean Wavefront `.obj` files with normals and UVs out of the box.
   - Includes in-engine Unity procedural C# mesh generator (`ProceduralApparelGenerator.cs`).
   - Also includes headless Blender Python scripts (`generate_tshirt.py`, `generate_hoodie.py`) for FBX generation.

---

## Project Structure

```text
unity/
├── Assets/
│   ├── Materials/
│   │   ├── M_TShirt_Default.mat      # URP Lit material for T-shirt
│   │   ├── M_Hoodie_Default.mat      # URP Lit material for Hoodie
│   │   ├── M_AR_Reticle.mat          # Transparent reticle placement material
│   │   └── M_AR_Plane.mat            # Semi-transparent plane visualizer material
│   ├── Models/
│   │   └── Generated/
│   │       ├── TShirt.obj            # Generated 3D T-Shirt model (704 tris)
│   │       ├── TShirt.mtl            # Material definition
│   │       ├── Hoodie.obj            # Generated 3D Hoodie model (1064 tris)
│   │       └── Hoodie.mtl            # Material definition
│   └── Scripts/
│       ├── AR/
│       │   ├── ARPlacementManager.cs     # Raycast plane hits, reticle, spawning
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
│           └── AppManager.cs         # Master state machine, camera & permissions
├── Documentation/
│   ├── ARCHITECTURE.md               # Subsystem breakdown & architectural diagrams
│   ├── UNITY_SETUP.md                # GameObject hierarchy & Inspector wiring guide
│   ├── ANDROID_BUILD.md              # Android platform & ARCore build guide
│   └── TROUBLESHOOTING.md            # Diagnoses for ARCore, tracking, and shaders
└── Tools/
    ├── PythonStandalone/
    │   └── generate_apparel_obj.py   # Pure Python OBJ generator (no Blender needed)
    └── Blender/
        ├── generate_tshirt.py        # Blender Python script for FBX export
        └── generate_hoodie.py        # Blender Python script for FBX export
```

---

## Quickstart Guide

### 1. 3D Model Generation
The repository already includes pre-generated, optimized 3D models in `Assets/Models/Generated/`:
- `TShirt.obj` (416 vertices, 704 triangles)
- `Hoodie.obj` (628 vertices, 1064 triangles)

To regenerate them at any time without Blender:
```bash
python3 Tools/PythonStandalone/generate_apparel_obj.py
```

### 2. Unity Editor Assembly
Follow the step-by-step instructions in [UNITY_SETUP.md](Documentation/UNITY_SETUP.md) to:
1. Construct the scene hierarchy.
2. Attach scripts and wire serialized Inspector fields.
3. Configure AR Session and XR Origin.
4. Hook up Canvas buttons for product and color switching.

### 3. Build & Run on Android
Follow [ANDROID_BUILD.md](Documentation/ANDROID_BUILD.md) to:
1. Switch platform to **Android**.
2. Enable **Google ARCore** under XR Plug-in Management.
3. Set Scripting Backend to **IL2CPP** and Architecture to **ARM64**.
4. Build APK and install via USB debugging.

---

## Acceptance Criteria Checklist

- [x] All required C# scripts implemented and verified modularly.
- [x] No intentional placeholder implementations remain in core features.
- [x] T-shirt and Hoodie 3D models generated and verified in `Assets/Models/Generated/`.
- [x] Standalone generator works with pure standard library Python (no Blender required).
- [x] In-engine Unity procedural C# mesh generator provided (`ProceduralApparelGenerator.cs`).
- [x] Blender Python FBX generation scripts created in `Tools/Blender/`.
- [x] Smooth 360-degree rotation with inertia implemented.
- [x] Dual-finger pinch-to-zoom implemented.
- [x] 6-color real-time customization via `MaterialPropertyBlock` implemented.
- [x] AR Foundation plane detection, reticle cursor, and tap-to-place implemented.
- [x] AR placed-object touch rotation and pinch scaling implemented.
- [x] Detailed documentation: `ARCHITECTURE.md`, `UNITY_SETUP.md`, `ANDROID_BUILD.md`, `TROUBLESHOOTING.md`.
