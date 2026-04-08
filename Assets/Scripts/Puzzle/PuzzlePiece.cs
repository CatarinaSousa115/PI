using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PuzzlePiece : MonoBehaviour
    {
        [HideInInspector] public int gridX, gridY, totalCols, totalRows;
        [HideInInspector] public Vector3 correctLocalPosition;
        [HideInInspector] public bool isPlaced = false;
        [HideInInspector] public float pieceW;
        [HideInInspector] public float pieceH;

        // Bottom, Right, Top, Left
        [HideInInspector] public int[] edges = new int[4];

        private MeshRenderer mr;

        private static readonly Color normalColor = Color.white;
        private static readonly Color highlightColor = new Color(1f, 1f, 0.6f);
        private static readonly Color placedColor = new Color(0.8f, 1f, 0.8f);

        public void Init(int gx, int gy, int cols, int rows, Material mat, int[] edgeDirs, float w, float h)
        {
            gridX = gx; gridY = gy;
            totalCols = cols; totalRows = rows;
            edges = edgeDirs;
            pieceW = w;
            pieceH = h;

            mr = GetComponent<MeshRenderer>();
            mr.material = mat;

            Mesh mesh = BuildMesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        public void SetHighlight(bool on)
        {
            if (isPlaced) return;
            mr.material.color = on ? highlightColor : normalColor;
        }

        public void SetPlaced()
        {
            isPlaced = true;
            mr.material.color = placedColor;
        }

        // ─── Mesh Generation ───────────────────────────────────────────────

        List<Vector2> GenerateEdgePoints(Vector2 start, Vector2 end, Vector2 normal, int dir, int steps = 16)
        {
            var points = new List<Vector2>();
            float tabHeight = Mathf.Min(pieceW, pieceH) * 0.3f;

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float bump = 0f;

                if (t > 0.2f && t < 0.8f)
                {
                    float localT = (t - 0.2f) / 0.6f;
                    bump = Mathf.Sin(localT * Mathf.PI) * tabHeight * dir;
                }

                points.Add(Vector2.Lerp(start, end, t) + normal * bump);
            }

            return points;
        }

        Mesh BuildMesh()
        {
            float w = pieceW;
            float h = pieceH;

            Vector2 bl = new Vector2(0, 0);
            Vector2 br = new Vector2(w, 0);
            Vector2 tr = new Vector2(w, h);
            Vector2 tl = new Vector2(0, h);

            var outline = new List<Vector2>();
            outline.AddRange(GenerateEdgePoints(bl, br, Vector2.down, edges[0]));
            outline.AddRange(GenerateEdgePoints(br, tr, Vector2.right, edges[1]));
            outline.AddRange(GenerateEdgePoints(tr, tl, Vector2.up, edges[2]));
            outline.AddRange(GenerateEdgePoints(tl, bl, Vector2.left, edges[3]));

            Vector2 center = new Vector2(w / 2f, h / 2f);

            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            verts.Add(new Vector3(center.x, center.y, 0f));
            uvs.Add(new Vector2(
                (gridX + 0.5f) / totalCols,
                (gridY + 0.5f) / totalRows
            ));

            foreach (Vector2 p in outline)
            {
                verts.Add(new Vector3(p.x, p.y, 0f));
                uvs.Add(new Vector2(
                    (gridX + (p.x / w)) / totalCols,
                    (gridY + (p.y / h)) / totalRows
                ));
            }

            for (int i = 1; i < verts.Count - 1; i++)
            {
                tris.Add(0); tris.Add(i); tris.Add(i + 1);
            }
            tris.Add(0); tris.Add(verts.Count - 1); tris.Add(1);

            var mesh = new Mesh
            {
                name = $"Piece_{gridX}_{gridY}",
                vertices = verts.ToArray(),
                triangles = tris.ToArray(),
                uv = uvs.ToArray()
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}