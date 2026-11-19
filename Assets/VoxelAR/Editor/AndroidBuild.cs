using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelAR.EditorTools
{
    /// <summary>
    /// Android player settings for ARCore + an APK build.
    /// Menu: VoxelAR ▸ Apply Android Settings / Build Android APK.
    /// Batch: -executeMethod VoxelAR.EditorTools.AndroidBuild.BuildBatch  (writes Builds/VoxelAR.apk)
    /// </summary>
    /// <summary>Runs for every player build (Build Profiles window included), not just the VoxelAR menu.</summary>
    [InitializeOnLoad]
    internal class EnsureShadersPreprocess : IPreprocessBuildWithReport
    {
        // Also fix settings as soon as scripts load, so Project Settings ▸ Graphics shows it without a build.
        static EnsureShadersPreprocess() =>
            EditorApplication.delayCall += () => { if (AndroidBuild.EnsureAlwaysIncludedShaders()) AssetDatabase.SaveAssets(); };

        public int callbackOrder => -1000;
        // No SaveAssets here: saving mid-build trips 'm_LockCount == 0' and fails writing unity_builtin_extra.
        // The in-memory GraphicsSettings is what the build reads anyway.
        public void OnPreprocessBuild(BuildReport report) => AndroidBuild.EnsureAlwaysIncludedShaders();
    }

    public static class AndroidBuild
    {
        public const string DefaultPackageId = "com.voxelar.app"; // change to your own reverse-domain id before publishing
        public const string OutputPath = "Builds/VoxelAR.apk";

        [MenuItem("VoxelAR/Apply Android Settings")]
        public static void ApplySettings()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.productName = "VoxelAR";
            if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(android)) ||
                PlayerSettings.GetApplicationIdentifier(android) == "com.DefaultCompany.arvr")
                PlayerSettings.SetApplicationIdentifier(android, DefaultPackageId);

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EnsureAlwaysIncludedShaders();
            AssetDatabase.SaveAssets();
            Debug.Log($"[VoxelAR Build] Android settings applied ({PlayerSettings.GetApplicationIdentifier(android)}).");
        }

        // Shaders only referenced at runtime (code-built UI) — stripped from the player unless always included,
        // which renders every uGUI Image/Text magenta. The old fabricated GraphicsSettings had this list empty.
        private static readonly string[] RequiredShaders = { "UI/Default", "Sprites/Default" };

        /// <returns>True if the list was changed.</returns>
        public static bool EnsureAlwaysIncludedShaders()
        {
            bool changed = false;
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var list = gs.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in RequiredShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogError($"[VoxelAR Build] Shader '{name}' not found."); continue; }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                changed = true;
                Debug.Log($"[VoxelAR Build] Added '{name}' to Always Included Shaders.");
            }
            if (changed) gs.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        [MenuItem("VoxelAR/Build Android APK")]
        public static bool Build()
        {
            ApplySettings();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("[VoxelAR Build] Could not switch to Android (is the Android module installed?).");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[VoxelAR Build] {s.result}: {s.totalSize / (1024f * 1024f):F1} MB, {s.totalErrors} errors, {s.totalTime}.");
            return s.result == BuildResult.Succeeded;
        }

        public static void BuildBatch()
        {
            bool ok;
            try { ok = Build(); }
            catch (Exception e) { Debug.LogError("[VoxelAR Build] FAILED: " + e); ok = false; }
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
