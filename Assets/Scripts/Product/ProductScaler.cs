using UnityEngine;
using UnityEngine.EventSystems;

namespace MerchAR.Product
{
    /// <summary>
    /// Handles pinch-to-zoom on mobile devices and scroll wheel zoom in the Unity Editor.
    /// Clamps scale within realistic thresholds with smooth interpolation.
    /// </summary>
    public class ProductScaler : MonoBehaviour
    {
        [Header("Target & Scaling Limits")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 2.2f;
        [SerializeField] private float defaultScale = 1.0f;

        [Header("Sensitivity & Smoothing")]
        [SerializeField] private float pinchSensitivity = 0.005f;
        [SerializeField] private float scrollSensitivity = 0.5f;
        [SerializeField] private float smoothSpeed = 10f;

        private float targetScaleValue = 1.0f;

        public float DefaultScale => defaultScale;

        private void Start()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
            targetScaleValue = defaultScale;
            ApplyScaleImmediate(defaultScale);
        }

        private void Update()
        {
            HandlePinchZoom();
            HandleScrollWheel();
            SmoothApply();
        }

        private void HandlePinchZoom()
        {
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                // Prevent zooming if fingers start on UI elements
                if (EventSystem.current != null)
                {
                    if (EventSystem.current.IsPointerOverGameObject(touch0.fingerId) ||
                        EventSystem.current.IsPointerOverGameObject(touch1.fingerId))
                    {
                        return;
                    }
                }

                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;

                float prevDistance = (prevPos0 - prevPos1).magnitude;
                float currentDistance = (touch0.position - touch1.position).magnitude;
                float deltaDistance = currentDistance - prevDistance;

                targetScaleValue += deltaDistance * pinchSensitivity;
                targetScaleValue = Mathf.Clamp(targetScaleValue, minScale, maxScale);
            }
        }

        private void HandleScrollWheel()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                targetScaleValue += scroll * scrollSensitivity;
                targetScaleValue = Mathf.Clamp(targetScaleValue, minScale, maxScale);
            }
        }

        private void SmoothApply()
        {
            if (targetTransform == null) return;

            Vector3 current = targetTransform.localScale;
            float newScale = Mathf.Lerp(current.x, targetScaleValue, smoothSpeed * Time.deltaTime);
            targetTransform.localScale = new Vector3(newScale, newScale, newScale);
        }

        public void ResetScale()
        {
            targetScaleValue = defaultScale;
            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * defaultScale;
            }
        }

        public void ApplyScaleImmediate(float scale)
        {
            targetScaleValue = Mathf.Clamp(scale, minScale, maxScale);
            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * targetScaleValue;
            }
        }
    }
}
