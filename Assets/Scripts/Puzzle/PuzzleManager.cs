using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MuseumGame.Player;

public class PuzzleManager : MonoBehaviour
{
    [Header("Player & Mode")]
    public GameObject playerObject;
    private MonoBehaviour playerScript;

    [Header("Puzzle Setup")]
    public Texture2D paintingTexture;
    [Range(2, 8)] public int columns = 3;
    [Range(2, 8)] public int rows = 3;

    [Header("Frame & Layout")]
    public Transform frameTransform;
    public Vector2 frameSize = new Vector2(2f, 2f);
    public float framePadding = 0.05f;

    [Header("Prefabs")]
    public GameObject piecePrefab;
    public GameObject slotGhostPrefab;

    [Header("Scatter Settings")]
    public bool scatterInsideFrame = true;
    public float scatterRadius = 1.5f;
    public float pieceDepthOffset = 0.03f;

    [Header("Completion")]
    public UnityEvent onPuzzleComplete;
    public ParticleSystem completionParticles;
    public AudioClip completionSound;
    public AudioClip snapSound;

    public static PuzzleManager Instance { get; private set; }

    public Vector3[,] SlotPositions { get; private set; }
    public bool[,] SlotOccupied { get; private set; }
    public Vector2 PieceSize { get; private set; }

    public int LockedCount { get; private set; }
    public int TotalPieces => columns * rows;
    public bool IsActive { get; private set; }

    private AudioSource _audio;
    private List<GameObject> _allPieces = new List<GameObject>();
    private Dictionary<Vector2Int, PuzzleDragger> _pieceMap = new Dictionary<Vector2Int, PuzzleDragger>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        if (playerObject != null)
            playerScript = playerObject.GetComponent<SimpleFirstPersonController>();
    }

    // ─── Mode Switching ──────────────────────────────────────────

    public void EnterPuzzleMode()
    {
        IsActive = true;

        if (playerScript != null)
        {
            playerScript.enabled = false;
            var cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartPuzzle();
    }

    public void ExitPuzzleMode()
    {
        IsActive = false;

        if (playerScript != null)
        {
            playerScript.enabled = true;
            var cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ─── Puzzle Lifecycle ────────────────────────────────────────

    public void StartPuzzle()
    {
        if (_allPieces.Count > 0) return; // Already started
        LockedCount = 0;

        GenerateSlots();
        GeneratePieces();
        ScatterPieces();
    }

    public void ResetPuzzle()
    {
        foreach (var p in _allPieces) if (p != null) Destroy(p);
        _allPieces.Clear();
        _pieceMap.Clear();
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

    public PuzzleDragger GetPiece(int x, int y)
    {
        _pieceMap.TryGetValue(new Vector2Int(x, y), out PuzzleDragger dragger);
        return dragger;
    }

    // ─── Generation Logic ────────────────────────────────────────

    private void GenerateSlots()
    {
        SlotPositions = new Vector3[columns, rows];
        SlotOccupied = new bool[columns, rows];

        float usableW = frameSize.x - framePadding * 2f;
        float usableH = frameSize.y - framePadding * 2f;
        PieceSize = new Vector2(usableW / columns, usableH / rows);

        float startX = -usableW / 2f + PieceSize.x / 2f;
        float startY = -usableH / 2f + PieceSize.y / 2f;

        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
            {
                Vector3 localPos = new Vector3(startX + c * PieceSize.x, startY + r * PieceSize.y, 0f);
                SlotPositions[c, r] = frameTransform.TransformPoint(localPos);

                if (slotGhostPrefab != null)
                {
                    var ghost = Instantiate(slotGhostPrefab, SlotPositions[c, r], frameTransform.rotation, frameTransform);
                    Vector3 fs = frameTransform.lossyScale;
                    ghost.transform.localScale = new Vector3(PieceSize.x / fs.x, PieceSize.y / fs.y, 0.001f);
                }
            }
    }

    private void GeneratePieces()
    {
        if (paintingTexture == null) return;

        int pixW = paintingTexture.width / columns;
        int pixH = paintingTexture.height / rows;

        for (int c = 0; c < columns; c++)
            for (int r = 0; r < rows; r++)
            {
                Texture2D slice = SliceTexture(paintingTexture, c * pixW, r * pixH, pixW, pixH);
                float ppu = pixW / PieceSize.x;
                Sprite sprite = Sprite.Create(slice, new Rect(0, 0, slice.width, slice.height), new Vector2(0.5f, 0.5f), ppu);

                GameObject go = Instantiate(piecePrefab, SlotPositions[c, r], frameTransform.rotation);
                go.name = $"Piece_{c}_{r}";

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = sprite;

                // Setup Data Component
                var piece = go.GetComponent<PuzzlePiece>();
                piece.Initialise(new Vector2Int(c, r), SlotPositions[c, r]);

                // Setup Interaction Component
                var dragger = go.AddComponent<PuzzleDragger>();
                dragger.manager = this;
                dragger.piece = piece;

                _pieceMap[new Vector2Int(c, r)] = dragger;
                _allPieces.Add(go);
            }
    }

    private void ScatterPieces()
    {
        float usableW = frameSize.x - framePadding * 2f;
        float usableH = frameSize.y - framePadding * 2f;

        foreach (var go in _allPieces)
        {
            Vector3 worldPos;
            if (scatterInsideFrame)
            {
                float lx = Random.Range(-usableW / 2f, usableW / 2f);
                float ly = Random.Range(-usableH / 2f, usableH / 2f);
                worldPos = frameTransform.TransformPoint(new Vector3(lx, ly, 0f));
            }
            else
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(scatterRadius * 0.5f, scatterRadius);
                worldPos = frameTransform.position + frameTransform.right * (Mathf.Cos(angle) * radius) + frameTransform.up * (Mathf.Sin(angle) * radius);
            }

            worldPos -= frameTransform.forward * pieceDepthOffset;
            go.transform.position = worldPos;

            float tiltAngle = Random.Range(-20f, 20f);
            go.transform.rotation = Quaternion.AngleAxis(tiltAngle, frameTransform.forward) * frameTransform.rotation;
        }
    }

    private Texture2D SliceTexture(Texture2D src, int x, int y, int w, int h)
    {
        Color[] px = src.GetPixels(x, y, w, h);
        Texture2D tex = new Texture2D(w, h, src.format, false);
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    private void PlaySound(AudioClip clip) { if (clip != null && _audio != null) _audio.PlayOneShot(clip); }

    private IEnumerator CompletionSequence()
    {
        yield return new WaitForSeconds(0.3f);
        completionParticles?.Play();
        PlaySound(completionSound);
        onPuzzleComplete?.Invoke();
    }
}