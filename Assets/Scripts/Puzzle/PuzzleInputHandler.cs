using UnityEngine;

namespace Puzzle
{
    public class PuzzleInputHandler : MonoBehaviour
    {
        public Camera puzzleCamera;

        private Camera cam;
        private PuzzleDragger currentDragger;
        private PuzzleDragger lastHovered;

        void Start()
        {
            cam = puzzleCamera != null ? puzzleCamera : Camera.main;

            if (cam == null)
                Debug.LogError("[PuzzleInput] No camera found!");
            else
                Debug.Log($"[PuzzleInput] Camera ready: {cam.name}");
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) HandlePress();
            if (Input.GetMouseButton(0)) HandleDrag();
            if (Input.GetMouseButtonUp(0)) HandleRelease();
            HandleHover();
        }

        void HandlePress()
        {
            PuzzleDragger dragger = RaycastDragger();
            if (dragger == null) return;

            currentDragger = dragger;
            currentDragger.OnPickUp(GetRay());
        }

        void HandleDrag()
        {
            currentDragger?.OnDrag(GetRay());
        }

        void HandleRelease()
        {
            currentDragger?.OnRelease();
            currentDragger = null;
        }

        void HandleHover()
        {
            PuzzleDragger dragger = RaycastDragger();

            if (dragger != lastHovered)
            {
                lastHovered?.GetComponent<PuzzlePiece>().SetHighlight(false);
                dragger?.GetComponent<PuzzlePiece>().SetHighlight(true);
                lastHovered = dragger;
            }
        }

        PuzzleDragger RaycastDragger()
        {
            if (Physics.Raycast(GetRay(), out RaycastHit hit, 100f))
            {
                return hit.collider.GetComponent<PuzzleDragger>();
            }
            return null;
        }

        Ray GetRay() => cam.ScreenPointToRay(Input.mousePosition);
    }
}