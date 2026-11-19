using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using MerchAR.AR;
using MerchAR.Product;
using MerchAR.UI;

namespace MerchAR.Core
{
    /// <summary>
    /// Automatic Scene Bootstrap for MerchAR.
    /// Guarantees that even if MainScene is loaded without manual Inspector wiring,
    /// the full GameObject hierarchy, singletons, camera rigs, AR components,
    /// UI Canvas, and event systems are verified and wired at runtime.
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
            EnsureViewerRig();
            EnsureARRig();
            EnsureReticle();
            EnsureUICanvas();
            WireCrossReferences();
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

            var colorMgr = appManager.GetComponent<ProductColorManager>();
            if (colorMgr == null)
            {
                appManager.gameObject.AddComponent<ProductColorManager>();
            }
        }

        private static void EnsureViewerRig()
        {
            var viewerRig = GameObject.Find("[ViewerMode_Rig]");
            if (viewerRig == null)
            {
                viewerRig = new GameObject("[ViewerMode_Rig]");
            }

            var viewerCam = viewerRig.GetComponentInChildren<Camera>(true);
            if (viewerCam == null)
            {
                var camGo = new GameObject("ViewerCamera");
                camGo.transform.SetParent(viewerRig.transform);
                viewerCam = camGo.AddComponent<Camera>();
                viewerCam.transform.localPosition = new Vector3(0, 0.1f, 1.35f);
                viewerCam.transform.localRotation = Quaternion.Euler(5f, 180f, 0f);
                viewerCam.fieldOfView = 45f;
                viewerCam.clearFlags = CameraClearFlags.SolidColor;
                viewerCam.backgroundColor = new Color(0.957f, 0.961f, 0.969f, 1f);
            }

            // Directional Light
            var lights = viewerRig.GetComponentsInChildren<Light>(true);
            if (lights == null || lights.Length == 0)
            {
                var keyLight = new GameObject("Directional Light (Key Light)");
                keyLight.transform.SetParent(viewerRig.transform);
                var l1 = keyLight.AddComponent<Light>();
                l1.type = LightType.Directional;
                l1.intensity = 1.0f;
                keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                var fillLight = new GameObject("Fill Light");
                fillLight.transform.SetParent(viewerRig.transform);
                var l2 = fillLight.AddComponent<Light>();
                l2.type = LightType.Directional;
                l2.intensity = 0.4f;
                fillLight.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
            }

            // TurntableRoot
            var turntable = viewerRig.transform.Find("TurntableRoot");
            GameObject turntableGo;
            if (turntable == null)
            {
                turntableGo = new GameObject("TurntableRoot");
                turntableGo.transform.SetParent(viewerRig.transform);
                turntableGo.transform.localPosition = Vector3.zero;
            }
            else
            {
                turntableGo = turntable.gameObject;
            }

            if (turntableGo.GetComponent<ProductController>() == null)
                turntableGo.AddComponent<ProductController>();
            if (turntableGo.GetComponent<ProductRotator>() == null)
                turntableGo.AddComponent<ProductRotator>();
            if (turntableGo.GetComponent<ProductScaler>() == null)
                turntableGo.AddComponent<ProductScaler>();
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

        private static void WireCrossReferences()
        {
            var appMgr = FindFirstObjectByType<AppManager>();
            var productCtrl = FindFirstObjectByType<ProductController>();
            var placementMgr = FindFirstObjectByType<ARPlacementManager>();
            var uiMgr = FindFirstObjectByType<UIManager>();

            // Setup TurntableRoot on ProductController if needed
            if (productCtrl != null)
            {
                var turntable = GameObject.Find("TurntableRoot");
                if (turntable != null)
                {
                    var rotator = turntable.GetComponent<ProductRotator>();
                    var scaler = turntable.GetComponent<ProductScaler>();
                    if (rotator != null)
                    {
                        var targetField = typeof(ProductRotator).GetField("targetTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (targetField != null && targetField.GetValue(rotator) == null)
                            targetField.SetValue(rotator, turntable.transform);
                    }
                    if (scaler != null)
                    {
                        var targetField = typeof(ProductScaler).GetField("targetTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (targetField != null && targetField.GetValue(scaler) == null)
                            targetField.SetValue(scaler, turntable.transform);
                    }
                }
            }
        }
    }
}
