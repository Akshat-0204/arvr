using NUnit.Framework;
using UnityEngine;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class VoxelGridTests
    {
        private VoxelGrid grid;

        [SetUp]
        public void SetUp() => grid = new VoxelGrid(new Vector3Int(16, 16, 16));

        [Test]
        public void SetGet_RoundTrip()
        {
            var c = new Vector3Int(3, 4, 5);
            Assert.IsTrue(grid.Set(c, 2));
            Assert.AreEqual(2, grid.Get(c));
            Assert.AreEqual(1, grid.Count);
        }

        [TestCase(16, 0, 0)]
        [TestCase(-1, 0, 0)]
        [TestCase(0, 16, 0)]
        [TestCase(0, 0, -1)]
        public void Set_OutOfBounds_ReturnsFalse(int x, int y, int z)
        {
            Assert.IsFalse(grid.Set(new Vector3Int(x, y, z), 1));
            Assert.AreEqual(0, grid.Count);
        }

        [Test]
        public void Set_SameValue_NoEvent()
        {
            var c = new Vector3Int(1, 1, 1);
            grid.Set(c, 1);
            int events = 0;
            grid.CellChanged += (_, __) => events++;
            Assert.IsFalse(grid.Set(c, 1));
            Assert.AreEqual(0, events);
        }

        [Test]
        public void Set_FiresEvent_WithCellAndId()
        {
            Vector3Int got = default; byte gotId = 0;
            grid.CellChanged += (c, id) => { got = c; gotId = id; };
            grid.Set(new Vector3Int(2, 3, 4), 5);
            Assert.AreEqual(new Vector3Int(2, 3, 4), got);
            Assert.AreEqual(5, gotId);
        }

        [Test]
        public void SetZero_Removes_DecrementsCount()
        {
            var c = new Vector3Int(0, 0, 0);
            grid.Set(c, 1);
            grid.Set(c, 3); // replace keeps count
            Assert.AreEqual(1, grid.Count);
            Assert.IsTrue(grid.Set(c, 0));
            Assert.AreEqual(0, grid.Count);
        }

        [Test]
        public void Clear_FiresCleared_CountZero()
        {
            grid.Set(new Vector3Int(1, 0, 0), 1);
            grid.Set(new Vector3Int(2, 0, 0), 1);
            bool cleared = false;
            grid.Cleared += () => cleared = true;
            grid.Clear();
            Assert.IsTrue(cleared);
            Assert.AreEqual(0, grid.Count);
            CollectionAssert.IsEmpty(grid.All());
        }

        [Test]
        public void All_ReturnsOnlyFilled()
        {
            grid.Set(new Vector3Int(1, 2, 3), 4);
            grid.Set(new Vector3Int(15, 15, 15), 8);
            CollectionAssert.AreEquivalent(
                new[] { (new Vector3Int(1, 2, 3), (byte)4), (new Vector3Int(15, 15, 15), (byte)8) },
                grid.All());
        }
    }
}
