using System;
using NUnit.Framework;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class BlockPaletteTests
    {
        [Test]
        public void Count_IsEight() => Assert.AreEqual(8, BlockPalette.Count);

        [Test]
        public void EveryId_HasNameAndOpaqueColor()
        {
            for (byte id = 1; id <= BlockPalette.Count; id++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(BlockPalette.Name(id)), $"id {id}");
                Assert.AreEqual(1f, BlockPalette.Color(id).a, 1e-6f, $"id {id}");
            }
        }

        [Test]
        public void IdZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockPalette.Name(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockPalette.Color(0));
        }

        [Test]
        public void IdPastEnd_Throws() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => BlockPalette.Color((byte)(BlockPalette.Count + 1)));
    }
}
