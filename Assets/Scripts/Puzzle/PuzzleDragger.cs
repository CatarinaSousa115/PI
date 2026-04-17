using System.Collections.Generic;
using UnityEngine;

public class PuzzleDragger : MonoBehaviour
{
    [HideInInspector] public PuzzleManager manager;
    [HideInInspector] public PuzzlePiece piece;
    [HideInInspector] public PuzzleGroup group;

    public float snapDistance = 0.25f;
    private Plane _dragPlane;
    private Vector3 _dragOffset;

    public void StartDragging(Ray ray)
    {
        if (piece.IsLocked) return;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 10;

        if (group != null)
        {
            foreach (var member in group.members)
            {
                var msr = member.GetComponent<SpriteRenderer>();
                if (msr != null) msr.sortingOrder = 10;
            }
        }

        Vector3 boardNormal = manager.frameTransform.forward;
        _dragPlane = new Plane(-boardNormal, transform.position);

        if (_dragPlane.Raycast(ray, out float dist))
            _dragOffset = transform.position - ray.GetPoint(dist);
    }

    public void FollowMouse(Ray ray)
    {
        if (_dragPlane.Raycast(ray, out float dist))
        {
            Vector3 targetPos = ray.GetPoint(dist) + _dragOffset;
            Vector3 delta = targetPos - transform.position;

            if (group != null) group.MoveBy(delta);
            else transform.position = targetPos;

            Quaternion targetRot = manager.frameTransform.rotation;
            if (group != null)
            {
                foreach (var member in group.members)
                    member.transform.rotation = Quaternion.Lerp(member.transform.rotation, targetRot, Time.deltaTime * 10f);
            }
            else transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    public void StopDragging()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 1;

        if (group != null)
        {
            foreach (var member in group.members)
            {
                var msr = member.GetComponent<SpriteRenderer>();
                if (msr != null) msr.sortingOrder = 1;
            }
        }

        TrySnapToNeighbors();
        TrySnapToBoard();
    }

    private void TrySnapToNeighbors()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            PuzzleDragger neighbor = manager.GetPiece(piece.CorrectSlot.x + dir.x, piece.CorrectSlot.y + dir.y);
            if (neighbor == null || (group != null && neighbor.group == group)) continue;

            // Check distance relative to correct positions
            float dist = Vector3.Distance(
                transform.position - piece.CorrectWorldPos,
                neighbor.transform.position - neighbor.piece.CorrectWorldPos
            );

            if (dist <= snapDistance)
            {
                Vector3 delta = (neighbor.transform.position - neighbor.piece.CorrectWorldPos) + piece.CorrectWorldPos - transform.position;

                if (group != null) group.MoveBy(delta);
                else transform.position += delta;

                MergeGroups(neighbor);
                return; // Stop checking after one snap to prevent chained shifting errors
            }
        }
    }

    private void TrySnapToBoard()
    {
        float dist = Vector3.Distance(transform.position, piece.CorrectWorldPos);

        if (dist <= snapDistance)
        {
            if (group != null)
            {
                foreach (var m in group.members) m.SnapAndLock();
            }
            else SnapAndLock();
        }
    }

    public void SnapAndLock()
    {
        transform.position = piece.CorrectWorldPos;
        transform.rotation = manager.frameTransform.rotation;
        piece.LockPiece();
    }

    private void MergeGroups(PuzzleDragger neighbor)
    {
        if (group == null && neighbor.group == null)
        {
            PuzzleGroup newG = new PuzzleGroup(this);
            newG.Add(neighbor);
        }
        else if (group == null) neighbor.group.Add(this);
        else if (neighbor.group == null) group.Add(neighbor);
        else if (group != neighbor.group) group.Absorb(neighbor.group);
    }
}

// ─── Group Helper Class ──────────────────────────────────────────

public class PuzzleGroup
{
    public List<PuzzleDragger> members = new List<PuzzleDragger>();

    public PuzzleGroup(PuzzleDragger firstMember)
    {
        Add(firstMember);
    }

    public void Add(PuzzleDragger dragger)
    {
        if (!members.Contains(dragger)) members.Add(dragger);
        dragger.group = this;
    }

    public void MoveBy(Vector3 delta)
    {
        foreach (var m in members) m.transform.position += delta;
    }

    public void Absorb(PuzzleGroup other)
    {
        foreach (var m in other.members) Add(m);
    }
}