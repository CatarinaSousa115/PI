using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class PuzzlePiece : MonoBehaviour
{
    public Color lockedColor = Color.white;
    public Vector2Int CorrectSlot { get; private set; }
    public Vector3 CorrectWorldPos { get; private set; }
    public bool IsLocked { get; private set; }

    private SpriteRenderer _sr;
    private BoxCollider _col;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        ResizeCollider();
    }

    public void Initialise(Vector2Int slot, Vector3 correctWorldPos)
    {
        CorrectSlot = slot;
        CorrectWorldPos = correctWorldPos;
    }

    public void LockPiece()
    {
        IsLocked = true;
        _col.enabled = false;
        _sr.color = lockedColor;
        _sr.sortingOrder = 0;
        PuzzleManager.Instance.NotifyPieceLocked(this);
    }

    private void ResizeCollider()
    {
        if (_sr == null || _sr.sprite == null || _col == null) return;
        Bounds b = _sr.sprite.bounds;
        _col.center = b.center;
        _col.size = new Vector3(b.size.x, b.size.y, 0.01f);
    }
}