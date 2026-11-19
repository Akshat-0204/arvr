using System;
using System.Collections.Generic;
using UnityEngine;

namespace MerchAR.Product
{
    [System.Serializable]
    public class ProductItem
    {
        public string productId;
        public string displayName;
        public GameObject productRoot;
        public MeshRenderer[] targetRenderers;
        public Vector3 defaultLocalPosition = Vector3.zero;
        public Vector3 defaultLocalEulerAngles = Vector3.zero;
        public Vector3 defaultLocalScale = Vector3.one;
    }

    /// <summary>
    /// Central manager for merchandise models in the viewer.
    /// Handles product switching, active state toggles, and view reset.
    /// </summary>
    public class ProductController : MonoBehaviour
    {
        public static ProductController Instance { get; private set; }

        [Header("Product Catalog")]
        [SerializeField] private List<ProductItem> products = new List<ProductItem>();
        [SerializeField] private int initialProductIndex = 0;

        [Header("Turntable Root")]
        [Tooltip("Transform that rotates and scales in 3D viewer mode.")]
        [SerializeField] private Transform turntableRoot;

        public event Action<ProductItem> OnProductChanged;

        private int currentProductIndex = -1;

        public ProductItem CurrentProduct => (currentProductIndex >= 0 && currentProductIndex < products.Count) 
            ? products[currentProductIndex] 
            : null;

        public int CurrentIndex => currentProductIndex;
        public int TotalProducts => products.Count;
        public Transform TurntableRoot => turntableRoot;

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

            if (turntableRoot == null)
            {
                turntableRoot = transform;
            }
        }

        private void Start()
        {
            if (products.Count > 0)
            {
                int startIndex = Mathf.Clamp(initialProductIndex, 0, products.Count - 1);
                SelectProduct(startIndex);
            }
            else
            {
                Debug.LogWarning("[ProductController] No products registered in the inspector.");
            }
        }

        public void SelectProduct(int index)
        {
            if (index < 0 || index >= products.Count)
            {
                Debug.LogError($"[ProductController] Index {index} is out of bounds (Total: {products.Count}).");
                return;
            }

            currentProductIndex = index;

            for (int i = 0; i < products.Count; i++)
            {
                bool isActive = (i == currentProductIndex);
                if (products[i].productRoot != null)
                {
                    products[i].productRoot.SetActive(isActive);
                }
            }

            ResetView();

            Debug.Log($"[ProductController] Switched to product: {products[currentProductIndex].displayName}");
            OnProductChanged?.Invoke(products[currentProductIndex]);
        }

        public void NextProduct()
        {
            if (products.Count <= 1) return;
            int next = (currentProductIndex + 1) % products.Count;
            SelectProduct(next);
        }

        public void PreviousProduct()
        {
            if (products.Count <= 1) return;
            int prev = (currentProductIndex - 1 + products.Count) % products.Count;
            SelectProduct(prev);
        }

        public void ResetView()
        {
            if (turntableRoot != null)
            {
                turntableRoot.localPosition = Vector3.zero;
                turntableRoot.localRotation = Quaternion.identity;
                turntableRoot.localScale = Vector3.one;
            }

            var current = CurrentProduct;
            if (current != null && current.productRoot != null)
            {
                current.productRoot.transform.localPosition = current.defaultLocalPosition;
                current.productRoot.transform.localEulerAngles = current.defaultLocalEulerAngles;
                current.productRoot.transform.localScale = current.defaultLocalScale;
            }
        }
    }
}
