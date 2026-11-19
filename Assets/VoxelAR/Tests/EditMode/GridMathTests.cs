using NUnit.Framework;
using UnityEngine;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class GridMathTests
    {
        private const float Bs = 0.05f;

        [Test]
        public void CellToLocal_LocalToCell_RoundTrip()
        {
            var points = new[] { new Vector3(0.01f, 0.02f, -0.03f), new Vector3(-0.39f, 0.79f, 0.39f), new Vector3(0.123f, 0.456f, -0.2f) };
            foreach (var p in points)
            {
                Vector3 centre = GridMath.CellToLocal(GridMath.LocalToCell(p, Bs), Bs);
                Assert.LessOrEqual(Mathf.Abs(centre.x - p.x), Bs / 2 + 1e-6f);
                Assert.LessOrEqual(Mathf.Abs(centre.y - p.y), Bs / 2 + 1e-6f);
                Assert.LessOrEqual(Mathf.Abs(centre.z - p.z), Bs / 2 + 1e-6f);
            }
        }

        [Test]
        public void Origin_CornerCellsMapToGridEdges()
        {
            Assert.AreEqual(new Vector3Int(0, 0, 0), GridMath.LocalToCell(new Vector3(-0.399f, 0.001f, -0.399f), Bs));
            Assert.AreEqual(new Vector3Int(15, 15, 15), GridMath.LocalToCell(new Vector3(0.399f, 0.799f, 0.399f), Bs));
        }

        [Test]
        public void TopFaceHit_BuildAbove_BreakSelf()
        {
            var cell = new Vector3Int(3, 0, 3);
            Vector3 top = GridMath.CellToLocal(cell, Bs) + Vector3.up * (Bs / 2);
            Assert.AreEqual(new Vector3Int(3, 1, 3), GridMath.TargetCell(top, Vector3.up, Bs, build: true));
            Assert.AreEqual(new Vector3Int(3, 0, 3), GridMath.TargetCell(top, Vector3.up, Bs, build: false));
        }

        [Test]
        public void SideFaceHit_BuildNeighbour()
        {
            var cell = new Vector3Int(5, 2, 5);
            Vector3 side = GridMath.CellToLocal(cell, Bs) + Vector3.right * (Bs / 2);
            Assert.AreEqual(new Vector3Int(6, 2, 5), GridMath.TargetCell(side, Vector3.right, Bs, build: true));
            Assert.AreEqual(cell, GridMath.TargetCell(side, Vector3.right, Bs, build: false));
        }

        [Test]
        public void BasePlateHit_BuildsOnLayerZero()
        {
            Vector3 hit = GridMath.CellToLocal(new Vector3Int(7, 0, 9), Bs);
            hit.y = 0f;
            Assert.AreEqual(new Vector3Int(7, 0, 9), GridMath.TargetCell(hit, Vector3.up, Bs, build: true));
        }
    }
}
