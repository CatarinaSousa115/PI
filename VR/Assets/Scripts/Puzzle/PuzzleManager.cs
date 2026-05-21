using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Header("Player & Mode")]
    public GameObject playerObject;

    [Header("Puzzle Setup")]
    public Texture2D paintingTexture;
    [Range(2, 10)] public int columns = 3;
    [Range(2, 10)] public int rows = 3;

    [Header("Frame & Layout")]
    public Transform frameTransform;
    private Transform _pieceContainer;
    [Tooltip("The size of the puzzle area inside the frame in local units.")]
    public Vector2 frameSize = new Vector2(1f, 1f);
    [Tooltip("Offset to shift the entire puzzle grid.")]
    public Vector2 frameOffset = Vector2.zero;
    [Tooltip("If true, the puzzle centers itself on the mesh bounds.")]
    public bool autoCenterOnMesh = true;

    [Header("Prefabs")]
    public GameObject piecePrefab;

    [Header("Input")]
    public bool ensureInputHandler = true;

    [Header("Scatter Settings")]
    public bool scatterInsideFrame = true;
    public float scatterRadius = 1.0f;
    public float pieceDepthOffset = 0.15f;

    [Header("Completion")]
    public UnityEvent onPuzzleComplete;
    public ParticleSystem completionParticles;
    public AudioClip completionSound;
    public AudioClip snapSound;

    public static PuzzleManager Instance { get; private set; }
    public Vector3[,] SlotPositions { get; private set; }
    public Vector2 PieceSize { get; private set; }
    public int LockedCount { get; private set; }
    public int TotalPieces => columns * rows;
    public bool IsActive { get; private set; }

    private readonly List<GameObject> _allPieces = new();
    private readonly Dictionary<Vector2Int, PuzzleDragger> _pieceMap = new();
    private AudioSource _audio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _audio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        StartPuzzle();
    }

    public void EnterPuzzleMode()
    {
        IsActive = true;

        if (_allPieces.Count == 0)
            StartPuzzle();
    }

    public void ExitPuzzleMode()
    {
        IsActive = false;
    }

    public void StartPuzzle()
    {
        if (_allPieces.Count > 0 || paintingTexture == null || frameTransform == null)
            return;

        GameObject containerGO = new GameObject("PieceContainer");
        _pieceContainer = containerGO.transform;
        _pieceContainer.position = frameTransform.position;
        _pieceContainer.rotation = frameTransform.rotation;
        _pieceContainer.localScale = Vector3.one;

        GenerateSlots();
        GeneratePieces();
        ScatterPieces();
        EnsureInputHandler();
    }

    public void ResetPuzzle()
    {
        foreach (var p in _allPieces)
        {
            if (p != null)
                Destroy(p);
        }

        _allPieces.Clear();
        _pieceMap.Clear();
        LockedCount = 0;
        IsActive = false;
    }

    private void GenerateSlots()
    {
        SlotPositions = new Vector3[columns, rows];

        float worldWidth = Mathf.Abs(frameSize.x * frameTransform.lossyScale.x);
        float worldHeight = Mathf.Abs(frameSize.y * frameTransform.lossyScale.y);

        Vector3 center = Vector3.zero;
        if (autoCenterOnMesh)
        {
            Renderer rend = frameTransform.GetComponent<Renderer>()
                         ?? frameTransform.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                center = _pieceContainer.InverseTransformPoint(rend.bounds.center);
                center.z = 0;
            }
        }

        PieceSize = new Vector2(worldWidth / columns, worldHeight / rows);

        float startX = center.x - worldWidth / 2f + PieceSize.x / 2f + frameOffset.x * frameTransform.lossyScale.x;
        float startY = center.y - worldHeight / 2f + PieceSize.y / 2f + frameOffset.y * frameTransform.lossyScale.y;

        for (int c = 0; c < columns; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                SlotPositions[c, r] = new Vector3(startX + c * PieceSize.x, startY + r * PieceSize.y, -0.01f);
            }
        }
    }

    private void GeneratePieces()
    {
        int pixW = paintingTexture.width / columns;
        int pixH = paintingTexture.height / rows;

        for (int c = 0; c < columns; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                Texture2D slice = SliceTexture(paintingTexture, c * pixW, r * pixH, pixW, pixH);
                if (slice == null)
                    continue;

                Sprite sprite = Sprite.Create(slice, new Rect(0, 0, slice.width, slice.height), new Vector2(0.5f, 0.5f), 100f);
                GameObject go = Instantiate(piecePrefab, _pieceContainer, false);
                go.name = $"Piece_{c}_{r}";

                var spriteRenderer = go.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.sprite = sprite;

                float sw = slice.width / 100f;
                float sh = slice.height / 100f;
                go.transform.localScale = new Vector3(PieceSize.x / sw, PieceSize.y / sh, 1f);

                Vector3 slotPos = SlotPositions[c, r];
                go.transform.localPosition = slotPos;

                var box = go.GetComponent<BoxCollider>();
                if (box == null)
                    box = go.AddComponent<BoxCollider>();

                box.isTrigger = false;
                box.size = new Vector3(Mathf.Abs(sw), Mathf.Abs(sh), 0.1f);

                var piece = go.GetComponent<PuzzlePiece>() ?? go.AddComponent<PuzzlePiece>();
                piece.Initialise(new Vector2Int(c, r), slotPos);

                var dragger = go.GetComponent<PuzzleDragger>() ?? go.AddComponent<PuzzleDragger>();
                dragger.manager = this;
                dragger.piece = piece;

                _pieceMap[new Vector2Int(c, r)] = dragger;
                _allPieces.Add(go);

                // Do not add VRPuzzlePiece/XRGrabInteractable here.
                // This puzzle is controlled by PuzzleInputHandler + PuzzleDragger raycast logic.
            }
        }
    }

    private void ScatterPieces()
    {
        float halfW = Mathf.Abs(frameSize.x * frameTransform.lossyScale.x) / 2f;
        float halfH = Mathf.Abs(frameSize.y * frameTransform.lossyScale.y) / 2f;

        float marginX = PieceSize.x / 2f;
        float marginY = PieceSize.y / 2f;

        foreach (var go in _allPieces)
        {
            Vector3 pos = new Vector3(
                Random.Range(-halfW + marginX, halfW - marginX),
                Random.Range(-halfH + marginY, halfH - marginY),
                -pieceDepthOffset - Random.Range(0f, 0.01f)
            );

            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));

            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sortingOrder = 5;
        }
    }

    private Texture2D SliceTexture(Texture2D src, int x, int y, int w, int h)
    {
        try
        {
            Color[] px = src.GetPixels(x, y, w, h);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
        catch
        {
            return null;
        }
    }

    private void EnsureInputHandler()
    {
        if (!ensureInputHandler || FindFirstObjectByType<PuzzleInputHandler>() != null)
            return;

        gameObject.AddComponent<PuzzleInputHandler>();
    }

    public void NotifyPieceLocked(PuzzlePiece piece)
    {
        LockedCount++;
        PlaySound(snapSound);

        if (LockedCount >= TotalPieces)
            StartCoroutine(CompletionSequence());
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
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (frameTransform == null)
            return;

        Gizmos.matrix = frameTransform.localToWorldMatrix;
        Vector3 center = Vector3.zero;
        Renderer rend = frameTransform.GetComponent<Renderer>() ?? frameTransform.GetComponentInChildren<Renderer>();
        if (autoCenterOnMesh && rend != null)
        {
            center = frameTransform.InverseTransformPoint(rend.bounds.center);
            center.z = 0;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center + (Vector3)frameOffset, new Vector3(frameSize.x, frameSize.y, 0.01f));
    }
#endif
}
