using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    [RequireComponent(typeof(PuzzlePiece))]
    public class PuzzleDragger : MonoBehaviour
    {
        [HideInInspector] public PuzzleManager manager;
        [HideInInspector] public PuzzleGroup group;

        public float snapDistance = 0.15f;
        public float liftAmount = 0.02f;

        private PuzzlePiece piece;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool isDragging = false;

        void Awake()
        {
            piece = GetComponent<PuzzlePiece>();
        }

        // ─── PICKUP ─────────────────────────────────────────────

        public void OnPickUp(Ray ray)
        {
            // Allow dragging if part of a group
            if (piece.isPlaced && group == null)
                return;

            isDragging = true;

            Vector3 boardNormal = manager.transform.forward;
            Vector3 liftedOrigin = transform.position - boardNormal * liftAmount;
            dragPlane = new Plane(-boardNormal, liftedOrigin);

            if (dragPlane.Raycast(ray, out float dist))
                dragOffset = transform.position - ray.GetPoint(dist);
        }

        // ─── DRAG ───────────────────────────────────────────────

        public void OnDrag(Ray ray)
        {
            if (!isDragging) return;

            if (dragPlane.Raycast(ray, out float dist))
            {
                Vector3 worldPos = ray.GetPoint(dist) + dragOffset;
                Vector3 localPos = manager.transform.InverseTransformPoint(worldPos);
                localPos.z = -liftAmount;

                Vector3 newPos = manager.transform.TransformPoint(localPos);
                Vector3 delta = newPos - transform.position;

                if (group != null)
                    group.MoveBy(delta);
                else
                    transform.position = newPos;
            }
        }

        // ─── RELEASE ────────────────────────────────────────────

        public void OnRelease()
        {
            if (!isDragging) return;
            isDragging = false;

            TrySnapToNeighbours(); 
            TrySnap();             
        }

        // ─── SNAP TO BOARD ──────────────────────────────────────

        void TrySnap()
        {
            if (group != null)
            {
                foreach (var member in group.members)
                    member.TrySnapSingle();
            }
            else
            {
                TrySnapSingle();
            }
        }

        public void TrySnapSingle()
        {
            if (piece.isPlaced) return;

            Vector3 correctWorld = manager.transform.TransformPoint(piece.correctLocalPosition);

            Vector3 localCurrent = manager.transform.InverseTransformPoint(transform.position);
            Vector3 localCorrect = manager.transform.InverseTransformPoint(correctWorld);

            localCurrent.z = 0f;
            localCorrect.z = 0f;

            float dist = Vector3.Distance(localCurrent, localCorrect);

            if (dist <= snapDistance)
            {
                transform.position = correctWorld;
                transform.rotation = manager.transform.rotation;

                piece.SetPlaced();
                manager.OnPiecePlaced();

                CheckNeighbours();
            }
            else
            {
                Vector3 localPos = manager.transform.InverseTransformPoint(transform.position);
                localPos.z = 0f;
                transform.position = manager.transform.TransformPoint(localPos);
            }
        }

        // ─── GROUP MERGING AFTER PLACED ─────────────────────────

        void CheckNeighbours()
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int( 1,  0),
                new Vector2Int(-1,  0),
                new Vector2Int( 0,  1),
                new Vector2Int( 0, -1),
            };

            foreach (var dir in directions)
            {
                int nx = piece.gridX + dir.x;
                int ny = piece.gridY + dir.y;

                PuzzleDragger neighbour = manager.GetPiece(nx, ny);

                if (neighbour == null) continue;
                if (!neighbour.piece.isPlaced) continue;

                MergeWith(neighbour);
            }
        }

        // ─── PIECE-TO-PIECE SNAP ────────────────────────────────

        void TrySnapToNeighbours()
        {
            Vector2Int[] directions = new Vector2Int[]
            {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
            };

            foreach (var dir in directions)
            {
                int nx = piece.gridX + dir.x;
                int ny = piece.gridY + dir.y;

                PuzzleDragger neighbour = manager.GetPiece(nx, ny);
                if (neighbour == null) continue;

                if (group != null && neighbour.group == group) continue;

                Vector3 myCorrectWorld =
                    manager.transform.TransformPoint(piece.correctLocalPosition);

                Vector3 neighbourCorrectWorld =
                    manager.transform.TransformPoint(neighbour.piece.correctLocalPosition);

                Vector3 targetWorldPos =
                    neighbour.transform.position - (neighbourCorrectWorld - myCorrectWorld);

                Vector3 delta = targetWorldPos - transform.position;
                delta.z = 0f;

                float dist = delta.magnitude;

                if (dist <= snapDistance)
                {
                    if (group != null)
                        group.MoveBy(delta);
                    else
                        transform.position += delta;

                    MergeWith(neighbour);
                    return;
                }
            }
        }

        // ─── GROUP MERGE LOGIC ──────────────────────────────────

        void MergeWith(PuzzleDragger neighbour)
        {
            if (group == null && neighbour.group == null)
            {
                PuzzleGroup newGroup = new PuzzleGroup(this);
                newGroup.Add(neighbour);

                group = newGroup;
                neighbour.group = newGroup;
            }
            else if (group == null)
            {
                neighbour.group.Add(this);
                group = neighbour.group;
            }
            else if (neighbour.group == null)
            {
                group.Add(neighbour);
                neighbour.group = group;
            }
            else if (group != neighbour.group)
            {
                group.Absorb(neighbour.group);
            }
        }
    }
}