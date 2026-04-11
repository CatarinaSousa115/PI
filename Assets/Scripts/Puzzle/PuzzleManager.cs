using UnityEngine;
using System.Collections.Generic;
using MuseumGame;
using System.Collections;

namespace Puzzle
{
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [Header("Puzzle Settings")]
        public int cols = 4;
        public int rows = 4;
        public Material paintingMaterial;
        public float boardWidth = 1f;
        public float pieceSurfaceOffset = 0.03f;

        [Header("Scatter — world positions where pieces start")]
        public Transform scatterRoot;
        public float scatterRadius = 1.5f;
        public bool autoPositionScatterRoot = true;
        public float scatterPadding = 0.35f;

        [Header("Scatter Highlight")]
        public GameObject scatterHighlight;

        [Header("Board Highlight")]
        public GameObject boardHighlight;

        [Header("Events — hook these in the Inspector or via code")]
        public UnityEngine.Events.UnityEvent onPuzzleComplete;

        [Header("Museum Flow")]
        public MuseumRoom linkedRoom;
        [TextArea(2, 4)]
        public string completedObjective = "A sala foi restaurada. A primeira placa foi recuperada.";
        public string completionStatus = "Primeira placa recuperada.";

        [Header("Legacy UI")]
        public bool hideLegacyCanvasOnStart = true;
        public string legacyCanvasName = "PuzzleCanvas";

        [Header("Completion Animation")]
        public Transform frameCenter;
        public float snapDuration = 0.8f;

        private int totalPieces;
        private int placedPieces;
        private readonly List<GameObject> pieces = new List<GameObject>();
        private Dictionary<Vector2Int, PuzzleDragger> pieceMap = new Dictionary<Vector2Int, PuzzleDragger>();

