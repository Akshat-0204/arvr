using UnityEngine;

namespace VoxelAR.Voxel
{
    /// <summary>
    /// Converts between WorldRoot-local positions and grid cells.
    /// Cell (0,0,0) min corner sits at local (-size.x/2·bs, 0, -size.z/2·bs); y = 0 is the base-plate top.
    /// </summary>
    public static class GridMath
    {
        public static readonly Vector3Int DefaultSize = new Vector3Int(16, 16, 16);
        public const float DefaultBlockSize = 0.05f;

        public static Vector3 Origin(Vector3Int size, float blockSize) =>
            new Vector3(-size.x * 0.5f * blockSize, 0f, -size.z * 0.5f * blockSize);

        public static Vector3Int LocalToCell(Vector3 local, float blockSize) =>
            LocalToCell(local, blockSize, DefaultSize);

        public static Vector3Int LocalToCell(Vector3 local, float blockSize, Vector3Int size)
        {
            Vector3 p = (local - Origin(size, blockSize)) / blockSize;
            return new Vector3Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z));
        }

        /// <summary>Centre of the cell in WorldRoot-local space.</summary>
        public static Vector3 CellToLocal(Vector3Int c, float blockSize) =>
            CellToLocal(c, blockSize, DefaultSize);

        public static Vector3 CellToLocal(Vector3Int c, float blockSize, Vector3Int size) =>
            Origin(size, blockSize) + new Vector3(c.x + 0.5f, c.y + 0.5f, c.z + 0.5f) * blockSize;

        /// <summary>
        /// Cell targeted by a raycast hit. Build: cell in front of the hit face. Break: cell behind it.
        /// The hit point is nudged half a block along the normal to avoid float-on-face ambiguity.
        /// </summary>
        public static Vector3Int TargetCell(Vector3 localHitPoint, Vector3 localNormal, float blockSize, bool build) =>
            TargetCell(localHitPoint, localNormal, blockSize, build, DefaultSize);

        public static Vector3Int TargetCell(Vector3 localHitPoint, Vector3 localNormal, float blockSize, bool build, Vector3Int size)
        {
            Vector3 n = localNormal.sqrMagnitude > 0f ? localNormal.normalized : Vector3.up;
            Vector3 p = localHitPoint + n * (blockSize * 0.5f * (build ? 1f : -1f));
            return LocalToCell(p, blockSize, size);
        }
    }
}
