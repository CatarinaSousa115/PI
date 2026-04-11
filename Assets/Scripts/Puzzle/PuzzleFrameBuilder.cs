using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    public class PuzzleFrameBuilder : MonoBehaviour
    {
        [Header("References")]
        public Material frameFrontMaterial;  
        public Material frameSideMaterial;   

        [Header("Board dimensions")]
        public int cols = 4;
        public int rows = 4;
        public float boardWidth = 1f;     
        public float boardAspect = 1.777f;

        [Header("Frame dimensions")]
        [Range(0.01f, 0.4f)] public float borderTop = 0.12f;
        [Range(0.01f, 0.4f)] public float borderBottom = 0.12f;
        [Range(0.01f, 0.4f)] public float borderLeft = 0.12f;
        [Range(0.01f, 0.4f)] public float borderRight = 0.12f;
        [Range(0.01f, 0.2f)] public float frameDepth = 0.04f;

        [Header("Position offset — nudge frame if not centered")]
        public float offsetX = 0f;
        public float offsetY = 0f;
        void Start()
        {
            BuildFrame();
        }

        public void BuildFrame()
        {
            float bw = boardWidth;
            float bh = boardWidth / boardAspect;

            float innerL = 0f;
            float innerR = bw;
            float innerB = 0f;
            float innerT = bh;

            float outerL = -borderLeft + offsetX;
            float outerR = bw + borderRight + offsetX;
            float outerB = -borderBottom + offsetY;
            float outerT = bh + borderTop + offsetY;

            innerL += offsetX;
            innerR += offsetX;
            innerB += offsetY;
            innerT += offsetY;

            float totalW = outerR - outerL;
            float totalH = outerT - outerB;


            Vector3 center = new Vector3(bw / 2f, bh / 2f, 0f);

            GameObject front = new GameObject("Frame_Front");
            front.transform.SetParent(transform, false);
            front.transform.localPosition = Vector3.zero;
            AddMeshRenderer(front, frameFrontMaterial);

            var fVerts = new List<Vector3>();
            var fUVs = new List<Vector2>();
            var fTris = new List<int>();

            fVerts.Add(new Vector3(outerL, outerB, 0f));
            fVerts.Add(new Vector3(outerR, outerB, 0f));
            fVerts.Add(new Vector3(outerR, outerT, 0f));
            fVerts.Add(new Vector3(outerL, outerT, 0f));


            fVerts.Add(new Vector3(innerL, innerB, 0f));
            fVerts.Add(new Vector3(innerR, innerB, 0f));
            fVerts.Add(new Vector3(innerR, innerT, 0f));
            fVerts.Add(new Vector3(innerL, innerT, 0f));


            foreach (Vector3 v in fVerts)
            {
                fUVs.Add(new Vector2(
                    (v.x - outerL) / totalW,
                    (v.y - outerB) / totalH
                ));
            }

            AddQuad(fTris, 0, 1, 5, 4);
            AddQuad(fTris, 1, 2, 6, 5);
            AddQuad(fTris, 2, 3, 7, 6);
            AddQuad(fTris, 3, 0, 4, 7);

            AssignMesh(front, fVerts, fUVs, fTris, "Frame_Front_Mesh");

            // ── Back face ───


            GameObject back = new GameObject("Frame_Back");
            back.transform.SetParent(transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, frameDepth);
            AddMeshRenderer(back, frameSideMaterial);

            var bkVerts = new List<Vector3>();
            var bkUVs = new List<Vector2>();
            var bkTris = new List<int>();

            bkVerts.Add(new Vector3(outerL, outerB, 0f));
            bkVerts.Add(new Vector3(outerR, outerB, 0f));
            bkVerts.Add(new Vector3(outerR, outerT, 0f));
            bkVerts.Add(new Vector3(outerL, outerT, 0f));
            bkVerts.Add(new Vector3(innerL, innerB, 0f));
            bkVerts.Add(new Vector3(innerR, innerB, 0f));
            bkVerts.Add(new Vector3(innerR, innerT, 0f));
            bkVerts.Add(new Vector3(innerL, innerT, 0f));

            foreach (Vector3 v in bkVerts)
                bkUVs.Add(new Vector2((v.x - outerL) / totalW, (v.y - outerB) / totalH));

            AddQuad(bkTris, 4, 5, 1, 0);
            AddQuad(bkTris, 5, 6, 2, 1);
            AddQuad(bkTris, 6, 7, 3, 2);
            AddQuad(bkTris, 7, 4, 0, 3);

            AssignMesh(back, bkVerts, bkUVs, bkTris, "Frame_Back_Mesh");

            BuildSides(outerL, outerR, outerB, outerT,
                       innerL, innerR, innerB, innerT,
                       totalW, totalH);
        }

        void BuildSides(float oL, float oR, float oB, float oT,
                        float iL, float iR, float iB, float iT,
                        float totalW, float totalH)
        {
            float d = frameDepth;

            BuildSideQuad("Side_OuterBottom",
                new Vector3(oL, oB, 0), new Vector3(oR, oB, 0),
                new Vector3(oR, oB, d), new Vector3(oL, oB, d));

            BuildSideQuad("Side_OuterRight",
                new Vector3(oR, oB, 0), new Vector3(oR, oT, 0),
                new Vector3(oR, oT, d), new Vector3(oR, oB, d));

            BuildSideQuad("Side_OuterTop",
                new Vector3(oR, oT, 0), new Vector3(oL, oT, 0),
                new Vector3(oL, oT, d), new Vector3(oR, oT, d));

            BuildSideQuad("Side_OuterLeft",
                new Vector3(oL, oT, 0), new Vector3(oL, oB, 0),
                new Vector3(oL, oB, d), new Vector3(oL, oT, d));


            BuildSideQuad("Side_InnerBottom",
                new Vector3(iR, iB, 0), new Vector3(iL, iB, 0),
                new Vector3(iL, iB, d), new Vector3(iR, iB, d));

            BuildSideQuad("Side_InnerRight",
                new Vector3(iR, iT, 0), new Vector3(iR, iB, 0),
                new Vector3(iR, iB, d), new Vector3(iR, iT, d));

            BuildSideQuad("Side_InnerTop",
                new Vector3(iL, iT, 0), new Vector3(iR, iT, 0),
                new Vector3(iR, iT, d), new Vector3(iL, iT, d));

            BuildSideQuad("Side_InnerLeft",
                new Vector3(iL, iB, 0), new Vector3(iL, iT, 0),
                new Vector3(iL, iT, d), new Vector3(iL, iB, d));
        }

        void BuildSideQuad(string sideName, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            GameObject go = new GameObject(sideName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            AddMeshRenderer(go, frameSideMaterial);

            var verts = new List<Vector3> { a, b, c, d };

            // Simple UV along the length
            float len = Vector3.Distance(a, b);
            var uvs = new List<Vector2>
            {
                new Vector2(0,   0),
                new Vector2(len, 0),
                new Vector2(len, frameDepth),
                new Vector2(0,   frameDepth)
            };

            var tris = new List<int>();
            AddQuad(tris, 0, 1, 2, 3);

            AssignMesh(go, verts, uvs, tris, sideName + "_Mesh");
        }


        void AddQuad(List<int> tris, int a, int b, int c, int d)
        {
            tris.Add(a); tris.Add(b); tris.Add(c);
            tris.Add(a); tris.Add(c); tris.Add(d);
        }

        void AddMeshRenderer(GameObject go, Material mat)
        {
            go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
        }

        void AssignMesh(GameObject go,
                        List<Vector3> verts,
                        List<Vector2> uvs,
                        List<int> tris,
                        string meshName)
        {
            Mesh mesh = new Mesh
            {
                name = meshName,
                vertices = verts.ToArray(),
                uv = uvs.ToArray(),
                triangles = tris.ToArray()
            };
            mesh.RecalculateNormals();
            go.GetComponent<MeshFilter>().mesh = mesh;
        }


        [ContextMenu("Rebuild Frame")]
        public void RebuildFrame()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            BuildFrame();
        }

        
    }

    
}

