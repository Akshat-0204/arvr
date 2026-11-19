using UnityEngine;
using VoxelAR.Interaction;

namespace VoxelAR.AR
{
    /// <summary>
    /// Two-finger pinch (uniform scale) and twist (yaw around the build area's up axis).
    /// One-finger input is left to tap-to-build. Editor: mouse wheel scales, Q/E rotates.
    /// </summary>
    public class ARObjectManipulator : MonoBehaviour
    {
        public const float MinScale = 0.25f;
        public const float MaxScale = 3f;

        [SerializeField] private float rotateKeySpeed = 90f;
        [SerializeField] private float wheelScaleStep = 0.1f;

        private bool gestureStartedOverUI;

        public float Scale => transform.localScale.x;

        private void Update()
        {
            if (UnityEngine.Input.touchCount == 2) HandlePinchTwist();
            else gestureStartedOverUI = false;

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleEditorControls();
#endif
        }

        private void HandlePinchTwist()
        {
            Touch t0 = UnityEngine.Input.GetTouch(0);
            Touch t1 = UnityEngine.Input.GetTouch(1);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                var tap = TapInput.Instance;
                gestureStartedOverUI = tap != null && (tap.IsOverUI(t0.position) || tap.IsOverUI(t1.position));
            }
            if (gestureStartedOverUI) return;

            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;
            float prevDist = (prev0 - prev1).magnitude;
            float curDist = (t0.position - t1.position).magnitude;
            if (prevDist > 1f) SetScale(Scale * (curDist / prevDist));

            float angle = Vector2.SignedAngle(prev1 - prev0, t1.position - t0.position);
            transform.Rotate(Vector3.up, -angle, Space.Self);
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        private void HandleEditorControls()
        {
            float wheel = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f) SetScale(Scale * (1f + wheel * wheelScaleStep));
            if (UnityEngine.Input.GetKey(KeyCode.Q)) transform.Rotate(Vector3.up, -rotateKeySpeed * Time.deltaTime, Space.Self);
            if (UnityEngine.Input.GetKey(KeyCode.E)) transform.Rotate(Vector3.up, rotateKeySpeed * Time.deltaTime, Space.Self);
        }
#endif

        public void SetScale(float s) => transform.localScale = Vector3.one * Mathf.Clamp(s, MinScale, MaxScale);
    }
}
