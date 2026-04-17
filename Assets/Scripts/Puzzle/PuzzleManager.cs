using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MuseumGame;

namespace Puzzle
{

    public class PuzzleManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("Narrative / Museum Integration (Required by LobbyIntroController)")]
        public MuseumRoom linkedRoom;
        public string completedObjective;
        public string completionStatus;
        public Transform scatterRoot;
        public bool autoPositionScatterRoot;
        public bool hideLegacyCanvasOnStart = true;
        public float boardWidth = 0.95f;
        public Material paintingMaterial;

        [Header("Puzzle Setup")]
        [Tooltip("The full painting texture to slice into puzzle pieces.")]
        public Texture2D paintingTexture;

        [Tooltip("Number of columns in the puzzle grid.")]
        [Range(2, 8)] public int columns = 3;

        [Tooltip("Number of rows in the puzzle grid.")]
        [Range(2, 8)] public int rows = 3;

        [Header("Frame & Layout")]
        [Tooltip("The Transform of the frame quad/plane on the wall. Pieces snap inside this area.")]
        public Transform frameTransform;

        [Tooltip("World-space size of the frame (width, height). Match your frame mesh scale.")]
        public Vector2 frameSize = new Vector2(2f, 2f);

        [Tooltip("Padding inside the frame border (world units).")]
        public float framePadding = 0.05f;

        [Header("Prefabs")]
        [Tooltip("Prefab with PuzzlePiece component, a SpriteRenderer, and a 2D collider.")]
        public GameObject piecePrefab;

        [Tooltip("Prefab for slot ghost (optional transparent overlay showing correct position).")]
        public GameObject slotGhostPrefab;

        [Header("Scatter Settings")]
        [Tooltip("Radius around the frame in which pieces are randomly scattered at start.")]
        public float scatterRadius = 1.5f;

        [Tooltip("Z offset of scattered pieces relative to frame (bring them forward so they're clickable).")]
        public float pieceZOffset = -0.05f;

        [Header("Completion")]
        [Tooltip("Fired when every piece is correctly placed.")]
        public UnityEvent onPuzzleComplete;

        [Tooltip("Optional particle system to play on completion.")]
        public ParticleSystem completionParticles;

        [Tooltip("Optional AudioClip to play on completion.")]
        public AudioClip completionSound;

        [Tooltip("Optional AudioClip to play when a piece snaps in.")]
        public AudioClip snapSound;

        // ─────────────────────────────────────────────
        // Public State (read by PuzzlePiece)
        // ─────────────────────────────────────────────

        public static PuzzleManager Instance { get; private set; }

        /// <summary>All spawned puzzle pieces, indexed [col, row].</summary>
        public PuzzlePiece[,] Pieces { get; private set; }

        /// <summary>World-space center positions for each slot [col, row].</summary>
        public Vector3[,] SlotPositions { get; private set; }

        /// <summary>Whether a slot is already occupied by a locked piece.</summary>
        public bool[,] SlotOccupied { get; private set; }

        /// <summary>World-space size of a single piece.</summary>
        public Vector2 PieceSize { get; private set; }

        /// <summary>How many pieces are currently locked in place.</summary>
        public int LockedCount { get; private set; }

        public int TotalPieces => columns * rows;

        public bool IsActive { get; private set; }

        // ─────────────────────────────────────────────
        // Private
        // ─────────────────────────────────────────────

        private AudioSource _audio;
        private List<PuzzlePiece> _allPieces = new List<PuzzlePiece>();

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audio = GetComponent<AudioSource>();
            if (_audio == null)
                _audio = gameObject.AddComponent<AudioSource>();
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        public void StartPuzzle()
        {
            if (IsActive) return;
            IsActive = true;
            LockedCount = 0;

            GenerateSlots();
            GeneratePieces();
            ScatterPieces();
        }

        public void ResetPuzzle()
        {
            foreach (var piece in _allPieces)
                if (piece != null) Destroy(piece.gameObject);

            _allPieces.Clear();
            LockedCount = 0;
            IsActive = false;
        }

        public void NotifyPieceLocked(PuzzlePiece piece)
        {
            SlotOccupied[piece.CorrectSlot.x, piece.CorrectSlot.y] = true;
            LockedCount++;

            PlaySound(snapSound);

            if (LockedCount >= TotalPieces)
                StartCoroutine(CompletionSequence());
        }

        public Vector3 GetSlotWorldPosition(Vector2Int slot)
        {
            return SlotPositions[slot.x, slot.y];
        }

        public Vector2Int GetNearestSlot(Vector3 worldPos, out float distance)
        {
            Vector2Int nearest = Vector2Int.zero;
            float best = float.MaxValue;

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    float d = Vector3.Distance(worldPos, SlotPositions[c, r]);
                    if (d < best)
                    {
                        best = d;
                        nearest = new Vector2Int(c, r);
                    }
                }
            }

            distance = best;
            return nearest;
        }

        // ─────────────────────────────────────────────
        // Private: Generation
        // ─────────────────────────────────────────────

        private void GenerateSlots()
        {
            SlotPositions = new Vector3[columns, rows];
            SlotOccupied = new bool[columns, rows];

            float usableWidth = frameSize.x - framePadding * 2f;
            float usableHeight = frameSize.y - framePadding * 2f;

            PieceSize = new Vector2(usableWidth / columns, usableHeight / rows);

            float startX = -usableWidth / 2f + PieceSize.x / 2f;
            float startY = -usableHeight / 2f + PieceSize.y / 2f;

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    Vector3 localPos = new Vector3(
                        startX + c * PieceSize.x,
                        startY + r * PieceSize.y,
                        0f
                    );
                    SlotPositions[c, r] = frameTransform.TransformPoint(localPos);

                    if (slotGhostPrefab != null)
                    {
                        var ghost = Instantiate(slotGhostPrefab, SlotPositions[c, r], frameTransform.rotation, frameTransform);
                        ghost.transform.localScale = new Vector3(PieceSize.x, PieceSize.y, 1f);
                    }
                }
            }
        }

        private void GeneratePieces()
        {
            if (paintingTexture == null)
            {
                Debug.LogError("[PuzzleManager] No painting texture assigned!");
                return;
            }

            Pieces = new PuzzlePiece[columns, rows];

            int piecePixelW = paintingTexture.width / columns;
            int piecePixelH = paintingTexture.height / rows;

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    Texture2D sliceTex = SliceTexture(paintingTexture, c * piecePixelW, r * piecePixelH, piecePixelW, piecePixelH);

                    float ppu = sliceTex.width / PieceSize.x;
                    Sprite sprite = Sprite.Create(
                        sliceTex,
                        new Rect(0, 0, sliceTex.width, sliceTex.height),
                        new Vector2(0.5f, 0.5f),
                        ppu
                    );

                    GameObject go = Instantiate(piecePrefab, SlotPositions[c, r], frameTransform.rotation);
                    go.name = $"Piece_{c}_{r}";
                    go.transform.parent = null;

                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = sprite;

                    var piece = go.GetComponent<PuzzlePiece>();
                    if (piece == null)
                    {
                        Debug.LogError("[PuzzleManager] piecePrefab is missing PuzzlePiece component!");
                        Destroy(go);
                        continue;
                    }

                    piece.Initialise(new Vector2Int(c, r), SlotPositions[c, r], PieceSize);
                    Pieces[c, r] = piece;
                    _allPieces.Add(piece);
                }
            }
        }

        private void ScatterPieces()
        {
            foreach (var piece in _allPieces)
            {
                if (piece == null) continue;

                // Scatter around the scatterRoot if it exists, otherwise fall back to the frameTransform
                Transform scatterCenter = scatterRoot != null ? scatterRoot : frameTransform;

                Vector2 rnd = Random.insideUnitCircle.normalized * Random.Range(scatterRadius * 0.5f, scatterRadius);
                Vector3 scatter = scatterCenter.position + new Vector3(rnd.x, rnd.y, 0f);
                scatter.z = scatterCenter.position.z + pieceZOffset;

                piece.transform.position = scatter;
                piece.transform.rotation = frameTransform.rotation * Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f));
            }
        }

        // ─────────────────────────────────────────────
        // Private: Helpers
        // ─────────────────────────────────────────────

        private Texture2D SliceTexture(Texture2D source, int x, int y, int width, int height)
        {
            Color[] pixels = source.GetPixels(x, y, width, height);
            Texture2D result = new Texture2D(width, height, source.format, false);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audio != null)
                _audio.PlayOneShot(clip);
        }

        private IEnumerator CompletionSequence()
        {
            yield return new WaitForSeconds(0.3f);

            if (completionParticles != null)
                completionParticles.Play();

            PlaySound(completionSound);

            onPuzzleComplete?.Invoke();

            Debug.Log("[PuzzleManager] Puzzle complete!");
        }

    }
}