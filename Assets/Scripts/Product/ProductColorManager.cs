using System;
using System.Collections.Generic;
using UnityEngine;

namespace MerchAR.Product
{
    [System.Serializable]
    public struct ColorOption
    {
        public string colorName;
        public Color colorValue;

        public ColorOption(string name, Color color)
        {
            colorName = name;
            colorValue = color;
        }
    }

    /// <summary>
    /// Manages real-time material color switching for the active merchandise model.
    /// Uses MaterialPropertyBlock to prevent altering shared project assets or causing memory leaks.
    /// Supports Universal Render Pipeline (_BaseColor) with fallback to legacy (_Color).
    /// </summary>
    public class ProductColorManager : MonoBehaviour
    {
        public static ProductColorManager Instance { get; private set; }

        [Header("Material Property Settings")]
        [Tooltip("Shader property name for URP Lit shader Base Color.")]
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private string fallbackColorPropertyName = "_Color";

        [Header("Color Palette")]
        [SerializeField] private List<ColorOption> availableColors = new List<ColorOption>()
        {
            new ColorOption("Black", new Color(0.12f, 0.12f, 0.13f, 1f)),
            new ColorOption("White", new Color(0.95f, 0.95f, 0.97f, 1f)),
            new ColorOption("Red", new Color(0.85f, 0.18f, 0.18f, 1f)),
            new ColorOption("Blue", new Color(0.18f, 0.38f, 0.88f, 1f)),
            new ColorOption("Green", new Color(0.18f, 0.65f, 0.32f, 1f)),
            new ColorOption("Purple", new Color(0.55f, 0.22f, 0.78f, 1f))
        };

        public event Action<ColorOption> OnColorChanged;

        private MaterialPropertyBlock propertyBlock;
        private ColorOption currentColor;
        private int colorPropertyId;
        private int fallbackPropertyId;

        public IReadOnlyList<ColorOption> AvailableColors => availableColors;
        public ColorOption CurrentColor => currentColor;

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

            propertyBlock = new MaterialPropertyBlock();
            colorPropertyId = Shader.PropertyToID(colorPropertyName);
            fallbackPropertyId = Shader.PropertyToID(fallbackColorPropertyName);

            if (availableColors.Count > 0)
            {
                currentColor = availableColors[0];
            }
        }

        private void Start()
        {
            // Subscribe to product changes to apply current color immediately to newly selected product
            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged += HandleProductChanged;
                if (ProductController.Instance.CurrentProduct != null)
                {
                    ApplyColorToProduct(ProductController.Instance.CurrentProduct, currentColor.colorValue);
                }
            }
        }

        private void OnDestroy()
        {
            if (ProductController.Instance != null)
            {
                ProductController.Instance.OnProductChanged -= HandleProductChanged;
            }
        }

        private void HandleProductChanged(ProductItem product)
        {
            ApplyColorToProduct(product, currentColor.colorValue);
        }

        public void SetColorByIndex(int index)
        {
            if (index < 0 || index >= availableColors.Count)
            {
                Debug.LogWarning($"[ProductColorManager] Color index {index} out of range.");
                return;
            }

            SetColor(availableColors[index]);
        }

        public void SetColor(ColorOption option)
        {
            currentColor = option;

            if (ProductController.Instance != null)
            {
                ApplyColorToProduct(ProductController.Instance.CurrentProduct, option.colorValue);
            }

            Debug.Log($"[ProductColorManager] Applied color: {option.colorName} ({option.colorValue})");
            OnColorChanged?.Invoke(option);
        }

        /// <summary>
        /// Applies color to all renderers of a given product using MaterialPropertyBlock.
        /// </summary>
        public void ApplyColorToProduct(ProductItem product, Color color)
        {
            if (product == null || product.productRoot == null) return;

            MeshRenderer[] renderers = product.targetRenderers;
            if (renderers == null || renderers.Length == 0)
            {
                renderers = product.productRoot.GetComponentsInChildren<MeshRenderer>(true);
            }

            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning($"[ProductColorManager] No MeshRenderer found on product: {product.displayName}");
                return;
            }

            foreach (var rend in renderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(colorPropertyId, color);
                propertyBlock.SetColor(fallbackPropertyId, color);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Utility method to apply color to an arbitrary GameObject (e.g. an AR-placed instance).
        /// </summary>
        public void ApplyColorToHierarchy(GameObject root, Color color)
        {
            if (root == null) return;

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(colorPropertyId, color);
                propertyBlock.SetColor(fallbackPropertyId, color);
                rend.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
