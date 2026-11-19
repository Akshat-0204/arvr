using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;
using VoxelAR.Core;

namespace VoxelAR.EditorTools
{
    /// <summary>
    /// One-shot project setup (Task 1 + Task 3 materials). Everything is created through Unity APIs,
    /// so Unity itself serializes the assets — no hand-written YAML.
    /// Menu: VoxelAR ▸ Setup Project. Batch: -executeMethod VoxelAR.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        public const string Root = "Assets/VoxelAR";
        public const string ScenePath = Root + "/Scenes/Main.unity";
        public const string ResourcesDir = Root + "/Resources";
        public const string SettingsDir = Root + "/Settings";
        public const string VoxelLayer = "Voxel";

        [MenuItem("VoxelAR/Setup Project")]
        public static void Run()
        {
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(ResourcesDir);
            EnsureFolder(SettingsDir);

            PlayerSettings.productName = "VoxelAR";
            EnsureLayer(VoxelLayer);
            EnsureMaterials();
            EnsureRenderPipeline();
            EnsureXRLoaders();
            CreateScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoxelAR Setup] DONE");
        }

        public static void RunBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[VoxelAR Setup] FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        // ---------- folders / layers ----------

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureLayer(string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    Debug.Log($"[VoxelAR Setup] Layer '{layerName}' exists at {i}");
                    return;
                }

            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log($"[VoxelAR Setup] Layer '{layerName}' added at {i}");
                return;
            }
            throw new InvalidOperationException("No free user layer for " + layerName);
        }

        // ---------- materials ----------

        private static void EnsureMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null)
                throw new InvalidOperationException("URP shaders not found — URP package not loaded?");

            string blockPath = ResourcesDir + "/M_Block.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(blockPath) == null)
            {
                var m = new Material(lit) { name = "M_Block", enableInstancing = true };
                m.SetFloat("_Smoothness", 0.2f);
                m.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(m, blockPath);
                Debug.Log("[VoxelAR Setup] Created " + blockPath);
            }

            string overlayPath = ResourcesDir + "/M_Overlay.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(overlayPath) == null)
            {
                var m = new Material(unlit) { name = "M_Overlay" };
                // URP transparent, alpha blend
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.8f));
                AssetDatabase.CreateAsset(m, overlayPath);
                Debug.Log("[VoxelAR Setup] Created " + overlayPath);
            }
        }

        // ---------- render pipeline ----------

        private static void EnsureRenderPipeline()
        {
            var current = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            UniversalRendererData data = current != null ? FindRendererData(current) : null;

            if (current == null || data == null)
            {
                Debug.LogWarning($"[VoxelAR Setup] URP asset unusable (asset={(current ? current.name : "null")}, renderer={(data ? data.name : "null")}) — creating fresh one.");
                string dataPath = SettingsDir + "/VoxelAR-Renderer.asset";
                string assetPath = SettingsDir + "/VoxelAR-URP.asset";
                data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(dataPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }
                current = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
                if (current == null)
                {
                    current = UniversalRenderPipelineAsset.Create(data);
                    AssetDatabase.CreateAsset(current, assetPath);
                }
            }
            else
            {
                Debug.Log($"[VoxelAR Setup] URP asset OK: {AssetDatabase.GetAssetPath(current)} renderer {AssetDatabase.GetAssetPath(data)}");
            }

            EnsureARBackgroundFeature(data);

            GraphicsSettings.defaultRenderPipeline = current;
            int saved = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = current;
            }
            QualitySettings.SetQualityLevel(saved, false);
            EditorUtility.SetDirty(current);
        }

        private static UniversalRendererData FindRendererData(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            var list = so.FindProperty("m_RendererDataList");
            if (list == null || list.arraySize == 0) return null;
            return list.GetArrayElementAtIndex(0).objectReferenceValue as UniversalRendererData;
        }

        private static void EnsureARBackgroundFeature(UniversalRendererData data)
        {
            data.rendererFeatures.RemoveAll(f => f == null);
            if (data.rendererFeatures.Any(f => f is ARBackgroundRendererFeature))
            {
                Debug.Log("[VoxelAR Setup] ARBackgroundRendererFeature present");
                SyncFeatureMap(data);
                return;
            }

            var feature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            feature.name = "ARBackgroundRendererFeature";
            AssetDatabase.AddObjectToAsset(feature, data);
            data.rendererFeatures.Add(feature);
            SyncFeatureMap(data);
            EditorUtility.SetDirty(data);
            Debug.Log("[VoxelAR Setup] Added ARBackgroundRendererFeature");
        }

        /// <summary>Keeps ScriptableRendererData.m_RendererFeatureMap aligned with rendererFeatures (normally done by its inspector).</summary>
        private static void SyncFeatureMap(ScriptableRendererData data)
        {
            var field = typeof(ScriptableRendererData).GetField("m_RendererFeatureMap", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return;
            var map = new List<long>();
            foreach (var f in data.rendererFeatures)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out _, out long localId);
                map.Add(localId);
            }
            field.SetValue(data, map);
            EditorUtility.SetDirty(data);
        }

        // ---------- XR ----------

        private static void EnsureXRLoaders()
        {
            AssignLoader(BuildTargetGroup.Android, "UnityEngine.XR.ARCore.ARCoreLoader");
            AssignLoader(BuildTargetGroup.Standalone, "UnityEngine.XR.Simulation.SimulationLoader");
        }

        private static void AssignLoader(BuildTargetGroup group, string loaderType)
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if (settings == null || settings.Manager == null)
            {
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
                if (perTarget == null)
                    throw new InvalidOperationException("XRGeneralSettingsPerBuildTarget config object missing.");
                perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                EditorUtility.SetDirty(perTarget);
                settings = perTarget.SettingsForBuildTarget(group);
                Debug.Log($"[VoxelAR Setup] Created XR settings for {group}");
            }

            var active = settings.Manager.activeLoaders.Where(l => l != null).Select(l => l.GetType().FullName).ToList();
            if (active.Contains(loaderType))
            {
                Debug.Log($"[VoxelAR Setup] XR {group}: {loaderType} already active ({string.Join(", ", active)})");
            }
            else
            {
                bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, loaderType, group);
                Debug.Log($"[VoxelAR Setup] XR {group}: assign {loaderType} → {ok}");
            }
            settings.InitManagerOnStart = true;
            EditorUtility.SetDirty(settings);
        }

        // ---------- scene ----------

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("Bootstrap");
            go.AddComponent<SceneBootstrap>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Failed to save " + ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[VoxelAR Setup] Scene saved + sole Build Settings entry: " + ScenePath);
        }
    }
}