        // ─── Lifecycle ─────────────────────────────────────────────────────

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
            ResolveLinkedRoom();
            HideLegacyCanvas();
            ConfigureScatterRoot();
            GenerateAndSpawn();
            Scatter();
            SetupHighlights();
            Physics.SyncTransforms();
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
        }


        void SetupBoardHighlight()
        {
            if (boardHighlight == null) return;
            if (paintingMaterial == null || paintingMaterial.mainTexture == null) return;

            float aspect = (float)paintingMaterial.mainTexture.width /
                                        paintingMaterial.mainTexture.height;
            float boardHeight = boardWidth / aspect;

            // Size it to match the board exactly
            boardHighlight.transform.localScale = new Vector3(
                boardWidth,
                boardHeight,
                1f
            );

            // Center it on the board, slightly behind pieces
            boardHighlight.transform.localPosition = new Vector3(
                boardWidth / 2f,
                boardHeight / 2f,
                0.05f          // behind pieces (positive Z = behind in your setup)
            );

            boardHighlight.SetActive(true);
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
            if (paintingMaterial == null || paintingMaterial.mainTexture == null)
            {
                Debug.LogError("[PuzzleManager] Assign a material with a texture before generating the puzzle.");
                return;
            }

            GenerateEdges();

            float aspect = (float)paintingMaterial.mainTexture.width /
                                        paintingMaterial.mainTexture.height;
            float boardHeight = boardWidth / aspect;

            float w = boardWidth / cols;
            float h = boardHeight / rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int[] edgeDirs = new int[4]
                    {
                        (y == 0)        ? 0 : -vEdges[x, y - 1],
                        (x == cols - 1) ? 0 :  hEdges[x, y],
                        (y == rows - 1) ? 0 :  vEdges[x, y],
                        (x == 0)        ? 0 : -hEdges[x - 1, y],
                    };

                    Vector3 localPos = new Vector3(
                        x * w,
                        y * h,
                        pieceSurfaceOffset
                    );

                    GameObject go = new GameObject($"Piece_{x}_{y}");
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.identity;

                    go.AddComponent<MeshFilter>();
                    go.AddComponent<MeshRenderer>();

                    float tabSize = Mathf.Min(w, h) * 0.3f;
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(w + tabSize * 2f, h + tabSize * 2f, 0.01f);
                    box.center = new Vector3(w / 2f, h / 2f, 0f);

                    PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
                    pp.Init(x, y, cols, rows, paintingMaterial, edgeDirs, w, h);
                    pp.correctLocalPosition = new Vector3(x * w, y * h, pieceSurfaceOffset);

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

        // ─── Scatter Highlight ─────────────────────────────────────────────

        void SetupScatterHighlight()
        {
            if (scatterHighlight == null) return;

            // Size it to match the scatter radius
            float diameter = scatterRadius * 2f;
            scatterHighlight.transform.localScale = new Vector3(diameter, diameter, 1f);

            // Position it at the scatter origin
            Vector3 origin = scatterRoot != null
                ? scatterRoot.position
                : transform.position + transform.right * 1.5f;

            scatterHighlight.transform.position = origin;
            scatterHighlight.SetActive(true);
        }

        void HideScatterHighlight()
        {
            if (scatterHighlight == null) return;
            
            scatterHighlight.SetActive(false);
        }

        // ─── Win State ─────────────────────────────────────────────────────

        public void OnPiecePlaced()
        {
            placedPieces++;
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);

            if (placedPieces >= totalPieces)
            {
                // Hide the scatter area — puzzle is complete
                HideScatterHighlight();

                if (MuseumGameManager.Instance != null && !string.IsNullOrWhiteSpace(completedObjective))
                    MuseumGameManager.Instance.SetObjective(completedObjective);

                MuseumHud.Instance?.SetStatus(completionStatus);
                linkedRoom?.MarkComplete();
                onPuzzleComplete?.Invoke();
                StartCoroutine(CompletionSnap());
            }
        }

        IEnumerator CompletionSnap()
        {

            if (boardHighlight != null) boardHighlight.SetActive(false);


            // Disable input immediately
            var inputHandler = GetComponent<PuzzleInputHandler>();
            if (inputHandler != null) inputHandler.enabled = false;

            // Lock all pieces
            foreach (GameObject p in pieces)
            {
                var dragger = p.GetComponent<PuzzleDragger>();
                if (dragger != null) dragger.enabled = false;
            }

            // Animate board into frame
            Vector3 startPos = transform.position;
            Vector3 targetPos = frameCenter != null
                ? frameCenter.position
                : transform.position;

            float elapsed = 0f;

            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / snapDuration;
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }

            transform.position = targetPos;

            PuzzleUI.Instance?.ShowVictory(GetElapsedTime());
        }

        // ─── Gizmos ────────────────────────────────────────────────────────

        void OnDrawGizmos()
        {
            if (frameCenter == null) return;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(frameCenter.position, 0.05f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, frameCenter.position);
        }

        // ─── Public API ────────────────────────────────────────────────────

        float startTime;
        void OnEnable() => startTime = Time.time;
        public float GetElapsedTime() => Time.time - startTime;

        public void RestartPuzzle()
        {
            startTime = Time.time;
            HideLegacyCanvas();
            ConfigureScatterRoot();

            foreach (GameObject p in pieces)
                Destroy(p);
            pieces.Clear();
            pieceMap.Clear();
            placedPieces = 0;

            var inputHandler = GetComponent<PuzzleInputHandler>();
            if (inputHandler != null) inputHandler.enabled = true;

            GenerateAndSpawn();
            Scatter();
            SetupScatterHighlight();
            Physics.SyncTransforms();
            PuzzleUI.Instance?.UpdateCounter(placedPieces, totalPieces);
            SetupBoardHighlight();
        }

        public PuzzleDragger GetPiece(int x, int y)
        {
            pieceMap.TryGetValue(new Vector2Int(x, y), out PuzzleDragger dragger);
            return dragger;
        }

        // ─── Private Helpers ───────────────────────────────────────────────

        private void ResolveLinkedRoom()
        {
            if (linkedRoom != null) return;

            MuseumRoom[] rooms = FindObjectsByType<MuseumRoom>(FindObjectsSortMode.None);
            foreach (MuseumRoom room in rooms)
            {
                if (room.roomId == MuseumRoomId.Reconstruction)
                {
                    linkedRoom = room;
                    return;
                }
            }
        }

        private void HideLegacyCanvas()
        {
            if (!hideLegacyCanvasOnStart) return;

            Transform legacyCanvas = transform.Find(legacyCanvasName);
            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);
        }

        private void ConfigureScatterRoot()
        {
            if (!autoPositionScatterRoot
                || scatterRoot == null
                || paintingMaterial == null
                || paintingMaterial.mainTexture == null)
                return;

            float aspect = (float)paintingMaterial.mainTexture.width /
                                        paintingMaterial.mainTexture.height;
            float boardHeight = boardWidth / aspect;

            scatterRoot.localPosition = new Vector3(
                boardWidth + scatterPadding,
                boardHeight * 0.5f,
                0f
            );
        }

        void SetupHighlights()
        {
            if (paintingMaterial == null || paintingMaterial.mainTexture == null) return;

            float aspect = (float)paintingMaterial.mainTexture.width /
                                        paintingMaterial.mainTexture.height;
            float boardHeight = boardWidth / aspect;

            if (boardHighlight != null)
            {
                // Account for parent's scale so world size matches board exactly
                Vector3 parentScale = boardHighlight.transform.parent != null
                    ? boardHighlight.transform.parent.lossyScale
                    : Vector3.one;

                boardHighlight.transform.localScale = new Vector3(
                    boardWidth / parentScale.x,
                    boardHeight / parentScale.y,
                    1f
                );
            }

            if (scatterHighlight != null)
            {
                float diameter = scatterRadius * 2f;

                Vector3 parentScale = scatterHighlight.transform.parent != null
                    ? scatterHighlight.transform.parent.lossyScale
                    : Vector3.one;

                scatterHighlight.transform.localScale = new Vector3(
                    diameter / parentScale.x,
                    diameter / parentScale.y,
                    1f
                );
            }
        }
    }
}