using System;
using UnityEngine;

namespace VoxelAR.Voxel
{
    /// <summary>Block materials. Id 0 = empty; ids 1..Count map to palette entries.</summary>
    public static class BlockPalette
    {
        private static readonly string[] Names =
            { "Stone", "Dirt", "Grass", "Sand", "Wood", "Brick", "Water", "White" };

        private static readonly Color32[] Colors =
        {
            new Color32(0x8A, 0x8A, 0x8A, 0xFF),
            new Color32(0x7A, 0x52, 0x30, 0xFF),
            new Color32(0x5D, 0xA1, 0x30, 0xFF),
            new Color32(0xE3, 0xD5, 0x9B, 0xFF),
            new Color32(0xA0, 0x70, 0x3C, 0xFF),
            new Color32(0xB0, 0x41, 0x3E, 0xFF),
            new Color32(0x3A, 0x7B, 0xD5, 0xFF),
            new Color32(0xF2, 0xF2, 0xF2, 0xFF),
        };

        public static readonly Color BasePlateColor = new Color32(0x3C, 0x3C, 0x3C, 0xFF);

        public static int Count => Names.Length;

        public static bool IsValid(byte id) => id >= 1 && id <= Count;

        public static string Name(byte id) => Names[Index(id)];

        public static Color Color(byte id) => Colors[Index(id)];

        private static int Index(byte id)
        {
            if (!IsValid(id))
                throw new ArgumentOutOfRangeException(nameof(id), id, $"Block id must be 1..{Count}.");
            return id - 1;
        }
    }
}
