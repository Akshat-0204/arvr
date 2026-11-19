# MerchAR — Unity Scene Assembly & Inspector Setup Guide

This guide provides the exact step-by-step procedure to assemble the Unity scene, configure GameObjects, attach components, wire Inspector references, and set up prefabs in **Unity 6 (Universal Render Pipeline)**.

---

## 1. Project Prerequisites & Package Verification

Ensure the following packages are installed via **Window > Package Manager**:
1. **Universal RP** (`com.unity.render-pipelines.universal`)
2. **AR Foundation** (`com.unity.xr.arfoundation` - v5.x / v6.x)
3. **Google ARCore XR Plugin** (`com.unity.xr.arcore`)

---

## 2. Complete Scene Hierarchy Structure

In your main scene (e.g., `Assets/Scenes/MainScene.unity`), construct the following GameObject tree:

```text
MainScene
├── [AppCore]
│   └── AppManager
├── [ViewerMode_Rig]
│   ├── ViewerCamera
│   ├── Directional Light (Key Light)
│   ├── Fill Light
│   └── TurntableRoot
│       ├── TShirt_Instance
│       └── Hoodie_Instance
├── [ARMode_Rig]
│   ├── AR Session
│   └── XR Origin (AR Rig)
│       ├── Camera Offset
│       │   └── Main Camera (AR Camera)
│       ├── AR Plane Manager
│       ├── AR Raycast Manager
│       └── ARPlacementManager
├── [Environment_Assets]
│   └── AR_Reticle_Cursor
└── [UI_Canvas]
    ├── ViewerPanel
    │   ├── Header (App Title)
    │   ├── ProductCard (Name, Price)
    │   ├── ProductSelectorButtons
    │   │   ├── Btn_TShirt
    │   │   └── Btn_Hoodie
    │   ├── ColorPaletteContainer
    │   │   ├── Swatch_0_Black
    │   │   ├── Swatch_1_White
    │   │   ├── Swatch_2_Red
    │   │   ├── Swatch_3_Blue
    │   │   ├── Swatch_4_Green
    │   │   └── Swatch_5_Purple
    │   ├── Btn_ResetView
    │   └── Btn_EnterAR
    ├── ARPanel
    │   ├── InstructionBanner
    │   │   └── Txt_Instruction
    │   ├── Btn_ExitAR
    │   ├── Btn_RemovePlaced
    │   └── Btn_ResetPose
    └── ToastNotification
        └── Txt_ToastMessage
```

---

## 3. Step-by-Step GameObject Configuration

### Step 3.1: Configure `[AppCore]` / `AppManager`
1. Create an Empty GameObject named `AppManager`.
2. Attach component: **`AppManager`** (`MerchAR.Core.AppManager.cs`).
3. Attach component: **`ProductColorManager`** (`MerchAR.Product.ProductColorManager.cs`).
4. Inspector assignments for `AppManager`:
   - **Viewer Camera**: Drag `ViewerCamera` from `[ViewerMode_Rig]`.
   - **Viewer Lighting Rig**: Drag `[ViewerMode_Rig]`.
   - **Viewer Product Root**: Drag `TurntableRoot`.
   - **AR Session**: Drag `AR Session` from `[ARMode_Rig]`.
   - **XR Origin**: Drag `XR Origin` from `[ARMode_Rig]`.
   - **AR Placement Manager Root**: Drag `ARPlacementManager`.

---

### Step 3.2: Configure `[ViewerMode_Rig]` & 3D Merchandise Turntable
1. **ViewerCamera**:
   - Component: `Camera`.
   - Position: `(0, 0.1, 1.35)`.
   - Rotation: `(5, 180, 0)`.
   - Projection: `Perspective` (Field of View: `45`).
   - Clear Flags: `Solid Color` (Background: `#F4F5F7` clean light grey).
2. **TurntableRoot**:
   - Create Empty GameObject named `TurntableRoot` at `(0, 0, 0)`.
   - Attach **`ProductController`** (`MerchAR.Product.ProductController.cs`).
   - Attach **`ProductRotator`** (`MerchAR.Product.ProductRotator.cs`).
     - Target Transform: `TurntableRoot`.
     - Rotation Speed: `0.3`.
     - Damping: `5`.
     - Auto Rotate: `false` (or `true` if you want subtle showroom idle spin).
   - Attach **`ProductScaler`** (`MerchAR.Product.ProductScaler.cs`).
     - Target Transform: `TurntableRoot`.
     - Min Scale: `0.5`, Max Scale: `2.0`.
3. **Instantiate 3D Merchandise**:
   - Drag `Assets/Models/Generated/TShirt.obj` under `TurntableRoot`. Name it `TShirt_Instance`.
     - Material: Assign `Assets/Materials/M_TShirt_Default.mat`.
     - Position: `(0, 0, 0)`, Rotation: `(0, 180, 0)`, Scale: `(1, 1, 1)`.
   - Drag `Assets/Models/Generated/Hoodie.obj` under `TurntableRoot`. Name it `Hoodie_Instance`.
     - Material: Assign `Assets/Materials/M_Hoodie_Default.mat`.
     - Position: `(0, 0, 0)`, Rotation: `(0, 180, 0)`, Scale: `(1, 1, 1)`.
     - Set `Hoodie_Instance` initially inactive (`SetActive(false)`).
