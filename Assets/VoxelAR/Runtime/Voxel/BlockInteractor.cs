using System;
using UnityEngine;
using VoxelAR.AR;
using VoxelAR.Interaction;

namespace VoxelAR.Voxel
{
    /// <summary>
    /// Tap → physics raycast against blocks/base plate → grid cell (in WorldRoot local space, so it
    /// stays correct under any pinch scale / twist) → <see cref="EditHistory.Apply"/>.
    /// </summary>
    public class BlockInteractor : MonoBehaviour
    {
        private const float MaxRayDistance = 20f;

        private Camera arCamera;
        private WorldPlacement placement;
        private EditHistory history;

        private ToolMode mode = ToolMode.Build;
        private byte selectedBlock = 1;

        public event Action<ToolMode> ModeChanged;
        public event Action<byte> SelectedBlockChanged;

        public ToolMode Mode
        {
            get => mode;
            set { if (mode == value) return; mode = value; ModeChanged?.Invoke(mode); }
        }

        public byte SelectedBlock
        {
            get => selectedBlock;
            set
            {
                if (!BlockPalette.IsValid(value) || selectedBlock == value) return;
                selectedBlock = value;
                SelectedBlockChanged?.Invoke(value);
            }
        }

        public void Init(Camera cam, WorldPlacement worldPlacement, EditHistory editHistory)
        {
            arCamera = cam;
            placement = worldPlacement;
            history = editHistory;
        }

        private void Update()
        {
            if (placement == null || !placement.IsPlaced || arCamera == null) return;
            if (TapInput.Instance == null || !TapInput.Instance.TryConsumeTap(out Vector2 screen)) return;
            TryEditAt(screen);
        }

        public bool TryEditAt(Vector2 screenPos)
        {
            var view = placement.View;
            if (view == null) return false;

            Ray ray = arCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, MaxRayDistance, VoxelWorldView.VoxelMask, QueryTriggerInteraction.Ignore))
                return false;
            if (!hit.collider.transform.IsChildOf(view.transform)) return false;

            bool build = mode == ToolMode.Build;
            if (!build && view.IsBasePlate(hit.collider)) return false;

            Transform root = view.transform;
            Vector3 localPoint = root.InverseTransformPoint(hit.point);
            Vector3 localNormal = SnapToAxis(root.InverseTransformDirection(hit.normal));
            Vector3Int cell = GridMath.TargetCell(localPoint, localNormal, view.BlockSize, build, view.Grid.Size);

            return history.Apply(cell, build ? selectedBlock : (byte)0);
        }

        private static Vector3 SnapToAxis(Vector3 n)
        {
            Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(n.x), 0, 0);
            if (a.y >= a.z) return new Vector3(0, Mathf.Sign(n.y), 0);
            return new Vector3(0, 0, Mathf.Sign(n.z));
        }
    }
}
