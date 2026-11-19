using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using VoxelAR.Interaction;
using VoxelAR.Voxel;

namespace VoxelAR.AR
{
    /// <summary>
    /// Shows a reticle (build-area footprint) on detected planes and, on the first tap, places the
    /// build area: WorldRoot (anchored) + base plate + <see cref="VoxelWorldView"/> + pinch/twist manipulator.
    /// Grid data lives outside WorldRoot, so <see cref="ResetPlacement"/> and re-placing keeps the build.
    /// </summary>
    public class WorldPlacement : MonoBehaviour
    {
        private ARRaycastManager raycastManager;
        private ARAnchorManager anchorManager;
        private Camera arCamera;
        private GameObject reticle;
        private VoxelGrid grid;
        private float blockSize;

        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private Pose reticlePose;
        private bool reticleValid;
        private bool lastPlaneStatus;
        private bool firedInitialStatus;
        private bool placing;
        private ARAnchor anchor;

        public Transform WorldRoot { get; private set; }
        public VoxelWorldView View { get; private set; }
        public bool IsPlaced => WorldRoot != null;
        public bool IsPlaneDetected => reticleValid;

        public event Action<Transform> Placed;
        public event Action Removed;
        public event Action<bool> PlaneStatusChanged;

        public void Init(ARRaycastManager raycast, ARAnchorManager anchors, Camera cam, GameObject reticleObject,
                         VoxelGrid voxelGrid, float voxelBlockSize)
        {
            raycastManager = raycast;
            anchorManager = anchors;
            arCamera = cam;
            reticle = reticleObject;
            grid = voxelGrid;
            blockSize = voxelBlockSize;
            if (reticle != null) reticle.SetActive(false);
        }

        private void Update()
        {
            if (IsPlaced || placing)
            {
                if (reticle != null && reticle.activeSelf) reticle.SetActive(false);
                return;
            }

            UpdateReticle();

            if (reticleValid && TapInput.Instance != null && TapInput.Instance.TryConsumeTap(out _))
                Place(reticlePose);
        }

        private void UpdateReticle()
        {
            if (raycastManager == null) return;

            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            reticleValid = raycastManager.Raycast(centre, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0;

            if (reticleValid)
            {
                reticlePose = new Pose(hits[0].pose.position, FacingCamera(hits[0].pose));
                if (reticle != null)
                {
                    reticle.SetActive(true);
                    reticle.transform.SetPositionAndRotation(reticlePose.position, reticlePose.rotation);
                }
            }
            else if (reticle != null)
            {
                reticle.SetActive(false);
            }

            if (!firedInitialStatus || reticleValid != lastPlaneStatus)
            {
                firedInitialStatus = true;
                lastPlaneStatus = reticleValid;
                PlaneStatusChanged?.Invoke(reticleValid);
            }
        }

        /// <summary>Yaw the pose so the build area's front (−Z) faces the camera; keep it level with the plane.</summary>
        private Quaternion FacingCamera(Pose planePose)
        {
            if (arCamera == null) return planePose.rotation;
            Vector3 up = planePose.up;
            Vector3 toCam = Vector3.ProjectOnPlane(arCamera.transform.position - planePose.position, up);
            if (toCam.sqrMagnitude < 1e-6f) return planePose.rotation;
            return Quaternion.LookRotation(-toCam.normalized, up);
        }

        public async void Place(Pose pose)
        {
            if (IsPlaced || placing) return;
            placing = true;

            ARAnchor newAnchor = null;
            if (anchorManager != null && anchorManager.enabled)
            {
                try
                {
                    var result = await anchorManager.TryAddAnchorAsync(pose);
                    if (result.status.IsSuccess()) newAnchor = result.value;
                    else Debug.LogWarning($"[WorldPlacement] Anchor failed ({result.status}); using plain pose.");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[WorldPlacement] Anchor exception, using plain pose: " + e.Message);
                }
            }

            if (this == null) return; // destroyed while awaiting
            placing = false;
            anchor = newAnchor;

            var root = new GameObject("WorldRoot").transform;
            if (anchor != null) root.SetParent(anchor.transform, false);
            root.SetPositionAndRotation(pose.position, pose.rotation);

            View = root.gameObject.AddComponent<VoxelWorldView>();
            View.Bind(grid, blockSize);
            root.gameObject.AddComponent<ARObjectManipulator>();
            WorldRoot = root;

            if (reticle != null) reticle.SetActive(false);
            Debug.Log($"[WorldPlacement] Build area placed at {pose.position} (anchored: {anchor != null}).");
            Placed?.Invoke(root);
        }

        /// <summary>Removes the build area from the scene. Grid data is kept.</summary>
        public void ResetPlacement()
        {
            if (!IsPlaced) return;
            View.Unbind();
            SafeDestroy(WorldRoot.gameObject);
            WorldRoot = null;
            View = null;

            if (anchor != null)
            {
                if (anchorManager == null || !anchorManager.TryRemoveAnchor(anchor))
                    SafeDestroy(anchor.gameObject);
                anchor = null;
            }

            firedInitialStatus = false;
            Removed?.Invoke();
        }

        private static void SafeDestroy(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
