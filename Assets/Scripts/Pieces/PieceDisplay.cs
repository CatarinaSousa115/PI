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

        // Inicializa o estado com base nos progressos guardados no MuseumGameManager
        if (MuseumGame.MuseumGameManager.Instance != null)
        {
            InitializeFromGameManager();
            MuseumGame.MuseumGameManager.Instance.RoomCompleted += OnRoomCompleted;
        }

        GeneratePieces();
    }

    void OnDestroy()
    {
        if (MuseumGame.MuseumGameManager.Instance != null)
        {
            MuseumGame.MuseumGameManager.Instance.RoomCompleted -= OnRoomCompleted;
        }
    }

    private void InitializeFromGameManager()
    {
        if (MuseumGame.MuseumGameManager.Instance.IsRoomComplete(MuseumGame.MuseumRoomId.Reconstruction))
        {
            piecesUnlocked[5] = true;
            piecesUnlocked[10] = true;
            piecesUnlocked[15] = true;
            piecesUnlocked[20] = true;
        }
        else
        {
            if (MuseumGame.MuseumGameManager.Instance.IsRoomComplete(MuseumGame.MuseumRoomId.Conversations))
                piecesUnlocked[10] = true;
            if (MuseumGame.MuseumGameManager.Instance.IsRoomComplete(MuseumGame.MuseumRoomId.Lights))
                piecesUnlocked[15] = true;
            if (MuseumGame.MuseumGameManager.Instance.IsRoomComplete(MuseumGame.MuseumRoomId.Minigames))
                piecesUnlocked[20] = true;
        }
    }

    private void OnRoomCompleted(MuseumGame.MuseumRoomId roomId)
    {
        if (roomId == MuseumGame.MuseumRoomId.Reconstruction)
        {
            UnlockPiece(5);
            UnlockPiece(10);
            UnlockPiece(15);
            UnlockPiece(20);
        }
        else
        {
            int index = MapRoomIdToPieceIndex(roomId);
            if (index != -1)
            {
                UnlockPiece(index);
            }
        }
    }

    private int MapRoomIdToPieceIndex(MuseumGame.MuseumRoomId roomId)
    {
        switch (roomId)
        {
            case MuseumGame.MuseumRoomId.Reconstruction: return 5;
            case MuseumGame.MuseumRoomId.Conversations: return 10;
            case MuseumGame.MuseumRoomId.Lights: return 15;
            case MuseumGame.MuseumRoomId.Minigames: return 20;
            default: return -1;
        }
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
        if (spawnedPieces == null || index < 0 || index >= spawnedPieces.Length) return;
        piecesUnlocked[index] = true;
        if (spawnedPieces[index] != null)
        {
            var renderer = spawnedPieces[index].GetComponent<MeshRenderer>();
            if (renderer != null && index < materials.Length)
            {
                renderer.material = materials[index];
            }
        }
    }
}