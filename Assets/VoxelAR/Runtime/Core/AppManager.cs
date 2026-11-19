using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using VoxelAR.AR;
using VoxelAR.Save;
using VoxelAR.UI;
using VoxelAR.Voxel;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace VoxelAR.Core
{
    /// <summary>
    /// App controller. Owns the build data (grid, undo history, save slots), checks camera permission and
    /// ARCore availability before enabling the AR session, autosaves to slot 1 and reports tracking loss.
    /// </summary>
    public class AppManager : MonoBehaviour, UIManager.IActions
    {
        public const int AutosaveSlot = 0;

        public static AppManager Instance { get; private set; }

        public VoxelGrid Grid { get; private set; }
        public EditHistory History { get; private set; }
        public SaveSystem Saves { get; private set; }

        private ARSession arSession;
        private UIManager ui;
        private WorldPlacement placement;
        private bool hasTracked;
        private bool trackingLost;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Grid = new VoxelGrid(GridMath.DefaultSize);
            History = new EditHistory(Grid);
            Saves = new SaveSystem();
        }

        public void Init(ARSession session, UIManager uiManager, WorldPlacement worldPlacement)
        {
            arSession = session;
            ui = uiManager;
            placement = worldPlacement;
        }

        private void OnEnable() => ARSession.stateChanged += OnSessionStateChanged;
        private void OnDisable() => ARSession.stateChanged -= OnSessionStateChanged;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            RestoreAutosave();
            StartCoroutine(CheckAndStartARSession());
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Autosave();
        }

        private void OnApplicationQuit() => Autosave();

        // ───────────────────────── session ─────────────────────────

        public void RetryStart()
        {
            StopAllCoroutines();
            StartCoroutine(CheckAndStartARSession());
        }

        private IEnumerator CheckAndStartARSession()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                bool answered = false;
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => answered = true;
                callbacks.PermissionDenied += _ => answered = true;
                Permission.RequestUserPermission(Permission.Camera, callbacks);

                float timeout = 30f;
                while (!answered && timeout > 0f)
                {
                    timeout -= Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    Debug.LogWarning("[AppManager] Camera permission denied.");
                    ui?.ShowBlockingMessage("VoxelAR needs the camera to show your build in the room.\n\n" +
                                            "If no prompt appears, allow Camera in Settings › Apps › VoxelAR.",
                                            "Retry", RetryStart);
                    yield break;
                }
            }
#endif

            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
                yield return ARSession.CheckAvailability();

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("[AppManager] AR is not supported on this device.");
                ui?.ShowBlockingMessage("This phone doesn't support ARCore, so VoxelAR can't run on it.");
                yield break;
            }

            ui?.HideBlockingMessage();
            if (arSession != null) arSession.enabled = true;
            Debug.Log("[AppManager] AR session started.");
        }

        private void OnSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            if (args.state == ARSessionState.SessionTracking)
            {
                hasTracked = true;
                if (trackingLost)
                {
                    trackingLost = false;
                    ui?.SetStatusOverride(null);
                }
            }
            else if (hasTracked && args.state == ARSessionState.SessionInitializing)
            {
                trackingLost = true;
                ui?.SetStatusOverride(TrackingLostMessage(ARSession.notTrackingReason));
            }
        }

        private static string TrackingLostMessage(NotTrackingReason reason)
        {
            switch (reason)
            {
                case NotTrackingReason.InsufficientLight: return "Too dark — turn on a light";
                case NotTrackingReason.InsufficientFeatures: return "Point at a surface with more texture";
                case NotTrackingReason.ExcessiveMotion: return "Moving too fast — slow down";
                default: return "Tracking lost — hold still and look around slowly";
            }
        }

        // ───────────────────────── build data ─────────────────────────

        private void RestoreAutosave()
        {
            if (!Saves.Exists(AutosaveSlot)) return;
            if (Saves.Load(AutosaveSlot, Grid, out string error))
            {
                History.ResetHistory();
                if (Grid.Count > 0) ui?.ShowToast($"Restored your last build ({Grid.Count} blocks)");
            }
            else
            {
                Debug.LogWarning("[AppManager] Autosave not restored: " + error);
            }
        }

        private void Autosave()
        {
            if (Grid == null) return;
            if (!Saves.Save(AutosaveSlot, Grid)) Debug.LogWarning("[AppManager] Autosave failed: " + Saves.LastError);
        }

        public void SaveSlot(int slot)
        {
            if (Saves.Save(slot, Grid)) ui?.ShowToast($"Saved to slot {slot + 1}");
            else ui?.ShowToast("Save failed: " + Saves.LastError);
        }

        public void LoadSlot(int slot)
        {
            if (!Saves.Exists(slot)) { ui?.ShowToast($"Slot {slot + 1} is empty"); return; }
            if (Saves.Load(slot, Grid, out string error))
            {
                History.ResetHistory();
                ui?.ShowToast($"Loaded slot {slot + 1} ({Grid.Count} blocks)");
            }
            else
            {
                ui?.ShowToast("Load failed: " + error);
            }
        }

        public void ClearBuild()
        {
            if (History.Clear())
            {
                Autosave();
                ui?.ShowToast("Build cleared — Undo to bring it back");
            }
        }

        public void Replace()
        {
            if (placement != null) placement.ResetPlacement();
        }
    }
}
