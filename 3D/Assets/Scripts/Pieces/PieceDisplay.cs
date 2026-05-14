using UnityEngine;

public class PieceDisplay : MonoBehaviour
{
    public GameObject piecePrefab;
    public Material[] materials;
    public bool[] piecesUnlocked;
    public Material lockedMaterial;

    public float pieceWidth = 0.08f;
    public float pieceHeight = 0.10f;
    public float gap = -0.05f;
    public float yPosition = 1.5f;
    public float zOffset = 0.02f;

    private GameObject[] spawnedPieces;

    void Start()
    {
        // Se o array não estiver configurado, cria com tudo true
        if (piecesUnlocked == null || piecesUnlocked.Length != materials.Length)
        {
            piecesUnlocked = new bool[materials.Length];
            for (int i = 0; i < piecesUnlocked.Length; i++)
                piecesUnlocked[i] = true;
        }
        // NÃO reinicializa — usa o que está no Inspector

        GeneratePieces();
    }

    void GeneratePieces()
    {
        spawnedPieces = new GameObject[materials.Length];

        int N = materials.Length;
        float totalWidth = N * pieceWidth + (N - 1) * gap;
        float startX = totalWidth / 2f - pieceWidth / 2f;

        for (int i = 0; i < N; i++)
        {
            float x = startX - i * (pieceWidth + gap); 
            GameObject piece = Instantiate(piecePrefab, transform);
            piece.transform.localPosition = new Vector3(x, yPosition, zOffset);
            piece.transform.localScale = new Vector3(pieceWidth, pieceHeight, 0.05f);

            var renderer = piece.GetComponent<MeshRenderer>();
            renderer.material = piecesUnlocked[i] ? materials[i] : lockedMaterial;

            spawnedPieces[i] = piece;
        }
    }

    public void UnlockPiece(int index)
    {
        if (index < 0 || index >= spawnedPieces.Length) return;
        piecesUnlocked[index] = true;
        spawnedPieces[index].GetComponent<MeshRenderer>().material = materials[index];
    }
}