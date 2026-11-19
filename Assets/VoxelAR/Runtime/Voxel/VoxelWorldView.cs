using System.Collections.Generic;
using UnityEngine;

namespace VoxelAR.Voxel
{
    /// <summary>
    /// Renders a <see cref="VoxelGrid"/> as one cube per filled cell under this transform (WorldRoot local space),
    /// plus a base plate whose top face is y = 0. Removed cubes are pooled.
    /// </summary>
    public class VoxelWorldView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private const string VoxelLayerName = "Voxel";

        private readonly Dictionary<Vector3Int, GameObject> cubes = new Dictionary<Vector3Int, GameObject>();
        private readonly Stack<GameObject> pool = new Stack<GameObject>();
        private MaterialPropertyBlock mpb;
        private Material blockMaterial;
        private Transform blockParent;
        private int voxelLayer;

        public VoxelGrid Grid { get; private set; }
        public float BlockSize { get; private set; }
        public Collider BasePlate { get; private set; }
        public int ActiveCubeCount => cubes.Count;

        /// <summary>Layer mask for raycasting blocks + base plate.</summary>
        public static int VoxelMask
        {
            get
            {
                int l = LayerMask.NameToLayer(VoxelLayerName);
                return l < 0 ? Physics.DefaultRaycastLayers : 1 << l;
            }
        }

        public void Bind(VoxelGrid grid, float blockSize)
        {
            Unbind();
            Grid = grid;
            BlockSize = blockSize;
            mpb ??= new MaterialPropertyBlock();
            voxelLayer = Mathf.Max(0, LayerMask.NameToLayer(VoxelLayerName));
            blockMaterial = Resources.Load<Material>("M_Block");
            if (blockMaterial == null) Debug.LogWarning("[VoxelWorldView] Resources/M_Block missing — run VoxelAR ▸ Setup Project.");

            if (blockParent == null)
            {
                blockParent = new GameObject("Blocks").transform;
                blockParent.SetParent(transform, false);
            }
            if (BasePlate == null) BasePlate = CreateBasePlate();

            grid.CellChanged += OnCellChanged;
            grid.Cleared += Rebuild;
            Rebuild();
        }

        public void Unbind()
        {
            if (Grid == null) return;
            Grid.CellChanged -= OnCellChanged;
            Grid.Cleared -= Rebuild;
            Grid = null;
        }

        private void OnDestroy() => Unbind();

        public bool IsBasePlate(Collider c) => c != null && c == BasePlate;

        private void Rebuild()
        {
            foreach (var go in cubes.Values) Release(go);
            cubes.Clear();
            foreach (var (cell, id) in Grid.All()) OnCellChanged(cell, id);
        }

        private void OnCellChanged(Vector3Int cell, byte id)
        {
            if (id == 0)
            {
                if (cubes.TryGetValue(cell, out var old))
                {
                    Release(old);
                    cubes.Remove(cell);
                }
                return;
            }

            if (!cubes.TryGetValue(cell, out var cube))
            {
                cube = Acquire();
                cube.name = $"Block {cell.x},{cell.y},{cell.z}";
                cube.transform.localPosition = GridMath.CellToLocal(cell, BlockSize, Grid.Size);
                cubes[cell] = cube;
            }
            ApplyColor(cube.GetComponent<Renderer>(), BlockPalette.Color(id));
        }

        private GameObject Acquire()
        {
            GameObject go;
            if (pool.Count > 0)
            {
                go = pool.Pop();
                go.SetActive(true);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.layer = voxelLayer;
                go.transform.SetParent(blockParent, false);
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one * BlockSize;
                var r = go.GetComponent<Renderer>();
                if (blockMaterial != null) r.sharedMaterial = blockMaterial;
            }
            return go;
        }

        private void Release(GameObject go)
        {
            go.SetActive(false);
            pool.Push(go);
        }

        private Collider CreateBasePlate()
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "BasePlate";
            plate.layer = voxelLayer;
            plate.transform.SetParent(transform, false);
            const float thickness = 0.005f;
            plate.transform.localScale = new Vector3(Grid.Size.x * BlockSize, thickness, Grid.Size.z * BlockSize);
            plate.transform.localPosition = new Vector3(0f, -thickness * 0.5f, 0f);
            var r = plate.GetComponent<Renderer>();
            if (blockMaterial != null) r.sharedMaterial = blockMaterial;
            ApplyColor(r, BlockPalette.BasePlateColor);
            return plate.GetComponent<Collider>();
        }

        private void ApplyColor(Renderer r, Color c)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }
}
