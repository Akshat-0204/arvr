using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MerchAR.Product;

namespace MerchAR.UI
{
    [System.Serializable]
    public class ColorSwatchBinding
    {
        public int colorIndex;
        public Button button;
        public Image swatchImage;
        public GameObject selectionRing;
    }

    /// <summary>
    /// Manages the color palette UI buttons.
    /// Provides visual rings around the active color and coordinates with ProductColorManager.
    /// </summary>
    public class ColorSelector : MonoBehaviour
    {
        [Header("Color Swatch Bindings")]
        [SerializeField] private List<ColorSwatchBinding> swatches = new List<ColorSwatchBinding>();

        private void Start()
        {
            InitializeSwatches();

            if (ProductColorManager.Instance != null)
            {
                ProductColorManager.Instance.OnColorChanged += HandleColorChanged;
            }
        }

        private void OnDestroy()
        {
            if (ProductColorManager.Instance != null)
            {
                ProductColorManager.Instance.OnColorChanged -= HandleColorChanged;
            }
        }

        private void InitializeSwatches()
        {
            if (ProductColorManager.Instance == null) return;

            var colors = ProductColorManager.Instance.AvailableColors;

            for (int i = 0; i < swatches.Count; i++)
            {
                var swatch = swatches[i];
                int idx = swatch.colorIndex;

                if (idx >= 0 && idx < colors.Count)
                {
                    if (swatch.swatchImage != null)
                    {
                        swatch.swatchImage.color = colors[idx].colorValue;
                    }
                }

                if (swatch.button != null)
                {
                    swatch.button.onClick.AddListener(() => OnSwatchClicked(idx));
                }
            }

            // Highlight initial color (0)
            HighlightSwatch(0);
        }

        private void OnSwatchClicked(int index)
        {
            if (ProductColorManager.Instance != null)
            {
                ProductColorManager.Instance.SetColorByIndex(index);
            }
            HighlightSwatch(index);
        }

        private void HandleColorChanged(ColorOption colorOption)
        {
            if (ProductColorManager.Instance == null) return;
            var list = ProductColorManager.Instance.AvailableColors;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].colorName == colorOption.colorName)
                {
                    HighlightSwatch(i);
                    break;
                }
            }
        }

        public void HighlightSwatch(int selectedIndex)
        {
            foreach (var swatch in swatches)
            {
                bool isSelected = (swatch.colorIndex == selectedIndex);
                if (swatch.selectionRing != null)
                {
                    swatch.selectionRing.SetActive(isSelected);
                }
            }
        }
    }
}
