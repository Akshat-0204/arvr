using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using VoxelAR.Core;
using VoxelAR.UI;
using VoxelAR.Voxel;

namespace VoxelAR.Tests
{
    public class BootstrapSmokeTests
    {
        private GameObject bootstrapGo;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go != null && go.transform.parent == null && go.scene.IsValid()) Object.Destroy(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsAndWiresEverything()
        {
            bootstrapGo = new GameObject("Bootstrap");
            var b = bootstrapGo.AddComponent<SceneBootstrap>();
            yield return null;
            yield return null;

            Assert.IsNotNull(b.Session);
            Assert.IsNotNull(b.Origin);
            Assert.AreSame(b.ARCamera, b.Origin.Camera);
            Assert.IsNotNull(b.Origin.GetComponent<ARPlaneManager>().planePrefab, "plane visual template");
            Assert.IsNotNull(b.Origin.GetComponent<ARAnchorManager>());
            Assert.IsNotNull(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>());

            Assert.IsNotNull(UIManager.Instance);
            Assert.AreEqual(UIManager.MsgFindSurface, b.UI.CurrentInstruction);
            Assert.IsFalse(b.UI.transform.Find("SafeArea/BottomBar").gameObject.activeSelf, "bar hidden until placed");

            // Place without AR (no plane) and verify bar + instruction follow.
            b.Placement.Place(new Pose(new Vector3(0, -0.5f, 1f), Quaternion.identity));
            yield return null;
            Assert.IsTrue(b.Placement.IsPlaced);
            Assert.IsTrue(b.UI.transform.Find("SafeArea/BottomBar").gameObject.activeSelf);
            Assert.AreEqual(UIManager.MsgBuild, b.UI.CurrentInstruction);

            // Undo button tracks history.
            var undo = b.UI.transform.Find("SafeArea/BottomBar/Tools/Button Undo").GetComponent<Button>();
            Assert.IsFalse(undo.interactable);
            b.App.History.Apply(new Vector3Int(0, 0, 0), 2);
            Assert.IsTrue(undo.interactable);
            Assert.AreEqual(1, b.Placement.View.ActiveCubeCount);

            // Swatch selects block, mode button toggles break.
            b.UI.transform.Find("SafeArea/BottomBar/Palette/Swatch Brick").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(6, b.Interactor.SelectedBlock);
            b.UI.transform.Find("SafeArea/BottomBar/Tools/Button Build").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(ToolMode.Break, b.Interactor.Mode);
            Assert.AreEqual(UIManager.MsgBreak, b.UI.CurrentInstruction);

            // Re-place keeps grid.
            b.App.Replace();
            yield return null;
            Assert.IsFalse(b.Placement.IsPlaced);
            Assert.AreEqual(1, b.App.Grid.Count);
            Assert.IsFalse(b.UI.transform.Find("SafeArea/BottomBar").gameObject.activeSelf);
        }
    }
}