4. Wire **`ProductController`** Inspector fields:
   - **Turntable Root**: Drag `TurntableRoot`.
   - **Products** (Size: 2):
     - **Element 0**:
       - Product Id: `tshirt`
       - Display Name: `Classic Crewneck T-Shirt`
       - Product Root: Drag `TShirt_Instance`
       - Target Renderers: Drag `TShirt_Instance`'s MeshRenderer
       - Default Local Position: `(0, 0, 0)`
       - Default Local Euler Angles: `(0, 180, 0)`
       - Default Local Scale: `(1, 1, 1)`
     - **Element 1**:
       - Product Id: `hoodie`
       - Display Name: `Heavyweight Streetwear Hoodie`
       - Product Root: Drag `Hoodie_Instance`
       - Target Renderers: Drag `Hoodie_Instance`'s MeshRenderer
       - Default Local Position: `(0, 0, 0)`
       - Default Local Euler Angles: `(0, 180, 0)`
       - Default Local Scale: `(1, 1, 1)`

---

### Step 3.3: Configure AR Foundation & ARCore (`[ARMode_Rig]`)
1. In the Hierarchy, right-click and choose **XR > AR Session**.
   - Ensure components **AR Session** and **AR Input Manager** are present.
2. Right-click and choose **XR > XR Origin (Mobile AR)** (or `AR Session Origin` in AR Foundation 4).
   - Ensure the child camera has:
     - **AR Camera Manager**
     - **AR Camera Background**
     - Tag: `MainCamera`
3. Add AR Managers to `XR Origin`:
   - Click `XR Origin` > **Add Component** > **AR Plane Manager**:
     - Plane Detection Mode: `Horizontal`.
     - Plane Prefab: (Optional) You can create a simple quad prefab with `ARPlane` and `ARPlaneMeshVisualizer` and assign `Assets/Materials/M_AR_Plane.mat`.
   - Click `XR Origin` > **Add Component** > **AR Raycast Manager**.
   - Click `XR Origin` > **Add Component** > **ARPlaneController** (`MerchAR.AR.ARPlaneController.cs`).
4. Setup **`ARPlacementManager`**:
   - Create child under `[ARMode_Rig]` named `ARPlacementManager`.
   - Attach **`ARPlacementManager`** (`MerchAR.AR.ARPlacementManager.cs`).
   - Inspector fields:
     - **Raycast Manager**: Drag `XR Origin`.
     - **Plane Manager**: Drag `XR Origin`.
     - **AR Camera**: Drag the AR Camera.
     - **Placement Reticle Prefab / Instance**: Drag `AR_Reticle_Cursor` (see Step 3.4).
     - **Tshirt AR Prefab**: Drag `TShirt_Prefab` from `Assets/Prefabs/` (or `TShirt_Instance`).
     - **Hoodie AR Prefab**: Drag `Hoodie_Prefab` from `Assets/Prefabs/` (or `Hoodie_Instance`).

---

### Step 3.4: Configure Placement Reticle Indicator
1. Create a 3D Plane or Quad in the scene named `AR_Reticle_Cursor`.
2. Rotation: `(90, 0, 0)`, Scale: `(0.25, 0.25, 0.25)`.
3. Material: Assign `Assets/Materials/M_AR_Reticle.mat`.
4. Remove any `MeshCollider` to prevent raycast collision with itself.
5. In the Inspector of `ARPlacementManager`, drag `AR_Reticle_Cursor` into **Placement Reticle Instance**.

---

### Step 3.5: Configure UI Canvas
1. Create a **Canvas** (`GameObject > UI > Canvas`).
   - Canvas Scaler: `Scale With Screen Size`, Reference Resolution: `1080 x 2400`, Match: `0.5`.
2. Attach component **`UIManager`** (`MerchAR.UI.UIManager.cs`) to the Canvas GameObject.
3. Wire UI Inspector references:
   - **Viewer Panel**: Drag `ViewerPanel`.
   - **AR Panel**: Drag `ARPanel`.
   - **Enter AR Button**: Drag `Btn_EnterAR`.
   - **Exit AR Button**: Drag `Btn_ExitAR`.
   - **Reset View Button**: Drag `Btn_ResetView`.
   - **Remove Placed Object Button**: Drag `Btn_RemovePlaced`.
   - **Reset AR Pose Button**: Drag `Btn_ResetPose`.
   - **Instruction Banner**: Drag `InstructionBanner`.
   - **Instruction Text**: Drag `Txt_Instruction`.
   - **Toast Panel**: Drag `ToastNotification`.
   - **Toast Text**: Drag `Txt_ToastMessage`.
4. Attach **`ProductSelector`** (`MerchAR.UI.ProductSelector.cs`):
   - Element 0: Product Index `0`, Button `Btn_TShirt`.
   - Element 1: Product Index `1`, Button `Btn_Hoodie`.
5. Attach **`ColorSelector`** (`MerchAR.UI.ColorSelector.cs`):
   - Configure elements 0 to 5 for Black, White, Red, Blue, Green, Purple matching `ColorSwatchBinding`.

---

## 4. Prefab Creation for AR

To enable seamless AR instantiation:
1. Drag `TShirt_Instance` from the Hierarchy into `Assets/Prefabs/TShirt_Prefab.prefab`.
   - Add **`ARObjectManipulator`** component to `TShirt_Prefab`.
2. Drag `Hoodie_Instance` from the Hierarchy into `Assets/Prefabs/Hoodie_Prefab.prefab`.
   - Add **`ARObjectManipulator`** component to `Hoodie_Prefab`.
3. Assign these two prefabs to the `Tshirt AR Prefab` and `Hoodie AR Prefab` fields on `ARPlacementManager`.

---

## 5. Mode Transition Verification

- On launch, `AppManager` starts in `Viewer3D`.
- `ViewerCamera` and `ViewerPanel` are active. `XR Origin` and `AR Session` are inactive.
- Tapping **"View in AR"** prompts Android Camera permissions, initializes ARCore, hides `ViewerCamera`, and enables `XR Origin` + `AR Session`.
- Tapping **"Exit AR"** de-spawns the AR model, pauses ARCore tracking, and returns to the 3D turntable viewer.
