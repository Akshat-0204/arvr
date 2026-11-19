using System;
using System.Collections.Generic;
using UnityEngine;
using VoxelAR.Voxel;

namespace VoxelAR.Save
{
    [Serializable]
    public class SaveData
    {
        public int version = VoxelSerializer.FormatVersion;
        public int sx, sy, sz;
        /// <summary>Flat [x,y,z,id, x,y,z,id, ...].</summary>
        public int[] cells = Array.Empty<int>();
    }

    /// <summary>VoxelGrid ⇄ JSON. Never throws on bad input; rejected loads leave the target untouched.</summary>
    public static class VoxelSerializer
    {
        public const int FormatVersion = 1;

        public static string ToJson(VoxelGrid g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            var cells = new List<int>(g.Count * 4);
            foreach (var (c, id) in g.All())
            {
                cells.Add(c.x);
                cells.Add(c.y);
                cells.Add(c.z);
                cells.Add(id);
            }
            var data = new SaveData { sx = g.Size.x, sy = g.Size.y, sz = g.Size.z, cells = cells.ToArray() };
            return JsonUtility.ToJson(data);
        }

        public static bool TryFromJson(string json, VoxelGrid target, out string error)
        {
            error = null;
            if (target == null) { error = "No target grid."; return false; }
            if (string.IsNullOrWhiteSpace(json)) { error = "Save is empty."; return false; }

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e) { error = "Save is corrupt: " + e.Message; return false; }

            if (data == null) { error = "Save is corrupt."; return false; }
            if (data.version != FormatVersion) { error = $"Unsupported save version {data.version}."; return false; }
            if (data.sx != target.Size.x || data.sy != target.Size.y || data.sz != target.Size.z)
            { error = $"Save grid size {data.sx}x{data.sy}x{data.sz} doesn't match."; return false; }

            var cells = data.cells ?? Array.Empty<int>();
            if (cells.Length % 4 != 0) { error = "Save cell data is malformed."; return false; }

            for (int i = 0; i < cells.Length; i += 4)
            {
                var c = new Vector3Int(cells[i], cells[i + 1], cells[i + 2]);
                int id = cells[i + 3];
                if (!target.InBounds(c)) { error = $"Cell {c} out of bounds."; return false; }
                if (id < 1 || id > BlockPalette.Count) { error = $"Unknown block id {id}."; return false; }
            }

            target.Clear();
            for (int i = 0; i < cells.Length; i += 4)
                target.Set(new Vector3Int(cells[i], cells[i + 1], cells[i + 2]), (byte)cells[i + 3]);
            return true;
        }
    }
}
