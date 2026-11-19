using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using VoxelAR.AR;
using VoxelAR.UI;

namespace VoxelAR.Core
{
    /// <summary>
    /// Builds the whole runtime hierarchy (event system, AR rig, app objects, reticle, UI) in code,
    /// so the scene only needs one GameObject with this component.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("Runtime Auto-Wiring")]
        [SerializeField] private bool autoBuildIfMissing = true;

        private void Awake()
        {
            if (autoBuildIfMissing)
            {
                VerifyAndWireScene();
            }
        }

        public static void VerifyAndWireScene()
        {
            EnsureEventSystem();
            EnsureAppCore();
            EnsureARRig();
            EnsureReticle();
            EnsureUICanvas();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }
        }

        private static void EnsureAppCore()
        {
            var appManager = FindFirstObjectByType<AppManager>();
            if (appManager == null)
            {
                var coreGo = new GameObject("[AppCore]");
                var appGo = new GameObject("AppManager");
                appGo.transform.SetParent(coreGo.transform);
                appManager = appGo.AddComponent<AppManager>();
            }
        }

        private static void EnsureARRig()
        {
            var arRig = GameObject.Find("[ARMode_Rig]");
            if (arRig == null)
            {
                arRig = new GameObject("[ARMode_Rig]");
            }

            var arSession = FindFirstObjectByType<ARSession>();
            if (arSession == null)
            {
                var sessionGo = new GameObject("AR Session");
                sessionGo.transform.SetParent(arRig.transform);
                sessionGo.AddComponent<ARSession>();
                sessionGo.AddComponent<ARInputManager>();
            }

            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                var originGo = new GameObject("XR Origin (AR Rig)");
                originGo.transform.SetParent(arRig.transform);
                originGo.AddComponent<Unity.XR.CoreUtils.XROrigin>();
                originGo.AddComponent<ARPlaneManager>();
                originGo.AddComponent<ARRaycastManager>();
                originGo.AddComponent<ARPlaneController>();

                var offsetGo = new GameObject("Camera Offset");
                offsetGo.transform.SetParent(originGo.transform);

                var camGo = new GameObject("Main Camera (AR Camera)");
                camGo.transform.SetParent(offsetGo.transform);
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<ARCameraManager>();
                camGo.AddComponent<ARCameraBackground>();
                camGo.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

                var originComp = originGo.GetComponent<Unity.XR.CoreUtils.XROrigin>();
                if (originComp != null)
                {
                    originComp.CameraFloorOffsetObject = offsetGo;
                    originComp.Camera = cam;
                }
            }

            var placementMgr = arRig.GetComponentInChildren<ARPlacementManager>(true);
            if (placementMgr == null)
            {
                var pmGo = new GameObject("ARPlacementManager");
                pmGo.transform.SetParent(arRig.transform);
                pmGo.AddComponent<ARPlacementManager>();
            }
        }

        private static void EnsureReticle()
        {
            var env = GameObject.Find("[Environment_Assets]");
            if (env == null)
            {
                env = new GameObject("[Environment_Assets]");
            }

            var reticle = env.transform.Find("AR_Reticle_Cursor");
            if (reticle == null)
            {
                var reticleGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                reticleGo.name = "AR_Reticle_Cursor";
                reticleGo.transform.SetParent(env.transform);
                reticleGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                reticleGo.transform.localScale = Vector3.one * 0.25f;

                var collider = reticleGo.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                var mat = Resources.Load<Material>("M_AR_Reticle");
                if (mat != null)
                {
                    var rend = reticleGo.GetComponent<MeshRenderer>();
                    if (rend != null) rend.sharedMaterial = mat;
                }
                reticleGo.SetActive(false);
            }
        }

        private static void EnsureUICanvas()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            GameObject canvasGo;
            if (canvas == null)
            {
                canvasGo = new GameObject("[UI_Canvas]");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 2400);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasGo = canvas.gameObject;
            }

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            if (canvasGo.GetComponent<UIManager>() == null)
            {
                canvasGo.AddComponent<UIManager>();
            }
        }
    }
}
