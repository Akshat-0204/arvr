using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelAR.Interaction
{
    /// <summary>
    /// Reads touch (device) or mouse (editor / XR Simulation) once per frame and exposes at most one
    /// world tap. Consumers call <see cref="TryConsumeTap"/> so a single tap is never used twice
    /// (e.g. placing the build area and building a block in the same frame).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class TapInput : MonoBehaviour
    {
        public static TapInput Instance { get; private set; }

        private readonly TapTracker tracker = new TapTracker();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private PointerEventData pointerData;

        private bool tapPending;
        private Vector2 tapPosition;

        /// <summary>Number of fingers currently down (0 for mouse).</summary>
        public int TouchCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            tapPending = false;
            TouchCount = UnityEngine.Input.touchCount;

            if (TouchCount > 0)
            {
                Touch t = UnityEngine.Input.GetTouch(0);
                bool overUI = t.phase == TouchPhase.Began && IsOverUI(t.position);
                if (tracker.Process(TouchCount, t.phase, t.position, Time.unscaledTime, overUI))
                    Raise(t.position);
                return;
            }

            if (!UnityEngine.Input.mousePresent) { tracker.Process(0, TouchPhase.Ended, default, 0, false); return; }

            Vector2 mouse = UnityEngine.Input.mousePosition;
            if (UnityEngine.Input.GetMouseButtonDown(0))
                tracker.Process(1, TouchPhase.Began, mouse, Time.unscaledTime, IsOverUI(mouse));
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                if (tracker.Process(1, TouchPhase.Ended, mouse, Time.unscaledTime, false)) Raise(mouse);
            }
            else if (UnityEngine.Input.GetMouseButton(0))
                tracker.Process(1, TouchPhase.Moved, mouse, Time.unscaledTime, false);
        }

        private void Raise(Vector2 pos)
        {
            tapPending = true;
            tapPosition = pos;
        }

        public bool TryConsumeTap(out Vector2 position)
        {
            position = tapPosition;
            if (!tapPending) return false;
            tapPending = false;
            return true;
        }

        /// <summary>UI raycast at a screen point. More reliable than IsPointerOverGameObject on the touch-began frame.</summary>
        public bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            pointerData ??= new PointerEventData(es);
            pointerData.position = screenPos;
            uiHits.Clear();
            es.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }
    }
}
