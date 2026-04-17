using System.Collections;
using UnityEngine;

namespace Puzzle
{

    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class PuzzlePiece : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("Snap Settings")]
        [Tooltip("How close (world units) the piece must be to its slot before auto-snapping.")]
        public float snapThreshold = 0.25f;

        [Tooltip("Speed of the smooth snap animation.")]
        public float snapSpeed = 12f;

        [Tooltip("Speed of the smooth rotation-to-zero animation when snapping.")]
        public float snapRotSpeed = 15f;

        [Header("Drag Settings")]
        [Tooltip("Z position (world) while dragging. Keep slightly in front of frame.")]
        public float dragZ = -0.1f;

        [Tooltip("How much the piece scales up while held.")]
        public float dragScaleMultiplier = 1.08f;

        [Header("Visual Feedback")]
        [Tooltip("Color tint when hovering over the correct slot.")]
        public Color nearCorrectColor = new Color(0.6f, 1f, 0.6f);

        [Tooltip("Color when locked in place.")]
        public Color lockedColor = Color.white;

        [Tooltip("Sort order while dragging (should be above other pieces).")]
        public int dragSortOrder = 10;

        // ─────────────────────────────────────────────
        // Public State
        // ─────────────────────────────────────────────

        /// <summary>Grid slot [column, row] this piece belongs to.</summary>
        public Vector2Int CorrectSlot { get; private set; }

        /// <summary>True once this piece is locked into its slot.</summary>
        public bool IsLocked { get; private set; }

        // ─────────────────────────────────────────────
        // Private
        // ─────────────────────────────────────────────

        private SpriteRenderer _sr;
        private Collider2D _col;
        private Camera _puzzleCam;

        private Vector3 _correctWorldPos;
        private Vector2 _pieceSize;

        private bool _isDragging;
        private Vector3 _dragOffset;
        private int _defaultSortOrder;
        private Vector3 _defaultScale;

        // For snapping animation
        private bool _isSnapping;
        private Vector3 _snapTarget;

        // Plane in world space representing the frame's surface (used for accurate dragging)
        private Plane _dragPlane;

        // ─────────────────────────────────────────────
        // Initialisation (called by PuzzleManager)
        // ─────────────────────────────────────────────

        public void Initialise(Vector2Int slot, Vector3 correctWorldPos, Vector2 pieceSize)
        {
            CorrectSlot = slot;
            _correctWorldPos = correctWorldPos;
            _pieceSize = pieceSize;
        }

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _col = GetComponent<Collider2D>();
            _defaultSortOrder = _sr.sortingOrder;
            _defaultScale = transform.localScale;
        }

        private void Start()
        {
            // Cache puzzle camera (tagged "PuzzleCamera" or fall back to main)
            var camObj = GameObject.FindWithTag("PuzzleCamera");
            _puzzleCam = camObj != null ? camObj.GetComponent<Camera>() : Camera.main;

            // Build drag plane aligned to the frame's forward direction
            if (PuzzleManager.Instance != null && PuzzleManager.Instance.frameTransform != null)
                _dragPlane = new Plane(PuzzleManager.Instance.frameTransform.forward,
                                       PuzzleManager.Instance.frameTransform.position);
            else
                _dragPlane = new Plane(Vector3.back, transform.position);
        }

        private void Update()
        {
            if (IsLocked) return;

            if (_isDragging)
                HandleDrag();

            if (_isSnapping)
                AnimateSnap();

            if (_isDragging)
                CheckProximityHighlight();
        }

        // ─────────────────────────────────────────────
        // Mouse / Pointer Events
        // ─────────────────────────────────────────────

        private void OnMouseDown()
        {
            if (IsLocked || !PuzzleManager.Instance.IsActive) return;

            _isDragging = true;
            _isSnapping = false;

            // Compute offset so piece doesn't jump to cursor centre
            Vector3 worldClick = GetMouseWorldPos();
            _dragOffset = transform.position - worldClick;

            // Visual feedback
            _sr.sortingOrder = dragSortOrder;
            transform.localScale = _defaultScale * dragScaleMultiplier;
        }

        private void OnMouseUp()
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Reset visuals
            _sr.sortingOrder = _defaultSortOrder;
            transform.localScale = _defaultScale;
            _sr.color = Color.white;

            TrySnap();
        }

        // ─────────────────────────────────────────────
        // Drag
        // ─────────────────────────────────────────────

        private void HandleDrag()
        {
            Vector3 target = GetMouseWorldPos() + _dragOffset;
            target.z = PuzzleManager.Instance.frameTransform.position.z + dragZ;
            transform.position = target;

            // Gradually straighten rotation while dragging
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.identity,
                Time.deltaTime * 8f
            );
        }

        private Vector3 GetMouseWorldPos()
        {
            Ray ray = _puzzleCam.ScreenPointToRay(Input.mousePosition);
            float enter;
            if (_dragPlane.Raycast(ray, out enter))
                return ray.GetPoint(enter);

            // Fallback
            Vector3 screen = Input.mousePosition;
            screen.z = Mathf.Abs(_puzzleCam.transform.position.z + dragZ);
            return _puzzleCam.ScreenToWorldPoint(screen);
        }

        // ─────────────────────────────────────────────
        // Snap Logic
        // ─────────────────────────────────────────────

        private void TrySnap()
        {
            if (PuzzleManager.Instance == null) return;

            float dist;
            Vector2Int nearest = PuzzleManager.Instance.GetNearestSlot(transform.position, out dist);

            bool isCorrectSlot = nearest == CorrectSlot;
            bool slotFree = !PuzzleManager.Instance.SlotOccupied[nearest.x, nearest.y];
            bool closeEnough = dist <= snapThreshold;

            if (closeEnough && isCorrectSlot && slotFree)
            {
                // Snap to correct slot → lock
                BeginSnapTo(_correctWorldPos, lockOnArrive: true);
            }
            else if (closeEnough && slotFree)
            {
                // Snap to nearest free slot temporarily (wrong slot)
                BeginSnapTo(PuzzleManager.Instance.GetSlotWorldPosition(nearest), lockOnArrive: false);
            }
            // else: leave piece where it is
        }

        private void BeginSnapTo(Vector3 targetPos, bool lockOnArrive)
        {
            _snapTarget = targetPos;
            _isSnapping = true;
            _lockOnArrive = lockOnArrive;
        }

        private bool _lockOnArrive;

        private void AnimateSnap()
        {
            transform.position = Vector3.Lerp(transform.position, _snapTarget, Time.deltaTime * snapSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * snapRotSpeed);

            if (Vector3.Distance(transform.position, _snapTarget) < 0.002f)
            {
                transform.position = _snapTarget;
                transform.rotation = Quaternion.identity;
                _isSnapping = false;

                if (_lockOnArrive)
                    LockPiece();
            }
        }

        private void LockPiece()
        {
            IsLocked = true;
            _sr.color = lockedColor;

            // Disable collider so it can't be clicked again
            _col.enabled = false;

            // Move to exact Z of frame
            Vector3 p = transform.position;
            p.z = PuzzleManager.Instance.frameTransform.position.z;
            transform.position = p;

            // Notify manager
            PuzzleManager.Instance.NotifyPieceLocked(this);
        }

        // ─────────────────────────────────────────────
        // Proximity Highlight
        // ─────────────────────────────────────────────

        private void CheckProximityHighlight()
        {
            if (PuzzleManager.Instance == null) return;

            float dist;
            Vector2Int nearest = PuzzleManager.Instance.GetNearestSlot(transform.position, out dist);

            bool isCorrect = nearest == CorrectSlot;
            bool isClose = dist <= snapThreshold * 1.5f;

            _sr.color = (isClose && isCorrect) ? nearCorrectColor : Color.white;
        }

        // ─────────────────────────────────────────────
        // Public: Force-lock at correct position (e.g. cheat / debug)
        // ─────────────────────────────────────────────

        public void ForceComplete()
        {
            if (IsLocked) return;
            transform.position = _correctWorldPos;
            transform.rotation = Quaternion.identity;
            LockPiece();
        }
    }
}