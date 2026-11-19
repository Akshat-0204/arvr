using NUnit.Framework;
using UnityEngine;
using VoxelAR.Interaction;

namespace VoxelAR.Tests
{
    public class TapTrackerTests
    {
        private TapTracker tap;

        [SetUp]
        public void SetUp() => tap = new TapTracker();

        [Test]
        public void QuickStillTouch_IsTap()
        {
            Assert.IsFalse(tap.Process(1, TouchPhase.Began, new Vector2(100, 100), 0f, false));
            Assert.IsFalse(tap.Process(1, TouchPhase.Stationary, new Vector2(102, 101), 0.05f, false));
            Assert.IsTrue(tap.Process(1, TouchPhase.Ended, new Vector2(103, 101), 0.1f, false));
        }

        [Test]
        public void Drag_IsNotTap()
        {
            tap.Process(1, TouchPhase.Began, new Vector2(100, 100), 0f, false);
            tap.Process(1, TouchPhase.Moved, new Vector2(160, 100), 0.05f, false);
            Assert.IsFalse(tap.Process(1, TouchPhase.Ended, new Vector2(101, 100), 0.1f, false));
        }

        [Test]
        public void LongPress_IsNotTap()
        {
            tap.Process(1, TouchPhase.Began, Vector2.zero, 0f, false);
            Assert.IsFalse(tap.Process(1, TouchPhase.Ended, Vector2.zero, 1f, false));
        }

        [Test]
        public void StartedOverUI_IsNotTap()
        {
            tap.Process(1, TouchPhase.Began, Vector2.zero, 0f, true);
            Assert.IsFalse(tap.Process(1, TouchPhase.Ended, Vector2.zero, 0.1f, false));
        }

        [Test]
        public void SecondFingerDuringGesture_CancelsTap()
        {
            tap.Process(1, TouchPhase.Began, Vector2.zero, 0f, false);
            tap.Process(2, TouchPhase.Moved, Vector2.zero, 0.05f, false);
            Assert.IsFalse(tap.Process(1, TouchPhase.Ended, Vector2.zero, 0.1f, false));
        }

        [Test]
        public void EndedWithoutBegin_IsNotTap()
        {
            Assert.IsFalse(tap.Process(1, TouchPhase.Ended, Vector2.zero, 0.1f, false));
        }

        [Test]
        public void TapAfterCancelledGesture_StillWorks()
        {
            tap.Process(1, TouchPhase.Began, Vector2.zero, 0f, true);
            tap.Process(1, TouchPhase.Ended, Vector2.zero, 0.1f, false);
            tap.Process(1, TouchPhase.Began, Vector2.zero, 1f, false);
            Assert.IsTrue(tap.Process(1, TouchPhase.Ended, Vector2.zero, 1.1f, false));
        }
    }
}
