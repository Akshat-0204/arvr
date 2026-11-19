using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using MerchAR.UI;
using MerchAR.AR;
using MerchAR.Product;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace MerchAR.Core
{
    public enum AppMode
    {
        Viewer3D,
        AugmentedReality
    }

    /// <summary>
    /// Master Application Controller for MerchAR.
    /// Orchestrates mode transitions between 3D Product Viewer and AR Experience.
    /// Controls camera activation, lighting rigs, AR session lifecycle, and Android camera permissions.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [Header("App Mode")]
        [SerializeField] private AppMode currentMode = AppMode.Viewer3D;

        [Header("3D Viewer Environment Rigs")]
        [Tooltip("Standard 3D Camera used for the turntable product viewer.")]
        [SerializeField] private Camera viewerCamera;
        [Tooltip("Studio lights for the 3D product showcase.")]
        [SerializeField] private GameObject viewerLightingRig;
        [Tooltip("The parent GameObject containing the 3D merchandise turntable.")]
        [SerializeField] private GameObject viewerProductRoot;

        [Header("AR Environment Rigs")]
        [Tooltip("AR Session GameObject that manages AR lifecycle.")]
        [SerializeField] private ARSession arSession;
        [Tooltip("XR Origin / AR Session Origin GameObject containing the AR Camera.")]
        [SerializeField] private GameObject xrOrigin;
        [Tooltip("AR Placement Manager GameObject.")]
        [SerializeField] private GameObject arPlacementManagerRoot;

        public AppMode CurrentMode => currentMode;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Ensure singleton roots remain active in Awake so all singletons initialize properly
            if (viewerProductRoot != null && !viewerProductRoot.activeSelf)
            {
                viewerProductRoot.SetActive(true);
            }
            if (arPlacementManagerRoot != null && !arPlacementManagerRoot.activeSelf)
            {
                arPlacementManagerRoot.SetActive(true);
            }

            // Ensure screen doesn't dim during AR presentation
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            // Initial boot mode is 3D Viewer
            ApplyMode(AppMode.Viewer3D);
        }

        public void SwitchToViewerMode()
        {
            ApplyMode(AppMode.Viewer3D);
        }

        public void SwitchToARMode()
        {
            StartCoroutine(CheckAndStartARSession());
        }

        private IEnumerator CheckAndStartARSession()
        {
#if UNITY_ANDROID
            // Request camera permission on Android if not yet granted
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                float timeout = 4.0f;
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Debug.LogWarning("[AppManager] Camera permission denied by user.");
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowToast("Camera permission is required for AR mode.");
                    }
                    yield break;
                }
            }
#endif

            // Verify AR availability via ARSession
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[AppManager] ARCore is not supported on this device.");
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowToast("ARCore is not supported on this device. Staying in 3D Viewer.");
                }
                yield break;
            }

            ApplyMode(AppMode.AugmentedReality);
        }

        private void ApplyMode(AppMode mode)
        {
            currentMode = mode;

            bool isViewer = (mode == AppMode.Viewer3D);
            bool isAR = (mode == AppMode.AugmentedReality);

            // Toggle 3D Viewer Cameras and Lights
            if (viewerCamera != null) viewerCamera.gameObject.SetActive(isViewer);
            if (viewerLightingRig != null) viewerLightingRig.SetActive(isViewer);

            // Keep viewerProductRoot active so ProductController singleton lives; toggle child visuals/components instead
            if (viewerProductRoot != null)
            {
                viewerProductRoot.SetActive(true);

                var rotator = viewerProductRoot.GetComponent<ProductRotator>();
                if (rotator != null) rotator.enabled = isViewer;

                var scaler = viewerProductRoot.GetComponent<ProductScaler>();
                if (scaler != null) scaler.enabled = isViewer;

                // Toggle visibility of merchandise models under turntable
                for (int i = 0; i < viewerProductRoot.transform.childCount; i++)
                {
                    Transform child = viewerProductRoot.transform.GetChild(i);
                    if (child != null)
                    {
                        if (isViewer)
                        {
                            if (ProductController.Instance != null && ProductController.Instance.CurrentProduct != null)
                            {
                                child.gameObject.SetActive(child.gameObject == ProductController.Instance.CurrentProduct.productRoot);
                            }
                            else
                            {
                                child.gameObject.SetActive(i == 0);
                            }
                        }
                        else
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // Keep arPlacementManagerRoot active so ARPlacementManager singleton lives; toggle component enabled state
            if (arPlacementManagerRoot != null)
            {
                arPlacementManagerRoot.SetActive(true);
                var placementMgr = arPlacementManagerRoot.GetComponent<ARPlacementManager>();
                if (placementMgr != null)
                {
                    placementMgr.enabled = isAR;
                    if (!isAR)
                    {
                        placementMgr.SetReticleVisible(false);
                    }
                }
            }

            // Toggle XR Origin
            if (xrOrigin != null) xrOrigin.SetActive(isAR);

            if (arSession != null)
            {
                arSession.enabled = isAR;
                if (isAR)
                {
                    arSession.Reset();
                }
            }

            // Inform UI
            if (UIManager.Instance != null)
            {
                if (isViewer)
                {
                    UIManager.Instance.ShowViewerMode();
                }
                else
                {
                    UIManager.Instance.ShowARMode();
                }
            }

            Debug.Log($"[AppManager] Switched to mode: {mode}");
        }
    }
}
