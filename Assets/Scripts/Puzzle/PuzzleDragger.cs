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
            if (piece.isPlaced && group == null) return;

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
                // Iterate over a copy so CheckNeighbours doesn't mutate the list mid-loop
                foreach (var member in new List<PuzzleDragger>(group.members))
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
                // Drop flush to board surface
                Vector3 localPos = manager.transform.InverseTransformPoint(transform.position);
                localPos.z = 0f;
                transform.position = manager.transform.TransformPoint(localPos);
            }
        }

        // ─── GROUP MERGING AFTER PLACED ─────────────────────────

        void CheckNeighbours()
        {
            var directions = new Vector2Int[]
            {
                new Vector2Int( 1,  0),
                new Vector2Int(-1,  0),
                new Vector2Int( 0,  1),
                new Vector2Int( 0, -1),
            };

            foreach (var dir in directions)
            {
                PuzzleDragger neighbour = manager.GetPiece(piece.gridX + dir.x, piece.gridY + dir.y);
                if (neighbour == null) continue;
                if (!neighbour.piece.isPlaced) continue;
                MergeWith(neighbour);
            }
        }

        // ─── PIECE-TO-PIECE SNAP ────────────────────────────────

        void TrySnapToNeighbours()
        {
            var directions = new Vector2Int[]
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

                Vector3 myLocalPos = manager.transform.InverseTransformPoint(transform.position);
                Vector3 neighbourLocalPos = manager.transform.InverseTransformPoint(neighbour.transform.position);

                Vector3 expectedLocalPos = neighbourLocalPos
                    + (piece.correctLocalPosition - neighbour.piece.correctLocalPosition);

                Vector2 myXY = new Vector2(myLocalPos.x, myLocalPos.y);
                Vector2 expectedXY = new Vector2(expectedLocalPos.x, expectedLocalPos.y);

                float dist = Vector2.Distance(myXY, expectedXY);

                if (dist <= snapDistance)
                {
                    Vector3 targetLocalPos = expectedLocalPos;
                    targetLocalPos.z = myLocalPos.z; 
                    Vector3 targetWorldPos = manager.transform.TransformPoint(targetLocalPos);
                    Vector3 delta = targetWorldPos - transform.position;

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