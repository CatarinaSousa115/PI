using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TopColorCube : MonoBehaviour
{
    public Material sideMaterial;
    public Material topMaterial;

    void Start()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = {
            // Frente
            new Vector3(-0.5f,-0.5f, 0.5f), new Vector3( 0.5f,-0.5f, 0.5f),
            new Vector3( 0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
            // Trás
            new Vector3( 0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f,-0.5f),
            new Vector3(-0.5f, 0.5f,-0.5f), new Vector3( 0.5f, 0.5f,-0.5f),
            // Cima (submesh separada)
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3( 0.5f, 0.5f, 0.5f),
            new Vector3( 0.5f, 0.5f,-0.5f), new Vector3(-0.5f, 0.5f,-0.5f),
            // Baixo
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3( 0.5f,-0.5f,-0.5f),
            new Vector3( 0.5f,-0.5f, 0.5f), new Vector3(-0.5f,-0.5f, 0.5f),
            // Esquerda
            new Vector3(-0.5f,-0.5f,-0.5f), new Vector3(-0.5f,-0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f,-0.5f),
            // Direita
            new Vector3( 0.5f,-0.5f, 0.5f), new Vector3( 0.5f,-0.5f,-0.5f),
            new Vector3( 0.5f, 0.5f,-0.5f), new Vector3( 0.5f, 0.5f, 0.5f),
        };

        mesh.vertices = vertices;
        mesh.subMeshCount = 2;

        // Submesh 0 — todas as faces EXCETO o topo (faces 0,1,3,4,5)
        int[] sidesTriangles = new int[5 * 6];
        int[] faceOrder = { 0, 1, 3, 4, 5 }; // índices das faces laterais
        for (int i = 0; i < faceOrder.Length; i++)
        {
            int offset = faceOrder[i] * 4;
            int t = i * 6;
            sidesTriangles[t] = offset; sidesTriangles[t + 1] = offset + 1; sidesTriangles[t + 2] = offset + 2;
            sidesTriangles[t + 3] = offset; sidesTriangles[t + 4] = offset + 2; sidesTriangles[t + 5] = offset + 3;
        }

        // Submesh 1 — apenas o TOPO (face 2)
        int topOffset = 2 * 4;
        int[] topTriangles = {
            topOffset, topOffset+1, topOffset+2,
            topOffset, topOffset+2, topOffset+3
        };

        mesh.SetTriangles(sidesTriangles, 0);
        mesh.SetTriangles(topTriangles, 1);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().materials = new Material[] { sideMaterial, topMaterial };
    }
}