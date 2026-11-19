using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using MerchAR.Product;

namespace MerchAR.AR
{
    /// <summary>
    /// Handles AR surface detection raycasting, reticle indicator tracking,
    /// and instantiating or repositioning the selected merchandise model on real-world planes.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        public static ARPlacementManager Instance { get; private set; }

        [Header("AR Foundation References")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Camera arCamera;

        [Header("Placement Reticle Indicator")]
        [Tooltip("Visual cursor/reticle displayed on detected planes before placement.")]
        [SerializeField] private GameObject placementReticlePrefab;
        [SerializeField] private GameObject placementReticleInstance;

        [Header("Placement Configuration")]
        [SerializeField] private float initialSpawnScale = 0.5f;

        [Header("Prefab References for AR")]
        [Tooltip("Prefabs to instantiate in AR corresponding to merchandise.")]
        [SerializeField] private GameObject tshirtARPrefab;
        [SerializeField] private GameObject hoodieARPrefab;

        public event Action<GameObject> OnObjectPlaced;
        public event Action OnObjectRemoved;
        public event Action<bool> OnPlaneDetectionStatusChanged; // true if plane under reticle

        private GameObject currentPlacedObject;
        private Pose currentReticlePose;
        private bool isReticlePoseValid = false;
        private bool lastPlaneDetectionStatus = false;
        private bool hasFiredInitialPlaneStatus = false;
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public GameObject CurrentPlacedObject => currentPlacedObject;
        public bool IsObjectPlaced => currentPlacedObject != null;
        public bool IsPlaneDetected => isReticlePoseValid;

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

            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }
            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
            if (arCamera == null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null) arCamera = xrOrigin.Camera;
                if (arCamera == null) arCamera = Camera.main;
                if (arCamera == null) arCamera = FindFirstObjectByType<Camera>();
            }
        }

        private void Start()
        {
            // Create placement reticle if not assigned directly in scene
            if (placementReticleInstance == null && placementReticlePrefab != null)
            {
                placementReticleInstance = Instantiate(placementReticlePrefab);
                placementReticleInstance.SetActive(false);
            }
            else if (placementReticleInstance != null)
            {
                placementReticleInstance.SetActive(false);
            }
        }

        private void OnDisable()
        {
            SetReticleVisible(false);
        }

        private void Update()
        {
            UpdateReticlePose();
            HandlePlacementTouch();
        }

        private void UpdateReticlePose()
        {
            if (raycastManager == null || arCamera == null) return;

            // Raycast from the center of the mobile screen
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            bool hitFound = raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

            if (hitFound && hits.Count > 0)
            {
                currentReticlePose = hits[0].pose;
                isReticlePoseValid = true;

                if (placementReticleInstance != null)
                {
                    // Only show reticle if object not yet placed, or when user wants to relocate
                    placementReticleInstance.SetActive(currentPlacedObject == null);
                    placementReticleInstance.transform.SetPositionAndRotation(currentReticlePose.position, currentReticlePose.rotation);
                }
            }
            else
            {
                isReticlePoseValid = false;
                if (placementReticleInstance != null)
                {
                    placementReticleInstance.SetActive(false);
                }
            }

            // Only fire event when plane detection status changes
            if (!hasFiredInitialPlaneStatus || isReticlePoseValid != lastPlaneDetectionStatus)
            {
                lastPlaneDetectionStatus = isReticlePoseValid;
                hasFiredInitialPlaneStatus = true;
                OnPlaneDetectionStatusChanged?.Invoke(isReticlePoseValid);
            }
        }

        private void HandlePlacementTouch()
        {
            // Only place if a valid plane is found and no object is placed yet
            if (!isReticlePoseValid || currentPlacedObject != null) return;

            // Touch input on mobile
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    // Ignore touch if interacting with UI buttons
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        return;
                    }

                    PlaceActiveProduct(currentReticlePose.position, currentReticlePose.rotation);
                }
            }
            // Editor / mouse fallback
            else if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                PlaceActiveProduct(currentReticlePose.position, currentReticlePose.rotation);
            }
        }

        public void PlaceActiveProduct(Vector3 position, Quaternion rotation)
        {
            GameObject prefabToSpawn = GetActiveProductPrefab();
            if (prefabToSpawn == null)
            {
                Debug.LogError("[ARPlacementManager] No prefab assigned to spawn for current merchandise.");
                return;
            }

            // Remove existing placed model if any
            RemovePlacedObject();

            // Orient the model facing towards the camera horizontally
            Vector3 cameraPosition = arCamera != null ? arCamera.transform.position : position + Vector3.forward;
            Vector3 directionToCamera = (cameraPosition - position);
            directionToCamera.y = 0; // Keep model upright
            Quaternion facingRotation = (directionToCamera.sqrMagnitude > 0.001f)
                ? Quaternion.LookRotation(directionToCamera)
                : rotation;

            currentPlacedObject = Instantiate(prefabToSpawn, position, facingRotation);
            currentPlacedObject.SetActive(true); // Ensure clone is active even if prefab was inactive
            currentPlacedObject.transform.localScale = Vector3.one * initialSpawnScale;

            // Ensure it has an ARObjectManipulator component attached
            var manipulator = currentPlacedObject.GetComponent<ARObjectManipulator>();
            if (manipulator == null)
            {
                manipulator = currentPlacedObject.AddComponent<ARObjectManipulator>();
            }

            // Apply currently chosen color to the newly spawned AR model
            if (ProductColorManager.Instance != null)
            {
                ProductColorManager.Instance.ApplyColorToHierarchy(
                    currentPlacedObject, 
                    ProductColorManager.Instance.CurrentColor.colorValue
                );
            }

            if (placementReticleInstance != null)
            {
                placementReticleInstance.SetActive(false);
            }

            Debug.Log($"[ARPlacementManager] Successfully placed {prefabToSpawn.name} at {position}.");
            OnObjectPlaced?.Invoke(currentPlacedObject);
        }

        public void RemovePlacedObject()
        {
            if (currentPlacedObject != null)
            {
                Destroy(currentPlacedObject);
                currentPlacedObject = null;
                Debug.Log("[ARPlacementManager] Placed object removed.");
                OnObjectRemoved?.Invoke();
            }
        }

        private GameObject GetActiveProductPrefab()
        {
            if (ProductController.Instance != null && ProductController.Instance.CurrentProduct != null)
            {
                string id = ProductController.Instance.CurrentProduct.productId.ToLower();
                if (id.Contains("hoodie"))
                {
                    return hoodieARPrefab != null ? hoodieARPrefab : ProductController.Instance.CurrentProduct.productRoot;
                }
                else
                {
                    return tshirtARPrefab != null ? tshirtARPrefab : ProductController.Instance.CurrentProduct.productRoot;
                }
            }

            return tshirtARPrefab;
        }

        public void SetReticleVisible(bool visible)
        {
            if (placementReticleInstance != null)
            {
                placementReticleInstance.SetActive(visible && isReticlePoseValid && currentPlacedObject == null);
            }
        }
    }
}
