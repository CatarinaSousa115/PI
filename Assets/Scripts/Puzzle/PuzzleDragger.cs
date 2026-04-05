using UnityEngine;

namespace Puzzle
{
    [RequireComponent(typeof(PuzzlePiece))]
    public class PuzzleDragger : MonoBehaviour
    {
        [HideInInspector] public PuzzleManager manager;

        public float snapDistance = 0.06f;
        public float liftAmount = 0.05f;  // how far piece lifts off the board when dragged

        private PuzzlePiece piece;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool isDragging = false;

        void Awake()
        {
            piece = GetComponent<PuzzlePiece>();
        }

        // Called by PuzzleInputHandler
        public void OnPickUp(Ray ray)
        {
            if (piece.isPlaced) return;

            isDragging = true;

            // The drag plane is parallel to the board surface, at the piece's height
            // Use the board's up direction so the puzzle works at any orientation
            Vector3 boardUp = manager.transform.up;
            dragPlane = new Plane(boardUp, transform.position);

            // Calculate offset so piece doesn't snap to cursor center
            if (dragPlane.Raycast(ray, out float dist))
                dragOffset = transform.position - ray.GetPoint(dist);

            // Lift piece slightly off the board
            transform.position += boardUp * liftAmount;
        }

        public void OnDrag(Ray ray)
        {
            if (!isDragging) return;

            if (dragPlane.Raycast(ray, out float dist))
            {
                Vector3 target = ray.GetPoint(dist) + dragOffset;

                // Keep the lift — project target onto the lifted plane
                target += manager.transform.up * liftAmount;
                target -= manager.transform.up * liftAmount; // compensated by offset below
                transform.position = ray.GetPoint(dist) + dragOffset + manager.transform.up * liftAmount;
            }
        }

        public void OnRelease()
        {
            if (!isDragging) return;
            isDragging = false;

            TrySnap();
        }

        void TrySnap()
        {
            // Convert correct position from board-local to world space
            Vector3 correctWorld = manager.transform.TransformPoint(piece.correctLocalPosition);

            // Compare only position (ignore lift axis)
            Vector3 flatCurrent = Vector3.ProjectOnPlane(transform.position, manager.transform.up);
            Vector3 flatCorrect = Vector3.ProjectOnPlane(correctWorld, manager.transform.up);

            float dist = Vector3.Distance(flatCurrent, flatCorrect);

            if (dist <= snapDistance)
            {
                transform.position = correctWorld;
                transform.rotation = manager.transform.rotation;
                piece.SetPlaced();
                manager.OnPiecePlaced();
            }
            else
            {
                // Drop back onto board surface
                Vector3 onBoard = Vector3.ProjectOnPlane(transform.position, manager.transform.up);
                // Restore to board plane
                float boardOffset = Vector3.Dot(manager.transform.position, manager.transform.up);
                transform.position = onBoard + manager.transform.up * boardOffset;
            }
        }
    }
}   