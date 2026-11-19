# VoxelAR — Troubleshooting

## Device / AR

**"This phone doesn't support ARCore"**
The device isn't on Google's ARCore supported list, or Google Play Services for AR is missing.
Install or update it from the Play Store. Emulators are not supported, so use XR Simulation in the editor.

**Camera permission message with a Retry button**
The user denied the camera. Tap Retry. If Android stops showing the prompt ("Don't ask again"),
enable it under Settings › Apps › VoxelAR › Permissions › Camera, then tap Retry.

**Black camera feed**
- The URP renderer in use must contain **AR Background Renderer Feature**. Run *VoxelAR ▸ Setup Project*,
  which checks `Assets/Settings/URP-HighFidelity` and assigns it to every quality level.
- Graphics API must be **OpenGLES3** for Android (*VoxelAR ▸ Apply Android Settings*).
- *Project Settings ▸ XR Plug-in Management ▸ Android*: ARCore must be ticked.

**Banner stays on "Move your phone slowly…"**
Planes need texture and light. Plain white tables, glass and dark rooms detect slowly or not at all.
Add a book or a sheet of paper. Hold the phone 30–60 cm above the surface and move it sideways.

**"Tracking lost…" banner**
ARCore lost its position (too dark, too fast, blank surface). The build stays anchored, so move slowly over
a textured area until the banner clears. If the build drifted, use *Menu ▸ Re-place area*. Your blocks are kept.

## Interaction

**Tapping buttons also builds blocks**
It shouldn't. A touch that starts over UI is never a tap (`TapTracker`). If you see this, check the EventSystem.
`SceneBootstrap` creates one with `StandaloneInputModule`, and *Player ▸ Active Input Handling* must be **Both**.

**Pinch adds a block**
Any second finger cancels the pending tap. If this happens, the build is probably using a custom input
path. Taps should come only through `TapInput.TryConsumeTap`.

**Blocks land in the wrong cell after scaling or rotating**
Hits are converted with `WorldRoot.InverseTransformPoint/Direction` before `GridMath.TargetCell`. Anything that
moves blocks must be a child of WorldRoot, or it will break the conversion.

## Rendering

**Pink/magenta blocks or reticle**
`Resources/M_Block` or `M_Overlay` is missing or uses a non-URP shader. Re-run *VoxelAR ▸ Setup Project*.

**Blocks not hit by taps**
The `Voxel` layer (8) must exist. Setup creates it. Without it, the raycast falls back to default layers.

## Saves

**Load says "slot N is empty"**
Nothing has been saved there yet. Slot 1 is filled automatically when the app is backgrounded.
Files: `persistentDataPath/builds/slot{0..2}.json`. On Android, pull with
`adb shell run-as com.voxelar.app cat files/builds/slot0.json`.

**"Load failed: …"**
The file is corrupt or from an unknown format version. The current build is left untouched, because
validation happens before the grid changes.

## Build

**IL2CPP / Gradle failures**
Use the Android SDK/NDK/JDK installed through Unity Hub (*Preferences ▸ External Tools*). ARM64 only.
minSdk 26. Check `Logs/build.log` for the first `error`.

**Batchmode says the project is already open**
Close the Unity editor first. Batch runs and the editor can't share a project.
