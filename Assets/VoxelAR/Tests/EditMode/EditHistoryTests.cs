using NUnit.Framework;
using UnityEngine;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class EditHistoryTests
    {
        private VoxelGrid grid;
        private EditHistory history;

        [SetUp]
        public void SetUp()
        {
            grid = new VoxelGrid(new Vector3Int(16, 16, 16));
            history = new EditHistory(grid);
        }

        [Test]
        public void ApplyThenUndo_RestoresEmpty()
        {
            var c = new Vector3Int(1, 2, 3);
            Assert.IsTrue(history.Apply(c, 4));
            Assert.IsTrue(history.CanUndo);
            Assert.IsTrue(history.Undo());
            Assert.AreEqual(0, grid.Get(c));
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void Undo_RestoresPreviousBlock_NotEmpty()
        {
            var c = new Vector3Int(0, 0, 0);
            history.Apply(c, 1);
            history.Apply(c, 5);
            history.Undo();
            Assert.AreEqual(1, grid.Get(c));
        }

        [Test]
        public void UndoOnEmpty_ReturnsFalse() => Assert.IsFalse(history.Undo());

        [Test]
        public void FiftyOneApplies_OnlyFiftyUndoable()
        {
            for (int i = 0; i < 51; i++)
                history.Apply(new Vector3Int(i % 16, i / 16, 0), 1);

            int undone = 0;
            while (history.Undo()) undone++;
            Assert.AreEqual(50, undone);
            Assert.AreEqual(1, grid.Count, "oldest edit stays applied");
            Assert.AreEqual(1, grid.Get(new Vector3Int(0, 0, 0)));
        }

        [Test]
        public void NoOpApply_NotRecorded()
        {
            Assert.IsFalse(history.Apply(new Vector3Int(0, 0, 0), 0), "same value");
            Assert.IsFalse(history.Apply(new Vector3Int(99, 0, 0), 1), "out of bounds");
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void ClearThenUndo_RestoresAllBlocks()
        {
            history.Apply(new Vector3Int(1, 0, 1), 1);
            history.Apply(new Vector3Int(2, 0, 1), 2);
            history.Apply(new Vector3Int(2, 1, 1), 3);

            Assert.IsTrue(history.Clear());
            Assert.AreEqual(0, grid.Count);

            Assert.IsTrue(history.Undo());
            Assert.AreEqual(3, grid.Count);
            Assert.AreEqual(3, grid.Get(new Vector3Int(2, 1, 1)));
        }

        [Test]
        public void ClearOnEmptyGrid_NotRecorded()
        {
            Assert.IsFalse(history.Clear());
            Assert.IsFalse(history.CanUndo);
        }

        [Test]
        public void Changed_FiresOnApplyAndUndo()
        {
            int fired = 0;
            history.Changed += () => fired++;
            history.Apply(new Vector3Int(0, 0, 0), 1);
            history.Undo();
            Assert.AreEqual(2, fired);
        }
    }
}
