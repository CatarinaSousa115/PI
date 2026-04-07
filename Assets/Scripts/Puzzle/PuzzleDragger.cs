using UnityEngine;

namespace Puzzle
{
    [RequireComponent(typeof(PuzzlePiece))]
    public class PuzzleDragger : MonoBehaviour
    {
        [HideInInspector] public PuzzleManager manager;
        [HideInInspector] public PuzzleGroup group;

        public float snapDistance = 0.1f;
        public float liftAmount = 0.02f;

        private PuzzlePiece piece;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool isDragging = false;

        void Awake()
        {
            piece = GetComponent<PuzzlePiece>();
        }

        public void OnPickUp(Ray ray)
        {
            if (piece.isPlaced) return;

            isDragging = true;

            Vector3 boardNormal = manager.transform.forward;
            Vector3 liftedOrigin = transform.position - boardNormal * liftAmount;
            dragPlane = new Plane(-boardNormal, liftedOrigin);

            if (dragPlane.Raycast(ray, out float dist))
                dragOffset = transform.position - ray.GetPoint(dist);
        }

        public void OnDrag(Ray ray)
        {
            if (!isDragging) return;

            if (dragPlane.Raycast(ray, out float dist))
            {
                Vector3 worldPos = ray.GetPoint(dist) + dragOffset;
                Vector3 localPos = manager.transform.InverseTransformPoint(worldPos);
                localPos.z = -liftAmount;
                Vector3 newPos = manager.transform.TransformPoint(localPos);

                // Move the whole group by the same delta
                Vector3 delta = newPos - transform.position;

                if (group != null)
                    group.MoveBy(delta);
                else
                    transform.position = newPos;
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

                
                if (group == null && neighbour.group == null)
                {
                    PuzzleGroup newGroup = new PuzzleGroup(neighbour);
                    newGroup.Add(this);
                    group = newGroup;
                    neighbour.group = newGroup;
                }
                else if (group == null && neighbour.group != null)
                {
                    neighbour.group.Add(this);
                    group = neighbour.group;
                }
                else if (group != null && neighbour.group == null)
                {
                    group.Add(neighbour);
                    neighbour.group = group;
                }
                else if (group != null && neighbour.group != null && group != neighbour.group)
                {
                    group.Absorb(neighbour.group);
                }
            }
        }
    }
}