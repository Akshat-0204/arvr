using UnityEngine;

namespace VoxelAR.Interaction
{
    /// <summary>
    /// Pure tap recogniser. A tap = single finger, moved less than <see cref="MaxMovePixels"/>,
    /// shorter than <see cref="MaxDuration"/>, did not start over UI, and no second finger
    /// touched the screen at any point during the gesture.
    /// </summary>
    public class TapTracker
    {
        public float MaxMovePixels = 20f;
        public float MaxDuration = 0.35f;

        private bool tracking;
        private bool cancelled;
        private Vector2 start;
        private float startTime;

        public bool IsTracking => tracking;

        /// <summary>
        /// Feed one frame of input (primary touch, or mouse mapped to touch phases).
        /// Returns true on the frame a valid tap completes.
        /// </summary>
        public bool Process(int touchCount, TouchPhase phase, Vector2 position, float time, bool overUI)
        {
            if (touchCount == 0)
            {
                tracking = false;
                return false;
            }

            if (touchCount > 1)
            {
                // Pinch / twist: whatever single-finger gesture was in progress is no longer a tap.
                if (tracking) cancelled = true;
                return false;
            }

            switch (phase)
            {
                case TouchPhase.Began:
                    tracking = true;
                    cancelled = overUI;
                    start = position;
                    startTime = time;
                    return false;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (tracking && (position - start).magnitude >= MaxMovePixels) cancelled = true;
                    return false;

                case TouchPhase.Ended:
                    if (!tracking) return false;
                    tracking = false;
                    return !cancelled
                           && (position - start).magnitude < MaxMovePixels
                           && time - startTime < MaxDuration;

                default: // Canceled
                    tracking = false;
                    return false;
            }
        }

        public void Reset() => tracking = false;
    }
}
