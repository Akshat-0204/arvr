using System.Collections.Generic;
using UnityEngine;

namespace MerchAR.Product
{
    public enum ApparelType
    {
        TShirt,
        Hoodie
    }

    /// <summary>
    /// Procedural Unity Mesh Generator for apparel.
    /// Allows immediate testing and standalone fallback generation within the Unity Editor
    /// without relying on any external 3D software or model import pipelines.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralApparelGenerator : MonoBehaviour
    {
        [Header("Apparel Configuration")]
        [SerializeField] private ApparelType apparelType = ApparelType.TShirt;
        [SerializeField] private bool generateOnStart = false;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public ApparelType CurrentType => apparelType;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (generateOnStart && meshFilter.sharedMesh == null)
            {
                GenerateMesh(apparelType);
            }
        }

        [ContextMenu("Generate T-Shirt")]
        public void GenerateTShirt()
        {
            apparelType = ApparelType.TShirt;
            GenerateMesh(ApparelType.TShirt);
        }

        [ContextMenu("Generate Hoodie")]
        public void GenerateHoodie()
        {
            apparelType = ApparelType.Hoodie;
            GenerateMesh(ApparelType.Hoodie);
        }

        public void GenerateMesh(ApparelType type)
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            Mesh mesh = new Mesh
            {
                name = type == ApparelType.TShirt ? "Procedural_TShirt" : "Procedural_Hoodie"
            };

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            if (type == ApparelType.TShirt)
            {
                BuildTShirt(vertices, normals, uvs, triangles);
            }
            else
            {
                BuildHoodie(vertices, normals, uvs, triangles);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            meshFilter.sharedMesh = mesh;
            Debug.Log($"[ProceduralApparelGenerator] Generated {type} with {vertices.Count} verts and {triangles.Count / 3} triangles.");
        }

        private void BuildTShirt(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            // Build Torso Cylinder
            int rings = 9;
            int segments = 20;
            float yMin = -0.38f;
            float yMax = 0.35f;

            int[,] grid = new int[rings, segments];

            for (int r = 0; r < rings; r++)
            {
                float t = (float)r / (rings - 1);
                float y = Mathf.Lerp(yMin, yMax, t);

                float rx = (t < 0.3f) ? 0.22f : ((t < 0.7f) ? 0.24f : 0.22f - (t - 0.7f) * 0.15f);
                float rz = (t < 0.3f) ? 0.12f : ((t < 0.7f) ? 0.13f : 0.11f - (t - 0.7f) * 0.08f);

                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * rx;
                    float z = Mathf.Sin(angle) * rz;

                    // Neck cut in front
                    float yOffset = 0f;
                    if (t > 0.8f && Mathf.Cos(angle) > 0.3f)
                    {
                        yOffset = -0.04f * (Mathf.Cos(angle) - 0.3f);
                    }

                    grid[r, s] = verts.Count;
                    verts.Add(new Vector3(x, y + yOffset, z));
                    norms.Add(new Vector3(x, 0f, z).normalized);
                    uvs.Add(new Vector2((float)s / segments, t));
                }
            }

