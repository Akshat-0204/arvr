using NUnit.Framework;
using UnityEngine;
using VoxelAR.AR;
using VoxelAR.Core;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class VoxelWorldViewTests
    {
        private const float Bs = 0.05f;
        private VoxelGrid grid;
        private EditHistory history;
        private GameObject root;
        private VoxelWorldView view;

        [SetUp]
        public void SetUp()
        {
            grid = new VoxelGrid(new Vector3Int(16, 16, 16));
            history = new EditHistory(grid);
            root = new GameObject("TestRoot");
            view = root.AddComponent<VoxelWorldView>();
            view.Bind(grid, Bs);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void SetCell_CreatesCubeAtCellCentre()
        {
            var c = new Vector3Int(8, 0, 8);
            grid.Set(c, 3);
            Assert.AreEqual(1, view.ActiveCubeCount);
            var cube = root.transform.Find("Blocks").GetChild(0);
            Assert.That(Vector3.Distance(GridMath.CellToLocal(c, Bs, grid.Size), cube.localPosition), Is.LessThan(1e-5f));
            Assert.That(cube.localScale.x, Is.EqualTo(Bs).Within(1e-6f));
        }

        [Test]
        public void RemoveAndUndo_TracksGrid()
        {
            history.Apply(new Vector3Int(1, 0, 1), 1);
            history.Apply(new Vector3Int(2, 0, 1), 2);
            Assert.AreEqual(2, view.ActiveCubeCount);
            history.Apply(new Vector3Int(1, 0, 1), 0);
            Assert.AreEqual(1, view.ActiveCubeCount);
            history.Undo();
            Assert.AreEqual(2, view.ActiveCubeCount);
        }

        [Test]
        public void Clear_RemovesAllCubes_AndRecyclesFromPool()
        {
            for (int x = 0; x < 5; x++) grid.Set(new Vector3Int(x, 0, 0), 1);
            grid.Clear();
            Assert.AreEqual(0, view.ActiveCubeCount);
            grid.Set(new Vector3Int(0, 0, 0), 1);
            Assert.AreEqual(5, root.transform.Find("Blocks").childCount, "pooled cubes reused, none created");
        }

        [Test]
        public void Bind_ShowsExistingCells()
        {
            var g2 = new VoxelGrid(new Vector3Int(16, 16, 16));
            g2.Set(new Vector3Int(0, 0, 0), 1);
            g2.Set(new Vector3Int(0, 1, 0), 1);
            view.Bind(g2, Bs);
            Assert.AreEqual(2, view.ActiveCubeCount);
            grid.Set(new Vector3Int(5, 5, 5), 1); // old grid no longer bound
            Assert.AreEqual(2, view.ActiveCubeCount);
        }

        [Test]
        public void BasePlate_TopAtZero_CoversGrid()
        {
            var b = view.BasePlate.transform;
            Assert.That(b.localPosition.y + b.localScale.y * 0.5f, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(b.localScale.x, Is.EqualTo(16 * Bs).Within(1e-6f));
            Assert.IsTrue(view.IsBasePlate(view.BasePlate));
        }

        [Test]
        public void Interactor_TapOnPlate_BuildsThenBreaks()
        {
            var appGo = new GameObject("App");
            var camGo = new GameObject("Cam");
            try
            {
                var cam = camGo.AddComponent<Camera>();
                cam.transform.SetPositionAndRotation(new Vector3(0.0125f, 1f, 0.0125f), Quaternion.Euler(90f, 0f, 0f));
                var placement = appGo.AddComponent<WorldPlacement>();
                placement.Init(null, null, cam, null, grid, Bs);
                placement.Place(new Pose(Vector3.zero, Quaternion.identity)); // no anchor manager → synchronous
                Assert.IsTrue(placement.IsPlaced);

                var interactor = appGo.AddComponent<BlockInteractor>();
                interactor.Init(cam, placement, new EditHistory(grid));
                Physics.SyncTransforms();

                // camera looks straight down at the plate centre → cell (8,0,8)
                Vector2 centre = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0f));
                Assert.IsTrue(interactor.TryEditAt(centre), "build on plate");
                Assert.AreEqual(1, grid.Get(new Vector3Int(8, 0, 8)));

                Physics.SyncTransforms();
                Assert.IsTrue(interactor.TryEditAt(centre), "stack on block");
                Assert.AreEqual(1, grid.Get(new Vector3Int(8, 1, 8)));

                interactor.Mode = ToolMode.Break;
                Physics.SyncTransforms();
                Assert.IsTrue(interactor.TryEditAt(centre), "break top block");
                Assert.AreEqual(0, grid.Get(new Vector3Int(8, 1, 8)));
                Assert.AreEqual(1, grid.Get(new Vector3Int(8, 0, 8)));

                placement.ResetPlacement();
                Assert.IsFalse(placement.IsPlaced);
                Assert.AreEqual(1, grid.Count, "grid kept after re-place");
            }
            finally
            {
                Object.DestroyImmediate(appGo);
                Object.DestroyImmediate(camGo);
                foreach (var r in Object.FindObjectsByType<VoxelWorldView>(FindObjectsSortMode.None))
                    if (r != view) Object.DestroyImmediate(r.gameObject);
            }
        }

        [Test]
        public void ReticleMesh_IsFlatFrame()
        {
            var m = SceneBootstrap.BuildFrameMesh(0.4f, 0.01f);
            Assert.AreEqual(24, m.triangles.Length);
            Assert.That(m.bounds.size.x, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(m.bounds.size.y, Is.EqualTo(0f).Within(1e-5f));
            Object.DestroyImmediate(m);
        }
    }
}
