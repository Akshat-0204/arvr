using System.IO;
using NUnit.Framework;
using UnityEngine;
using VoxelAR.Save;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class VoxelSerializerTests
    {
        private static readonly Vector3Int Size = new Vector3Int(16, 16, 16);

        private static VoxelGrid ThreeBlocks()
        {
            var g = new VoxelGrid(Size);
            g.Set(new Vector3Int(0, 0, 0), 1);
            g.Set(new Vector3Int(5, 3, 2), 4);
            g.Set(new Vector3Int(15, 15, 15), 8);
            return g;
        }

        private static string Json(int version, int sx, int[] cells) =>
            JsonUtility.ToJson(new SaveData { version = version, sx = sx, sy = 16, sz = 16, cells = cells });

        [Test]
        public void RoundTrip_ThreeBlocks()
        {
            var src = ThreeBlocks();
            var dst = new VoxelGrid(Size);
            Assert.IsTrue(VoxelSerializer.TryFromJson(VoxelSerializer.ToJson(src), dst, out var err), err);
            Assert.AreEqual(3, dst.Count);
            foreach (var (c, id) in src.All())
                Assert.AreEqual(id, dst.Get(c));
        }

        [Test]
        public void RoundTrip_EmptyGrid()
        {
            var dst = ThreeBlocks();
            Assert.IsTrue(VoxelSerializer.TryFromJson(VoxelSerializer.ToJson(new VoxelGrid(Size)), dst, out var err), err);
            Assert.AreEqual(0, dst.Count);
        }

        [TestCase("not json at all {{{")]
        [TestCase("")]
        [TestCase(null)]
        public void Garbage_ReturnsFalse_GridUnchanged(string json)
        {
            var dst = ThreeBlocks();
            Assert.IsFalse(VoxelSerializer.TryFromJson(json, dst, out var err));
            Assert.IsNotEmpty(err);
            Assert.AreEqual(3, dst.Count);
        }

        [Test]
        public void WrongVersion_Rejected() => AssertRejected(Json(2, 16, new[] { 0, 0, 0, 1 }));

        [Test]
        public void SizeMismatch_Rejected() => AssertRejected(Json(1, 8, new[] { 0, 0, 0, 1 }));

        [Test]
        public void OutOfBoundsCell_Rejected() => AssertRejected(Json(1, 16, new[] { 0, 0, 0, 1, 99, 0, 0, 1 }));

        [Test]
        public void UnknownId_Rejected() => AssertRejected(Json(1, 16, new[] { 0, 0, 0, 200 }));

        [Test]
        public void ZeroId_Rejected() => AssertRejected(Json(1, 16, new[] { 0, 0, 0, 0 }));

        [Test]
        public void LengthNotMultipleOfFour_Rejected() => AssertRejected(Json(1, 16, new[] { 0, 0, 0 }));

        private static void AssertRejected(string json)
        {
            var dst = ThreeBlocks();
            Assert.IsFalse(VoxelSerializer.TryFromJson(json, dst, out var err));
            Assert.IsNotEmpty(err);
            Assert.AreEqual(3, dst.Count, "target must be untouched");
        }
    }

    public class SaveSystemTests
    {
        private string dir;
        private SaveSystem saves;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "VoxelARTests_" + System.Guid.NewGuid().ToString("N"));
            saves = new SaveSystem(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void SaveLoad_RoundTrip_Overwrite()
        {
            var g = new VoxelGrid(new Vector3Int(16, 16, 16));
            g.Set(new Vector3Int(1, 1, 1), 2);
            Assert.IsTrue(saves.Save(1, g));
            g.Set(new Vector3Int(2, 2, 2), 3);
            Assert.IsTrue(saves.Save(1, g), "overwrite existing");
            Assert.IsFalse(File.Exists(saves.PathFor(1) + ".tmp"));

            var dst = new VoxelGrid(new Vector3Int(16, 16, 16));
            Assert.IsTrue(saves.Load(1, dst, out var err), err);
            Assert.AreEqual(2, dst.Count);
        }

        [Test]
        public void LoadMissingSlot_ReturnsFalse()
        {
            Assert.IsFalse(saves.Exists(0));
            Assert.IsFalse(saves.Load(0, new VoxelGrid(new Vector3Int(16, 16, 16)), out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void CorruptFile_ReturnsFalse()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(saves.PathFor(2), "\u0000garbage");
            Assert.IsFalse(saves.Load(2, new VoxelGrid(new Vector3Int(16, 16, 16)), out _));
        }

        [Test]
        public void InvalidSlot_ReturnsFalse()
        {
            var g = new VoxelGrid(new Vector3Int(16, 16, 16));
            Assert.IsFalse(saves.Save(3, g));
            Assert.IsFalse(saves.Save(-1, g));
        }
    }
}
