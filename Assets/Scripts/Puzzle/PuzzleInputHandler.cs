using UnityEngine;

namespace Puzzle
{
    // Attach to the same GameObject as PuzzleManager.
    // Handles all mouse/touch input for the puzzle independently
    // of whatever input system the rest of your game uses.
    public class PuzzleInputHandler : MonoBehaviour
    {
        [Header("Settings")]
        public LayerMask puzzleLayer;           // assign "Puzzle" layer in Inspector
        public Camera puzzleCamera;             // assign your camera, or leave null for Camera.main

        private Camera cam;
        private PuzzleDragger currentDragger;

        void Start()
        {
            cam = puzzleCamera != null ? puzzleCamera : Camera.main;
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

        PuzzleDragger lastHovered;

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
            if (Physics.Raycast(GetRay(), out RaycastHit hit, 100f, puzzleLayer))
                return hit.collider.GetComponent<PuzzleDragger>();
            return null;
        }

        Ray GetRay() => cam.ScreenPointToRay(Input.mousePosition);
    }
}