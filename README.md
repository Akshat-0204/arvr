# VoxelAR

Build with blocks on your real table. VoxelAR is an Android AR app (Unity 6, AR Foundation 6, ARCore)
that drops a 16 × 16 × 16 block build area onto a detected surface. You tap to stack coloured 5 cm
blocks, tap to break them, pinch and twist to resize and turn the whole build, and save up to three builds.

Works fully offline. No accounts, no network.

---

## Controls

| Gesture / button | Action |
|---|---|
| Move phone slowly | Find a table or floor (white overlay appears on planes) |
| Tap (while the square frame shows) | Place the build area |
| Tap a block face / base plate | **Build** mode: add the selected block on that face |
| Tap a block | **Break** mode: remove it |
| Pinch (two fingers) | Scale the build 0.25×–3× |
| Twist (two fingers) | Rotate the build |
| Colour swatches | Pick block: Stone, Dirt, Grass, Sand, Wood, Brick, Water, White |
| Build / Break | Toggle tool |
| Undo | Undo last edit (50 steps; also undoes Clear) |
| Menu | Save 1 (auto) / 2 / 3 · Load 1 / 2 / 3 · Clear build · Re-place area |

Slot 1 is the autosave. It is written when the app goes to the background or closes, and after Clear, and
it is restored on launch. "Re-place area" moves the build area and keeps your blocks.

Editor (XR Simulation): click = tap, mouse wheel = scale, Q / E = rotate.

---

## Requirements

- Unity **6000.6.9f1** with the **Android Build Support** module (incl. OpenJDK + SDK/NDK)
- ARCore-supported Android phone, Android 8.0 (API 26)+, ARM64
- Packages (already in `Packages/manifest.json`): AR Foundation 6.7, ARCore XR Plugin 6.7, URP 17.6,
  XR Management 4.5, Input System 1.7

## Getting started

1. Open the project in Unity Hub (`6000.6.9f1`).
2. Run **VoxelAR ▸ Setup Project** once. It creates the `Voxel` layer and the `M_Block`/`M_Overlay` materials,
   checks the URP renderer (AR background feature), assigns ARCore/Simulation loaders, applies Android
   settings and writes `Assets/VoxelAR/Scenes/Main.unity`.
3. Press Play. **XR Simulation** gives you a virtual room for testing in the editor.

The scene holds one `Bootstrap` GameObject. `SceneBootstrap` builds the AR rig, the app objects, the
reticle and the UI in code at startup. You don't need to wire anything in the Inspector.

## Build the APK

From the editor: **VoxelAR ▸ Build Android APK** → `Builds/VoxelAR.apk`.

Headless (Unity must be closed):

```bash
UNITY=/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/MacOS/Unity
$UNITY -batchmode -projectPath . -buildTarget Android \
  -executeMethod VoxelAR.EditorTools.AndroidBuild.BuildBatch -logFile Logs/build.log
adb install -r Builds/VoxelAR.apk
adb logcat -s Unity   # runtime logs are prefixed [AppManager] / [WorldPlacement] / …
```

Applied settings: IL2CPP, ARM64 only, minSdk 26, OpenGLES3, portrait, package id `com.voxelar.app`.
Change the package id in `Assets/VoxelAR/Editor/AndroidBuild.cs` before you publish.

## Tests

```bash
$UNITY -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/test.log
$UNITY -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode-results.xml -logFile Logs/playtest.log
```

- **EditMode**: grid and grid maths, palette, undo history, JSON serializer and save slots, the tap recogniser,
  cube rendering, and the tap → raycast → cell build/stack/break path.
- **PlayMode**: the full bootstrap under XR Simulation, covering rig wiring, UI state, placement, palette, mode and re-place.

## Project layout

```
Assets/VoxelAR/
  Runtime/
    Core/        SceneBootstrap (builds everything), AppManager (session, saves, autosave)
    AR/          WorldPlacement (reticle + anchored placement), ARObjectManipulator, ARPlaneController
    Voxel/       VoxelGrid, GridMath, BlockPalette, EditHistory, VoxelWorldView, BlockInteractor
    Interaction/ TapTracker (pure), TapInput (touch/mouse → taps, UI filtering)
    Save/        VoxelSerializer (JSON v1), SaveSystem (3 slots, atomic write)
    UI/          UIManager (code-built UI), SafeAreaFitter
  Editor/        ProjectSetup, AndroidBuild
  Tests/         EditMode, PlayMode
Documentation/   IMPLEMENTATION_PLAN.md, TROUBLESHOOTING.md
```

Saves live in `Application.persistentDataPath/builds/slot{0..2}.json`.

The previous MerchAR project is preserved in git history (`bbaff17`, `5a8d5f1`).
