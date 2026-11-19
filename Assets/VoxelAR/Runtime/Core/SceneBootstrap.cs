using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using VoxelAR.AR;
using VoxelAR.Interaction;
using VoxelAR.UI;
using VoxelAR.Voxel;

namespace VoxelAR.Core
{
    /// <summary>
    /// Builds the whole runtime hierarchy in code and wires it explicitly, so the scene only needs one
    /// GameObject with this component: EventSystem → AR rig → light → app objects → reticle → UI.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneBootstrap : MonoBehaviour
    {
        public const float ReticleSize = 0.8f; // 16 cells × 5 cm

        public static SceneBootstrap Instance { get; private set; }

        public ARSession Session { get; private set; }
        public XROrigin Origin { get; private set; }
        public Camera ARCamera { get; private set; }
        public AppManager App { get; private set; }
        public WorldPlacement Placement { get; private set; }
        public BlockInteractor Interactor { get; private set; }
        public UIManager UI { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Build()
        {
            EnsureEventSystem();

            var rig = new GameObject("[AR Rig]").transform;
            var templates = new GameObject("[Templates]");
            templates.SetActive(false); // children are prefab-like templates, never live

            // AR session (disabled until AppManager has checked permission + availability)
            var sessionGo = new GameObject("AR Session");
            sessionGo.transform.SetParent(rig, false);
            sessionGo.SetActive(false);
            Session = sessionGo.AddComponent<ARSession>();
            sessionGo.AddComponent<ARInputManager>();
            Session.enabled = false;
            sessionGo.SetActive(true);

            // XR origin + camera
            var originGo = new GameObject("XR Origin");
            originGo.transform.SetParent(rig, false);
            originGo.SetActive(false);
            Origin = originGo.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);
            ARCamera = CreateARCamera(offset.transform);
            Origin.CameraFloorOffsetObject = offset;
            Origin.Camera = ARCamera;

            var planeManager = originGo.AddComponent<ARPlaneManager>();
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal;
            planeManager.planePrefab = CreatePlaneTemplate(templates.transform);
            var raycastManager = originGo.AddComponent<ARRaycastManager>();
            var anchorManager = originGo.AddComponent<ARAnchorManager>();
            originGo.SetActive(true);

            CreateLight();

            // App objects
            var appGo = new GameObject("[App]");
            appGo.AddComponent<TapInput>();
            App = appGo.AddComponent<AppManager>();
            Placement = appGo.AddComponent<WorldPlacement>();
            Interactor = appGo.AddComponent<BlockInteractor>();
            var planeController = appGo.AddComponent<ARPlaneController>();

            var reticle = CreateReticle();
            Placement.Init(raycastManager, anchorManager, ARCamera, reticle, App.Grid, GridMath.DefaultBlockSize);
            Interactor.Init(ARCamera, Placement, App.History);
            planeController.Init(planeManager, Placement);

            UI = CreateCanvas().gameObject.AddComponent<UIManager>();
            UI.Init(Placement, Interactor, App.History, App);
            App.Init(Session, UI, Placement);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Camera CreateARCamera(Transform parent)
        {
            var camGo = new GameObject("AR Camera");
            camGo.transform.SetParent(parent, false);
            camGo.tag = "MainCamera";

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 20f;

            camGo.AddComponent<ARCameraManager>();
            camGo.AddComponent<ARCameraBackground>();

            var pos = new InputAction("Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
            pos.AddBinding("<HandheldARInputDevice>/devicePosition");
            var rot = new InputAction("Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
            rot.AddBinding("<HandheldARInputDevice>/deviceRotation");
            var driver = camGo.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(pos);
            driver.rotationInput = new InputActionProperty(rot);
            return cam;
        }

        private static GameObject CreatePlaneTemplate(Transform templates)
        {
            var go = new GameObject("Plane Visual");
            go.transform.SetParent(templates, false);
            go.AddComponent<ARPlane>();
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var overlay = Resources.Load<Material>("M_Overlay");
            if (overlay != null)
            {
                var m = new Material(overlay) { name = "M_PlaneOverlay (runtime)" };
                m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.15f));
                mr.sharedMaterial = m;
            }
            go.AddComponent<ARPlaneMeshVisualizer>();
            return go;
        }

        private static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
        }

        /// <summary>Flat square frame matching the build-area footprint, lying in XZ.</summary>
        private static GameObject CreateReticle()
        {
            var go = new GameObject("Reticle");
            go.AddComponent<MeshFilter>().sharedMesh = BuildFrameMesh(ReticleSize * 0.5f, 0.012f);
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var overlay = Resources.Load<Material>("M_Overlay");
            if (overlay != null) mr.sharedMaterial = overlay;
            go.SetActive(false);
            return go;
        }

        public static Mesh BuildFrameMesh(float half, float width)
        {
            float o = half, i = half - width;
            const float y = 0.002f;
            var v = new[]
            {
                new Vector3(-o, y, -o), new Vector3(o, y, -o), new Vector3(o, y, o), new Vector3(-o, y, o),
                new Vector3(-i, y, -i), new Vector3(i, y, -i), new Vector3(i, y, i), new Vector3(-i, y, i),
            };
            // Four quads between outer (0-3) and inner (4-7) rings, wound clockwise seen from +Y.
            var t = new[]
            {
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0,
            };
            var mesh = new Mesh { name = "ReticleFrame", vertices = v, triangles = t };
            mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("[UI]");
            go.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }
    }
}
