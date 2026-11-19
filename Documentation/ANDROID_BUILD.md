# MerchAR — Android Build & ARCore Deployment Guide

This guide walks through configuring the Unity Editor for Android, setting up ARCore XR Plugin, tuning player settings, compiling the release APK, and deploying to a physical Android device.

---

## 1. Prerequisites

1. **Unity 6 (or 2022.3+ LTS)** with Android Build Support installed:
   - Android Build Support
   - Android SDK & NDK Tools
   - OpenJDK
2. A physical Android device supporting Google ARCore:
   - Must be listed on [Google ARCore Supported Devices](https://developers.google.com/ar/devices).
   - "Google Play Services for AR" must be installed on the phone (automatically prompted via Play Store).
3. USB Cable with **USB Debugging** enabled under Developer Options on the device.

---

## 2. Switch Platform to Android

1. In the Unity Editor, navigate to **File > Build Profiles** (or **File > Build Settings**).
2. Select **Android** from the platform list.
3. Click **Switch Platform**.
4. Set **Texture Compression** to `ASTC` (standard for modern Android devices).

---

## 3. Configure XR Plug-in Management & ARCore

1. Open **Edit > Project Settings > XR Plug-in Management**.
2. Select the **Android tab** (robot icon).
3. Under Plug-in Providers, check **Google ARCore**.
4. Expand **XR Plug-in Management > ARCore** in the left menu:
   - **ARCore Requirement**: Set to `Required` (prevents installation on devices lacking ARCore hardware support) or `Optional` (allows app to run in 3D viewer mode on all Android devices, with AR mode gracefully disabled).

---

## 4. Player & Rendering Settings

Open **Edit > Project Settings > Player** and select the **Android tab**:

### 4.1 Identification
- **Package Name**: `com.merchar.viewer` (or `com.yourcollege.merchar`)
- **Version**: `1.0.0`
- **Bundle Version Code**: `1`

### 4.2 Minimum API Level
- **Minimum API Level**: `Android 7.0 'Nougat' (API Level 24)` or `Android 8.0 'Oreo' (API Level 26)`.
- **Target API Level**: `Automatic (highest installed)` or `Android 14 / 15 (API level 34 / 35)`.

### 4.3 Architecture & Scripting Backend
- **Scripting Backend**: `IL2CPP` (Required for 64-bit).
- **Target Architectures**:
  - Check **ARM64** (Mandatory for modern Google Play & ARCore).
  - Uncheck **ARMv7** (ARCore deprecated 32-bit on modern Android).

### 4.4 Rendering & Color Space
- **Color Space**: `Linear` (Standard for URP).
- **Auto Graphics API**: Check (or ensure `Vulkan` and `OpenGLES3` are active).
- **Multithreaded Rendering**: Enabled.

---

## 5. Camera Permissions & Manifest

ARCore requires camera access to feed video frames into the SLAM plane tracking engine.
In `MerchAR`, camera permissions are automatically handled in `AppManager.cs` using:
```csharp
UnityEngine.Android.Permission.RequestUserPermission(Permission.Camera);
```

Unity and the ARCore XR Plugin automatically inject the required camera tag into the final generated `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-feature android:name="android.hardware.camera.ar" android:required="true" />
```

---

## 6. Building the APK

1. Go to **File > Build Settings**.
2. Ensure `MainScene` is checked in **Scenes In Build**.
3. Select **Build** (or **Build and Run** if device is connected via USB).
4. Save the APK file as `MerchAR_v1.0.apk`.

---

## 7. Installing and Testing via ADB

If installing manually from terminal:
```bash
# 1. Verify device connection
adb devices

# 2. Install APK to phone
adb install -r MerchAR_v1.0.apk

# 3. View live logcat output for MerchAR
adb logcat -s Unity:V MerchAR:V
```

---

## 8. Verifying ARCore on Physical Device

1. Ensure the room has adequate ambient lighting and textured floors/tables (pure featureless white floors make visual feature point detection harder).
2. Move the phone slowly in a gentle circular arc to allow ARCore's SLAM algorithm to estimate horizontal planes.
3. As soon as the surface is detected, the **MerchAR Reticle Cursor** will snap to the floor and the status banner will update: *"Surface detected! Tap screen to place merchandise."*
