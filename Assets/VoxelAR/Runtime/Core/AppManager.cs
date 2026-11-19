using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using VoxelAR.UI;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace VoxelAR.Core
{
    /// <summary>
    /// App controller: checks camera permission and ARCore availability, then starts the AR session.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        [SerializeField] private ARSession arSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            if (arSession == null) arSession = FindFirstObjectByType<ARSession>();
            StartCoroutine(CheckAndStartARSession());
        }

        public void RetryStart()
        {
            StopAllCoroutines();
            StartCoroutine(CheckAndStartARSession());
        }

        private IEnumerator CheckAndStartARSession()
        {
#if UNITY_ANDROID
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
                        UIManager.Instance.ShowToast("Camera permission is required.");
                    }
                    yield break;
                }
            }
#endif

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[AppManager] ARCore is not supported on this device.");
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetInstruction("This phone doesn't support ARCore.");
                }
                yield break;
            }

            if (arSession != null) arSession.enabled = true;
            Debug.Log("[AppManager] AR session started.");
        }
    }
}
