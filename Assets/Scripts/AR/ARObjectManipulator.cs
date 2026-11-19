using UnityEngine;
using UnityEngine.EventSystems;

namespace MerchAR.AR
{
    /// <summary>
    /// Attached to placed merchandise in AR.
    /// Provides intuitive single/two-finger rotation and pinch-to-scale manipulation.
    /// </summary>
    public class ARObjectManipulator : MonoBehaviour
    {
        [Header("Scale Constraints")]
        [SerializeField] private float minScale = 0.2f;
        [SerializeField] private float maxScale = 1.5f;

        [Header("Sensitivity")]
        [SerializeField] private float rotationSpeed = 0.4f;
        [SerializeField] private float pinchSensitivity = 0.003f;

        private float currentScale;
        private Vector2 lastTouchPos;

        private void Start()
        {
            currentScale = transform.localScale.x;
        }

        private void Update()
        {
            HandleTouches();
        }

        private void HandleTouches()
        {
            // Two-finger Pinch (Scale) and Twist (Rotation)
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                if (EventSystem.current != null)
                {
                    if (EventSystem.current.IsPointerOverGameObject(touch0.fingerId) ||
                        EventSystem.current.IsPointerOverGameObject(touch1.fingerId))
                    {
                        return;
                    }
                }

                // 1. Pinch to Scale
                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;

                float prevDistance = (prevPos0 - prevPos1).magnitude;
                float currentDistance = (touch0.position - touch1.position).magnitude;
                float deltaDistance = currentDistance - prevDistance;

                currentScale += deltaDistance * pinchSensitivity;
                currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
                transform.localScale = Vector3.one * currentScale;

                // 2. Twist to Rotate
                Vector2 prevDir = prevPos1 - prevPos0;
                Vector2 currentDir = touch1.position - touch0.position;
                float angle = Vector2.SignedAngle(prevDir, currentDir);

                transform.Rotate(Vector3.up, -angle, Space.World);
            }
            // Single-finger horizontal drag (Rotation around Y)
            else if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                if (touch.phase == TouchPhase.Moved)
                {
                    float deltaX = touch.deltaPosition.x;
                    transform.Rotate(Vector3.up, -deltaX * rotationSpeed, Space.World);
                }
            }
        }
    }
}
