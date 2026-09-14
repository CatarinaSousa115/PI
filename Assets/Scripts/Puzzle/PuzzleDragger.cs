using UnityEngine;

/// <summary>
/// Handles dragging logic for a single puzzle piece.
/// Simplified version: now uses LOCAL space relative to the frame for all movement.
/// </summary>
public class PuzzleDragger : MonoBehaviour
{
    [HideInInspector] public PuzzleManager manager;
    [HideInInspector] public PuzzlePiece piece;

    [Tooltip("Distance in local units to target at which the piece will automatically snap into place.")]
    public float snapDistance = 0.15f;
    
    private Plane _dragPlane;
    private Vector3 _dragOffsetLocal;
    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public void StartDragging(Ray ray)
    {
        if (piece == null || piece.IsLocked) return;

        if (_sr != null) _sr.sortingOrder = 20;

        // Plane in world space, facing away from the frame
        Vector3 boardNormal = manager.frameTransform.forward;
        _dragPlane = new Plane(-boardNormal, transform.position);

        if (_dragPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPointWorld = ray.GetPoint(dist);
            // Store offset in local space to keep it scale-independent
            _dragOffsetLocal = transform.localPosition - manager.frameTransform.InverseTransformPoint(hitPointWorld);
        }
    }

    public void FollowRay(Ray ray)
    {
        if (piece == null || piece.IsLocked) return;

        if (_dragPlane.Raycast(ray, out float dist))
        {
            Vector3 hitPointWorld = ray.GetPoint(dist);
            Vector3 hitPointLocal = manager.frameTransform.InverseTransformPoint(hitPointWorld);
            
            transform.localPosition = hitPointLocal + _dragOffsetLocal;

            // Smoothly align rotation to 0 (correct rotation in local space)
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    public void StopDragging()
    {
        if (piece == null || piece.IsLocked) return;

        if (_sr != null) _sr.sortingOrder = 5;

        TrySnapToBoard();
    }

    private void TrySnapToBoard()
    {
        Vector2 current = new Vector2(transform.localPosition.x, transform.localPosition.y);
        Vector2 target = new Vector2(piece.CorrectSlotLocal.x, piece.CorrectSlotLocal.y);
        float dist = Vector2.Distance(current, target);

        if (dist <= snapDistance)
        {
            SnapAndLock();
        }
    }

    public void SnapAndLock()
    {
        transform.localPosition = piece.CorrectSlotLocal;
        transform.localRotation = Quaternion.identity;
        piece.LockPiece();
        
        Debug.Log($"[PuzzleDragger] {gameObject.name} snapped and locked!");
    }
}
