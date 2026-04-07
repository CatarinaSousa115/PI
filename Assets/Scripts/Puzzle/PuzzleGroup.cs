using UnityEngine;
using System.Collections.Generic;

namespace Puzzle
{
    public class PuzzleGroup
    {
        public List<PuzzleDragger> members = new List<PuzzleDragger>();

        public PuzzleDragger anchor;

        public PuzzleGroup(PuzzleDragger first)
        {
            anchor = first;
            members.Add(first);
        }

        public void Add(PuzzleDragger dragger)
        {
            if (!members.Contains(dragger))
                members.Add(dragger);
        }

        public void Absorb(PuzzleGroup other)
        {
            foreach (var m in other.members)
            {
                if (!members.Contains(m))
                {
                    members.Add(m);
                    m.group = this;
                }
            }
        }

        public void MoveBy(Vector3 delta)
        {
            foreach (var m in members)
                m.transform.position += delta;
        }
    }
}