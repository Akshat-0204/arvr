using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace VoxelAR.AR
{
    /// <summary>
    /// Hides plane visuals while the build area is placed (including planes detected afterwards)
    /// and shows them again when it is removed.
    /// </summary>
    public class ARPlaneController : MonoBehaviour
    {
        private ARPlaneManager planeManager;
        private WorldPlacement placement;
        private bool planesVisible = true;

        public void Init(ARPlaneManager planes, WorldPlacement worldPlacement)
        {
            planeManager = planes;
            placement = worldPlacement;
            placement.Placed += _ => SetPlanesActive(false);
            placement.Removed += () => SetPlanesActive(true);
            planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        }

        private void OnDestroy()
        {
            if (planeManager != null) planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            if (planesVisible) return;
            foreach (var p in args.added)
                if (p != null) p.gameObject.SetActive(false);
        }

        public void SetPlanesActive(bool active)
        {
            planesVisible = active;
            if (planeManager == null) return;
            foreach (var plane in planeManager.trackables)
                if (plane != null) plane.gameObject.SetActive(active);
        }
    }
}
