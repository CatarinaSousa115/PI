using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider))]
public class PuzzlePiece : MonoBehaviour
{
    public Color lockedColor = Color.white;
    public Vector2Int CorrectSlot { get; private set; }
    public Vector3 CorrectSlotLocal { get; private set; }
    public bool IsLocked { get; private set; }

    private SpriteRenderer _sr;
    private BoxCollider _col;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<BoxCollider>();
    }

    public void Initialise(Vector2Int slot, Vector3 correctSlotLocal)
    {
        CorrectSlot = slot;
        CorrectSlotLocal = correctSlotLocal;
    }

    public void LockPiece()
    {
        IsLocked = true;
        if (_col != null) _col.enabled = false;
        if (_sr != null)
        {
            _sr.color = lockedColor;
            _sr.sortingOrder = 0;
        }
        PuzzleManager.Instance.NotifyPieceLocked(this);
    }
}