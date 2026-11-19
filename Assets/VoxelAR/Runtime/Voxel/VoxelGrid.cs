using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelAR.Voxel
{
    /// <summary>
    /// Dense 3D grid of block ids. 0 = empty. Pure data, no Unity scene dependencies.
    /// </summary>
    public class VoxelGrid
    {
        private readonly byte[] cells;
        private int count;

        public Vector3Int Size { get; }
        public int Count => count;

        public event Action<Vector3Int, byte> CellChanged;
        public event Action Cleared;

        public VoxelGrid(Vector3Int size)
        {
            if (size.x <= 0 || size.y <= 0 || size.z <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Grid dimensions must be positive.");
            Size = size;
            cells = new byte[size.x * size.y * size.z];
        }

        public bool InBounds(Vector3Int c) =>
            c.x >= 0 && c.y >= 0 && c.z >= 0 && c.x < Size.x && c.y < Size.y && c.z < Size.z;

        public byte Get(Vector3Int c) => InBounds(c) ? cells[Index(c)] : (byte)0;

        /// <summary>Sets a cell. Returns false (and fires no event) when out of bounds or unchanged.</summary>
        public bool Set(Vector3Int c, byte id)
        {
            if (!InBounds(c)) return false;
            int i = Index(c);
            byte old = cells[i];
            if (old == id) return false;

            cells[i] = id;
            if (old == 0) count++;
            else if (id == 0) count--;

            CellChanged?.Invoke(c, id);
            return true;
        }

        public IEnumerable<(Vector3Int cell, byte id)> All()
        {
            for (int x = 0; x < Size.x; x++)
            for (int y = 0; y < Size.y; y++)
            for (int z = 0; z < Size.z; z++)
            {
                byte id = cells[(x * Size.y + y) * Size.z + z];
                if (id != 0) yield return (new Vector3Int(x, y, z), id);
            }
        }

        public void Clear()
        {
            Array.Clear(cells, 0, cells.Length);
            count = 0;
            Cleared?.Invoke();
        }

        private int Index(Vector3Int c) => (c.x * Size.y + c.y) * Size.z + c.z;
    }
}
