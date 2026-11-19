using System;
using UnityEngine;
using UnityEngine.UI;
using MerchAR.Core;
using MerchAR.AR;
using MerchAR.Product;

namespace MerchAR.UI
{
    /// <summary>
    /// Master UI Manager for MerchAR.
    /// Controls navigation panels, mode transitions (Viewer vs AR), action buttons,
    /// and contextual instruction messages for surface detection and merchandise placement.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Main Panels")]
        [Tooltip("The main 3D viewer UI panel containing product info, swatches, and navigation.")]
        [SerializeField] private GameObject viewerPanel;
        [Tooltip("The AR HUD panel containing camera view controls, scan banner, and placement actions.")]
        [SerializeField] private GameObject arPanel;

        [Header("Viewer Mode Controls")]
        [SerializeField] private Button enterArButton;
        [SerializeField] private Button resetViewButton;
        [SerializeField] private Text productNameText;
        [SerializeField] private Text productPriceText;

        [Header("AR Mode Controls")]
        [SerializeField] private Button exitArButton;
        [SerializeField] private Button removePlacedObjectButton;
        [SerializeField] private Button resetArPoseButton;
        [SerializeField] private GameObject instructionBanner;
        [SerializeField] private Text instructionText;

        [Header("Status & Notification")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastText;
        [SerializeField] private float toastDuration = 3f;

        private float toastTimer = 0f;

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
        }

        private void Start()
        {
            BindButtons();

            // Subscribe to ProductController events
            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged += UpdateProductInfo;
                if (ProductController.Instance.CurrentProduct != null)
                {
                    UpdateProductInfo(ProductController.Instance.CurrentProduct);
                }
            }

            // Subscribe to AR events
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.OnObjectPlaced += HandleARObjectPlaced;
                ARPlacementManager.Instance.OnObjectRemoved += HandleARObjectRemoved;
                ARPlacementManager.Instance.OnPlaneDetectionStatusChanged += HandlePlaneStatusChanged;
            }

            // Start in Viewer mode
            ShowViewerMode();
        }

        private void Update()
        {
            if (toastPanel != null && toastPanel.activeSelf)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f)
                {
                    toastPanel.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged -= UpdateProductInfo;
            }

            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.OnObjectPlaced -= HandleARObjectPlaced;
                ARPlacementManager.Instance.OnObjectRemoved -= HandleARObjectRemoved;
                ARPlacementManager.Instance.OnPlaneDetectionStatusChanged -= HandlePlaneStatusChanged;
            }
        }

        private void BindButtons()
        {
            if (enterArButton != null)
            {
                enterArButton.onClick.AddListener(() =>
                {
                    if (AppManager.Instance != null)
                    {
                        AppManager.Instance.SwitchToARMode();
                    }
                });
            }

            if (exitArButton != null)
            {
                exitArButton.onClick.AddListener(() =>
                {
                    if (AppManager.Instance != null)
                    {
                        AppManager.Instance.SwitchToViewerMode();
                    }
                });
            }

            if (resetViewButton != null)
            {
                resetViewButton.onClick.AddListener(() =>
                {
                    if (ProductController.Instance != null)
                    {
                        ProductController.Instance.ResetView();
                    }
                });
            }

            if (removePlacedObjectButton != null)
            {
                removePlacedObjectButton.onClick.AddListener(() =>
                {
                    if (ARPlacementManager.Instance != null)
                    {
                        ARPlacementManager.Instance.RemovePlacedObject();
                    }
                });
            }

            if (resetArPoseButton != null)
            {
                resetArPoseButton.onClick.AddListener(() =>
                {
                    if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.CurrentPlacedObject != null)
                    {
                        ARPlacementManager.Instance.CurrentPlacedObject.transform.localScale = Vector3.one * 0.5f;
                    }
                });
            }
        }

        public void ShowViewerMode()
        {
            if (viewerPanel != null) viewerPanel.SetActive(true);
            if (arPanel != null) arPanel.SetActive(false);
        }

        public void ShowARMode()
        {
            if (viewerPanel != null) viewerPanel.SetActive(false);
            if (arPanel != null) arPanel.SetActive(true);

            if (removePlacedObjectButton != null)
            {
                removePlacedObjectButton.gameObject.SetActive(false);
            }
            if (resetArPoseButton != null)
            {
                resetArPoseButton.gameObject.SetActive(false);
            }

            SetInstruction("Point phone at a flat floor or table and move slowly to scan.");
        }

        private void UpdateProductInfo(ProductItem product)
        {
            if (product == null) return;

            if (productNameText != null)
            {
                productNameText.text = product.displayName;
            }

            if (productPriceText != null)
            {
                productPriceText.text = product.productId.ToLower().Contains("hoodie") ? "$49.99" : "$29.99";
            }
        }

        private void HandleARObjectPlaced(GameObject placed)
        {
            if (removePlacedObjectButton != null)
            {
                removePlacedObjectButton.gameObject.SetActive(true);
            }
            if (resetArPoseButton != null)
            {
                resetArPoseButton.gameObject.SetActive(true);
            }

            SetInstruction("Merchandise placed! 1-finger swipe to rotate, pinch to scale.");
        }

        private void HandleARObjectRemoved()
        {
            if (removePlacedObjectButton != null)
            {
                removePlacedObjectButton.gameObject.SetActive(false);
            }
            if (resetArPoseButton != null)
            {
                resetArPoseButton.gameObject.SetActive(false);
            }

            SetInstruction("Scan surface and tap when reticle appears to place.");
        }

        private void HandlePlaneStatusChanged(bool hasPlane)
        {
            if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.IsObjectPlaced)
            {
                return;
            }

            if (hasPlane)
            {
                SetInstruction("Surface detected! Tap screen to place merchandise.");
            }
            else
            {
                SetInstruction("Point phone at a textured surface or floor to detect ground.");
            }
        }

        public void SetInstruction(string message)
        {
            if (instructionBanner != null)
            {
                instructionBanner.SetActive(!string.IsNullOrEmpty(message));
            }
            if (instructionText != null)
            {
                instructionText.text = message;
            }
        }

        public void ShowToast(string message, float duration = 3f)
        {
            if (toastText != null)
            {
                toastText.text = message;
            }
            if (toastPanel != null)
            {
                toastPanel.SetActive(true);
            }
            toastTimer = duration;
        }
    }
}
