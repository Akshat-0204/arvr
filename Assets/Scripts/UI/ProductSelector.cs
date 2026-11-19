using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MerchAR.Product;

namespace MerchAR.UI
{
    [System.Serializable]
    public class ProductButtonBinding
    {
        public int productIndex;
        public Button button;
        public Image activeIndicatorImage;
        public Text labelText;
    }

    /// <summary>
    /// Handles product switching buttons in the UI.
    /// Highlights the currently selected merchandise and triggers ProductController.
    /// </summary>
    public class ProductSelector : MonoBehaviour
    {
        [Header("Button Bindings")]
        [SerializeField] private List<ProductButtonBinding> productButtons = new List<ProductButtonBinding>();

        [Header("Colors for Active State")]
        [SerializeField] private Color activeTextColor = Color.black;
        [SerializeField] private Color inactiveTextColor = new Color(0.4f, 0.4f, 0.4f, 1f);

        private void Start()
        {
            RegisterButtonCallbacks();

            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged += HandleProductChanged;
                UpdateVisualState(ProductController.Instance.CurrentIndex);
            }
        }

        private void OnDestroy()
        {
            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged -= HandleProductChanged;
            }
        }

        private void RegisterButtonCallbacks()
        {
            foreach (var binding in productButtons)
            {
                if (binding.button != null)
                {
                    int idx = binding.productIndex;
                    binding.button.onClick.AddListener(() => OnButtonClicked(idx));
                }
            }
        }

        private void OnButtonClicked(int index)
        {
            if (ProductController.Instance != null)
            {
                ProductController.Instance.SelectProduct(index);
            }
        }

        private void HandleProductChanged(ProductItem product)
        {
            if (ProductController.Instance != null)
            {
                UpdateVisualState(ProductController.Instance.CurrentIndex);
            }
        }

        public void UpdateVisualState(int selectedIndex)
        {
            foreach (var binding in productButtons)
            {
                bool isSelected = (binding.productIndex == selectedIndex);

                if (binding.activeIndicatorImage != null)
                {
                    binding.activeIndicatorImage.enabled = isSelected;
                }

                if (binding.labelText != null)
                {
                    binding.labelText.color = isSelected ? activeTextColor : inactiveTextColor;
                    binding.labelText.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                }
            }
        }
    }
}
