using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace VoxelAR.AR
{
    /// <summary>
    /// Manages the visibility of detected AR planes and controls plane rendering behavior.
    /// Can automatically hide plane visualizations when merchandise is placed to keep the AR view clean.
    /// </summary>
    public class ARPlaneController : MonoBehaviour
    {
        [Header("AR Foundation Reference")]
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Settings")]
        [Tooltip("Hide plane visualization meshes once an object has been placed.")]
        [SerializeField] private bool hidePlanesAfterPlacement = true;

        private bool planesVisible = true;

        private void Awake()
        {
            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
        }

        private void Start()
        {
            if (ARPlacementManager.Instance != null && hidePlanesAfterPlacement)
            {
                ARPlacementManager.Instance.OnObjectPlaced += HandleObjectPlaced;
                ARPlacementManager.Instance.OnObjectRemoved += HandleObjectRemoved;
            }
        }

        private void OnDestroy()
        {
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.OnObjectPlaced -= HandleObjectPlaced;
                ARPlacementManager.Instance.OnObjectRemoved -= HandleObjectRemoved;
            }
        }

        private void HandleObjectPlaced(GameObject placed)
        {
            SetPlanesActive(false);
        }

        private void HandleObjectRemoved()
        {
            SetPlanesActive(true);
        }

        public void TogglePlanesVisibility()
        {
            SetPlanesActive(!planesVisible);
        }

        public void SetPlanesActive(bool active)
        {
            planesVisible = active;

            if (planeManager == null) return;

            foreach (var plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    plane.gameObject.SetActive(active);
                }
            }

            Debug.Log($"[ARPlaneController] Planes visibility set to: {active}");
        }

        public void ClearAllPlanes()
        {
            if (planeManager == null) return;

            foreach (var plane in planeManager.trackables)
            {
                if (plane != null)
                {
                    Destroy(plane.gameObject);
                }
            }
        }
    }
}
