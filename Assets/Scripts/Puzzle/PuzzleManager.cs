using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    // Place this (with PuzzleInputHandler) on the PuzzleBoard GameObject.
    // The board's Transform controls where the puzzle sits in the 3D world.
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [Header("Puzzle Settings")]
        public int cols = 4;
        public int rows = 4;
        public Material paintingMaterial;

        [Header("Scatter — world positions where pieces start")]
        public Transform scatterRoot;         // empty GameObject that defines the scatter area center
        public float scatterRadius = 1.5f;

        [Header("Events — hook these in the Inspector or via code")]
        public UnityEngine.Events.UnityEvent onPuzzleComplete;

        private int totalPieces;
        private int placedPieces;
        private readonly List<GameObject> pieces = new List<GameObject>();

        void Awake()
        {
            // Allow other systems to reference the puzzle manager
            // without assuming it's a global singleton
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
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
        }

        // ─── Edge Generation ───────────────────────────────────────────────

        int[,] hEdges; // [col-border index, row] — between columns
        int[,] vEdges; // [col, row-border index] — between rows

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

                    // Correct local position on the board (0→1 range)
                    Vector3 localPos = new Vector3(
                        x * (1f / cols),
                        y * (1f / rows),
                        0f
                    );

                    GameObject go = new GameObject($"Piece_{x}_{y}");
                    go.layer = LayerMask.NameToLayer("Puzzle");

                    // Parent to this board so it inherits position/rotation
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.identity;

                    go.AddComponent<MeshFilter>();
                    go.AddComponent<MeshRenderer>();
                    go.AddComponent<MeshCollider>();

                    PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
                    pp.Init(x, y, cols, rows, paintingMaterial, edgeDirs);
                    pp.correctLocalPosition = localPos;

                    PuzzleDragger pd = go.AddComponent<PuzzleDragger>();
                    pd.manager = this;

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

        // ─── Public API (call from other systems) ─────────────────────────

        float startTime;

        void OnEnable() => startTime = Time.time;

        public float GetElapsedTime() => Time.time - startTime;

        // Call this from your game to reset/restart the puzzle
        public void RestartPuzzle()
        {
            foreach (GameObject p in pieces)
                Destroy(p);
            pieces.Clear();
            placedPieces = 0;

            GenerateAndSpawn();
            Scatter();
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
        }
    }
}