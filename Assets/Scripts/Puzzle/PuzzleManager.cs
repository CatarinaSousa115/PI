using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [Header("Puzzle Settings")]
        public int cols = 4;
        public int rows = 4;
        public Material paintingMaterial;

        [Header("Scatter — world positions where pieces start")]
        public Transform scatterRoot;
        public float scatterRadius = 1.5f;

        [Header("Events — hook these in the Inspector or via code")]
        public UnityEngine.Events.UnityEvent onPuzzleComplete;

        private int totalPieces;
        private int placedPieces;
        private readonly List<GameObject> pieces = new List<GameObject>();
        private Dictionary<Vector2Int, PuzzleDragger> pieceMap = new Dictionary<Vector2Int, PuzzleDragger>();


        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            totalPieces = cols * rows;
            GenerateAndSpawn();
            Scatter();
            Physics.SyncTransforms();
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
        }

        // ─── Edge Generation ───────────────────────────────────────────────

        int[,] hEdges;
        int[,] vEdges;

        void GenerateEdges()
        {
            hEdges = new int[cols - 1, rows];
            vEdges = new int[cols, rows - 1];

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols - 1; x++)
                    hEdges[x, y] = Random.value > 0.5f ? 1 : -1;

            for (int y = 0; y < rows - 1; y++)
                for (int x = 0; x < cols; x++)
                    vEdges[x, y] = Random.value > 0.5f ? 1 : -1;
        }

        // ─── Spawning ──────────────────────────────────────────────────────

        void GenerateAndSpawn()
        {
            GenerateEdges();

            
            float aspect = (float)paintingMaterial.mainTexture.width /
               paintingMaterial.mainTexture.height;
            
            float boardWidth = 1f;
            float boardHeight = 1f / aspect;

            float w = boardWidth / cols;
            float h = boardHeight / rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int[] edgeDirs = new int[4]
                    {
                        (y == 0)        ? 0 : -vEdges[x, y - 1],  // Bottom
                        (x == cols - 1) ? 0 :  hEdges[x, y],      // Right
                        (y == rows - 1) ? 0 :  vEdges[x, y],      // Top
                        (x == 0)        ? 0 : -hEdges[x - 1, y],  // Left
                    };

                    Vector3 localPos = new Vector3(
                        x * w,
                        y * h,
                        0f
                    );

                    GameObject go = new GameObject($"Piece_{x}_{y}");
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.identity;

                    go.AddComponent<MeshFilter>();
                    go.AddComponent<MeshRenderer>();

                    BoxCollider box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(w, h, 0.01f);
                    box.center = new Vector3(w / 2f, h / 2f, 0f);

                    PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
                    pp.Init(x, y, cols, rows, paintingMaterial, edgeDirs);
                    pp.correctLocalPosition = new Vector3(x * w, y * h, 0f);


                    PuzzleDragger pd = go.AddComponent<PuzzleDragger>();
                    pd.manager = this;

                    pieceMap[new Vector2Int(x, y)] = pd;

                    pieces.Add(go);
                }
            }
        }

        // ─── Scattering ────────────────────────────────────────────────────

        void Scatter()
        {
            Vector3 origin = scatterRoot != null
                ? scatterRoot.position
                : transform.position + transform.right * 1.5f;

            foreach (GameObject piece in pieces)
            {
                Vector2 rand = Random.insideUnitCircle * scatterRadius;
                piece.transform.position = origin
                    + transform.right * rand.x
                    + transform.up * rand.y;
            }
        }

        // ─── Win State ─────────────────────────────────────────────────────

        public void OnPiecePlaced()
        {
            placedPieces++;
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);

            if (placedPieces >= totalPieces)
            {
                PuzzleUI.Instance?.ShowVictory(GetElapsedTime());
                onPuzzleComplete?.Invoke();
            }
        }

        // ─── Public API ────────────────────────────────────────────────────

        float startTime;
        void OnEnable() => startTime = Time.time;
        public float GetElapsedTime() => Time.time - startTime;

        public void RestartPuzzle()
        {
            foreach (GameObject p in pieces)
                Destroy(p);
            pieces.Clear();
            pieceMap.Clear();
            placedPieces = 0;

            GenerateAndSpawn();
            Scatter();
            Physics.SyncTransforms();
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
        }
    
        public PuzzleDragger GetPiece(int x, int y)
        {
            pieceMap.TryGetValue(new Vector2Int(x, y), out PuzzleDragger dragger);
            return dragger;
        }
    }
}