# MerchAR — Troubleshooting & Diagnosis Guide

This guide addresses common setup pitfalls, ARCore tracking challenges, and runtime issues.

---

## 1. Augmented Reality & Device Compatibility

### Issue: "ARCore is not supported on this device"
- **Cause**: The Android device either lacks hardware certification for ARCore or does not have Google Play Services for AR installed.
- **Solution**:
  1. Check the official Google list: [ARCore Supported Devices](https://developers.google.com/ar/devices).
  2. Open the Google Play Store on your device and search for **Google Play Services for AR**. Ensure it is updated.
  3. In Unity **XR Plug-in Management > ARCore**, verify **ARCore Requirement** is set appropriately. If testing on an unsupported phone, setting it to `Optional` will let the app run its 3D Viewer mode cleanly.

### Issue: Camera View is Completely Black in AR Mode
- **Cause 1**: Camera permission was denied by the user.
  - **Fix**: Open Android Settings > Apps > MerchAR > Permissions > Camera > Select "Allow while using the app".
- **Cause 2**: Missing **AR Camera Background** component on the AR Camera child of XR Origin.
  - **Fix**: Select the AR Camera GameObject, ensure `ARCameraBackground` and `ARCameraManager` components are attached.
- **Cause 3**: Graphics API incompatibility with ARCore.
  - **Fix**: Under **Player Settings > Other Settings > Graphics APIs**, ensure `Vulkan` or `OpenGLES3` is configured.

### Issue: "Scanning surface..." banner never finds a plane
- **Cause 1**: Extremely low ambient light or featureless surfaces (e.g. glossy glass table, solid white vinyl floor).
  - **Fix**: Move the phone near a surface with visible texture (carpet, wood grain, tile with grout lines) in a well-lit room.
- **Cause 2**: Plane Detection Mode is set to None.
  - **Fix**: Check `ARPlaneManager` on `XR Origin` and ensure **Plane Detection Mode** is set to `Horizontal`.

---

## 2. 3D Models & Rendering

### Issue: Model appears magenta/pink in Unity Editor or Device
- **Cause**: Material shader mismatch (e.g., standard Built-in pipeline shader inside a Universal Render Pipeline project).
- **Solution**:
  1. Select the material (`M_TShirt_Default` or `M_Hoodie_Default`).
  2. In the Inspector, change Shader to **Universal Render Pipeline/Lit** (or `Universal Render Pipeline/Simple Lit`).
  3. If importing custom FBX models, run **Window > Rendering > Render Pipeline Converter** and convert materials to URP.

### Issue: 3D Model is invisible or tiny after placement
- **Cause**: Asset scale unit mismatch between 3D modeling tool and Unity meters.
- **Solution**:
  1. In `Assets/Models/Generated/`, click on `TShirt.obj` or `Hoodie.obj`.
  2. In the Model tab of the Inspector, verify **Model > Scale Factor** is set to `1.0`.
  3. In `ARPlacementManager.cs`, adjust `initialSpawnScale` (default: `0.5f`).

### Issue: Fallback Procedural Model Generation
- If you ever need to generate models immediately inside Unity without importing OBJ/FBX:
  1. Add an empty GameObject in the scene.
  2. Add component **`ProceduralApparelGenerator`**.
  3. Right-click the component header in the Inspector and select **"Generate T-Shirt"** or **"Generate Hoodie"**.
  4. The procedural mesh is generated instantly with UVs and normals.

---

## 3. Customization & Input Interaction

### Issue: Color buttons change UI but do not alter 3D apparel
- **Cause**: Target MeshRenderers not assigned on `ProductController` or `ProductItem`.
- **Solution**:
  1. Check `ProductController` in the Inspector. Expand `Products > Element 0 / Element 1`.
  2. Ensure `Target Renderers` contains the `MeshRenderer` attached to the 3D model.
  3. `ProductColorManager` modifies `_BaseColor` via `MaterialPropertyBlock`. Ensure your shader uses `_BaseColor` (URP Lit) or `_Color`.

### Issue: Tapping UI buttons accidentally places merchandise in AR
- **Cause**: Touch event leaking through Canvas without UI raycast blocking.
- **Solution**:
  - `ARPlacementManager.cs`, `ProductRotator.cs`, and `ARObjectManipulator.cs` include:
    ```csharp
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
    ```
  - Ensure your UI Canvas has a `GraphicRaycaster` component attached and the scene has an `EventSystem` GameObject.

---

## 4. Compilation & Build Issues

### Issue: "IL2CPP build failed: ARM64 architecture required"
- **Cause**: Google Play 64-bit requirement.
- **Solution**:
  - Go to **Project Settings > Player > Other Settings**.
  - Set **Scripting Backend** to `IL2CPP`.
  - Under **Target Architectures**, check `ARM64` and uncheck `ARMv7`.
  - Ensure the Android NDK path is valid under **Unity > Settings > External Tools**.