            // Quads to triangles for torso
            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int nextS = (s + 1) % segments;
                    int v0 = grid[r, s];
                    int v1 = grid[r, nextS];
                    int v2 = grid[r + 1, nextS];
                    int v3 = grid[r + 1, s];

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            // Short Sleeves
            AddSleeve(verts, norms, uvs, tris, 1f, 0.18f, 0.075f, 0.065f, 0.22f);
            AddSleeve(verts, norms, uvs, tris, -1f, 0.18f, 0.075f, 0.065f, 0.22f);
        }

        private void BuildHoodie(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            // Build Bulkier Torso
            int rings = 10;
            int segments = 20;
            float yMin = -0.42f;
            float yMax = 0.36f;

            int[,] grid = new int[rings, segments];

            for (int r = 0; r < rings; r++)
            {
                float t = (float)r / (rings - 1);
                float y = Mathf.Lerp(yMin, yMax, t);

                float rx = (t < 0.2f) ? 0.24f : ((t < 0.7f) ? 0.27f : 0.24f - (t - 0.7f) * 0.16f);
                float rz = (t < 0.2f) ? 0.14f : ((t < 0.7f) ? 0.16f : 0.13f - (t - 0.7f) * 0.09f);

                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * rx;
                    float z = Mathf.Sin(angle) * rz;

                    grid[r, s] = verts.Count;
                    verts.Add(new Vector3(x, y, z));
                    norms.Add(new Vector3(x, 0f, z).normalized);
                    uvs.Add(new Vector2((float)s / segments, t));
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int nextS = (s + 1) % segments;
                    int v0 = grid[r, s];
                    int v1 = grid[r, nextS];
                    int v2 = grid[r + 1, nextS];
                    int v3 = grid[r + 1, s];

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }

            // Long Sleeves
            AddSleeve(verts, norms, uvs, tris, 1f, 0.42f, 0.088f, 0.06f, 0.24f);
            AddSleeve(verts, norms, uvs, tris, -1f, 0.42f, 0.088f, 0.06f, 0.24f);

            // Kangaroo Front Pocket
            AddFrontPocket(verts, norms, uvs, tris);

            // 3D Sculpted Hood behind neck
            AddHood(verts, norms, uvs, tris);
        }

        private void AddSleeve(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
            float side, float length, float rBase, float rEnd, float shoulderY)
        {
            int rings = 6;
            int segments = 12;
            int[,] sGrid = new int[rings, segments];
            Vector3 origin = new Vector3(side * 0.25f, shoulderY, 0f);

            for (int r = 0; r < rings; r++)
            {
                float t = (float)r / (rings - 1);
                Vector3 center = origin + new Vector3(side * (t * length * 0.8f), -t * length * 0.55f, t * length * 0.1f);
                float radius = Mathf.Lerp(rBase, rEnd, t);

                for (int s = 0; s < segments; s++)
                {
                    float angle = (float)s / segments * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(Mathf.Sin(angle) * radius * 0.5f, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.86f);

                    sGrid[r, s] = verts.Count;
                    verts.Add(center + offset);
                    norms.Add(offset.normalized);
                    uvs.Add(new Vector2((float)s / segments, t));
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int nextS = (s + 1) % segments;
                    int v0 = sGrid[r, s];
                    int v1 = sGrid[r, nextS];
                    int v2 = sGrid[r + 1, nextS];
                    int v3 = sGrid[r + 1, s];

                    if (side > 0)
                    {
                        tris.Add(v0);
                        tris.Add(v2);
                        tris.Add(v1);

                        tris.Add(v0);
                        tris.Add(v3);
                        tris.Add(v2);
                    }
                    else
                    {
                        tris.Add(v0);
                        tris.Add(v1);
                        tris.Add(v2);

                        tris.Add(v0);
                        tris.Add(v2);
                        tris.Add(v3);
                    }
                }
            }
        }

        private void AddFrontPocket(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            int start = verts.Count;
            // 4 points on the front (+Z or +X depending on forward face, here +Z)
            verts.Add(new Vector3(0.12f, -0.28f, 0.165f));
            verts.Add(new Vector3(-0.12f, -0.28f, 0.165f));
            verts.Add(new Vector3(-0.09f, -0.09f, 0.17f));
            verts.Add(new Vector3(0.09f, -0.09f, 0.17f));

            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);
            norms.Add(Vector3.forward);

            uvs.Add(new Vector2(0.2f, 0.2f));
            uvs.Add(new Vector2(0.8f, 0.2f));
            uvs.Add(new Vector2(0.7f, 0.6f));
            uvs.Add(new Vector2(0.3f, 0.6f));

            tris.Add(start);
            tris.Add(start + 2);
            tris.Add(start + 1);

            tris.Add(start);
            tris.Add(start + 3);
            tris.Add(start + 2);
        }

        private void AddHood(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
        {
            int rings = 6;
            int segments = 10;
            int[,] hGrid = new int[rings, segments];

            for (int r = 0; r < rings; r++)
            {
                float t = (float)r / (rings - 1);
                float hy = 0.32f + Mathf.Sin(t * Mathf.PI * 0.85f) * 0.20f;
                float hz = -0.06f - Mathf.Cos(t * Mathf.PI * 0.85f) * 0.15f;
                float hr = 0.14f * Mathf.Sin(t * Mathf.PI * 0.9f + 0.15f) + 0.04f;

                for (int s = 0; s < segments; s++)
                {
                    float frac = (float)s / (segments - 1);
                    float angle = (frac - 0.5f) * Mathf.PI * 1.1f;
                    float hx = Mathf.Sin(angle) * hr * 1.25f;
                    float hyAdj = hy + Mathf.Cos(angle) * hr * 0.5f;

                    hGrid[r, s] = verts.Count;
                    verts.Add(new Vector3(hx, hyAdj, hz));
                    norms.Add(new Vector3(hx, hyAdj - hy, hz - (-0.06f)).normalized);
                    uvs.Add(new Vector2(frac, t));
                }
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments - 1; s++)
                {
                    int v0 = hGrid[r, s];
                    int v1 = hGrid[r, s + 1];
                    int v2 = hGrid[r + 1, s + 1];
                    int v3 = hGrid[r + 1, s];

                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    tris.Add(v0);
                    tris.Add(v3);
                    tris.Add(v2);
                }
            }
        }
    }
}
