using UnityEngine;
using UnityEngine.EventSystems;

namespace MerchAR.Product
{
    /// <summary>
    /// Handles touch and mouse rotation in the 3D viewer mode.
    /// Provides smooth inertia damping, velocity decay, and optional auto-turntable spin.
    /// </summary>
    public class ProductRotator : MonoBehaviour
    {
        [Header("Target & Speed")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float rotationSpeed = 0.25f;
        [SerializeField] private float damping = 5f;

        [Header("Auto-Turntable")]
        [SerializeField] private bool autoRotate = false;
        [SerializeField] private float autoRotateSpeed = 20f;

        private Vector2 lastPointerPos;
        private bool isDragging = false;
        private float angularVelocityY = 0f;
        private float angularVelocityX = 0f;

        public bool AutoRotate
        {
            get => autoRotate;
            set => autoRotate = value;
        }

        private void Start()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
        }

        private void Update()
        {
            HandleInput();
            ApplyRotation();
        }

        private void HandleInput()
        {
            // Touch input on mobile
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Ignore touch if interacting with UI
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    isDragging = false;
                    return;
                }

                if (touch.phase == TouchPhase.Began)
                {
                    isDragging = true;
                    lastPointerPos = touch.position;
                    angularVelocityY = 0f;
                    angularVelocityX = 0f;
                }
                else if (touch.phase == TouchPhase.Moved && isDragging)
                {
                    Vector2 delta = touch.position - lastPointerPos;
                    lastPointerPos = touch.position;

                    angularVelocityY = -delta.x * rotationSpeed;
                    angularVelocityX = delta.y * rotationSpeed * 0.5f;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isDragging = false;
                }
            }
            // Mouse input for Unity Editor or desktop testing
            else if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    {
                        isDragging = false;
                        return;
                    }

                    isDragging = true;
                    lastPointerPos = Input.mousePosition;
                    angularVelocityY = 0f;
                    angularVelocityX = 0f;
                }
                else if (Input.GetMouseButton(0) && isDragging)
                {
                    Vector2 currentPos = Input.mousePosition;
                    Vector2 delta = currentPos - lastPointerPos;
                    lastPointerPos = currentPos;

                    angularVelocityY = -delta.x * rotationSpeed;
                    angularVelocityX = delta.y * rotationSpeed * 0.5f;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    isDragging = false;
                }
            }
            else
            {
                // Multi-touch active (handled by pinch zoom)
                isDragging = false;
            }
        }

        private void ApplyRotation()
        {
            if (targetTransform == null) return;

            if (autoRotate && !isDragging && Mathf.Abs(angularVelocityY) < 0.1f)
            {
                targetTransform.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.World);
            }
            else
            {
                // Rotate around world Y (horizontal swipe)
                targetTransform.Rotate(Vector3.up, angularVelocityY, Space.World);

                // Optional subtle tilt on local X (clamped)
                targetTransform.Rotate(Vector3.right, angularVelocityX, Space.Self);

                // Inertia damping
                if (!isDragging)
                {
                    angularVelocityY = Mathf.Lerp(angularVelocityY, 0f, damping * Time.deltaTime);
                    angularVelocityX = Mathf.Lerp(angularVelocityX, 0f, damping * Time.deltaTime);
                }
            }
        }

        public void ResetRotation()
        {
            angularVelocityY = 0f;
            angularVelocityX = 0f;
            isDragging = false;
            if (targetTransform != null)
            {
                targetTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
