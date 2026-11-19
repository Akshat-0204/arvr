using UnityEngine;
using UnityEngine.UI;
using VoxelAR.AR;

namespace VoxelAR.UI
{
    /// <summary>
    /// Build UI controller: instruction banner and toast. Buttons are added by UIBuilder (Task 7).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Instruction")]
        [SerializeField] private GameObject instructionBanner;
        [SerializeField] private Text instructionText;

        [Header("Toast")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastText;

        private float toastTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.OnObjectPlaced += HandlePlaced;
                ARPlacementManager.Instance.OnObjectRemoved += HandleRemoved;
                ARPlacementManager.Instance.OnPlaneDetectionStatusChanged += HandlePlaneStatusChanged;
            }

            SetInstruction("Move phone slowly to find a table");
        }

        private void Update()
        {
            if (toastPanel != null && toastPanel.activeSelf)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f) toastPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.OnObjectPlaced -= HandlePlaced;
                ARPlacementManager.Instance.OnObjectRemoved -= HandleRemoved;
                ARPlacementManager.Instance.OnPlaneDetectionStatusChanged -= HandlePlaneStatusChanged;
            }
        }

        private void HandlePlaced(GameObject placed)
        {
            SetInstruction("Tap to build · Pinch to scale · Twist to rotate");
        }

        private void HandleRemoved()
        {
            SetInstruction("Move phone slowly to find a table");
        }

        private void HandlePlaneStatusChanged(bool hasPlane)
        {
            if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.IsObjectPlaced) return;
            SetInstruction(hasPlane ? "Tap to place your build area" : "Move phone slowly to find a table");
        }

        public void SetInstruction(string message)
        {
            if (instructionBanner != null) instructionBanner.SetActive(!string.IsNullOrEmpty(message));
            if (instructionText != null) instructionText.text = message;
        }

        public void ShowToast(string message, float duration = 3f)
        {
            if (toastText != null) toastText.text = message;
            if (toastPanel != null) toastPanel.SetActive(true);
            toastTimer = duration;
        }
    }
}
