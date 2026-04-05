using UnityEngine;

namespace Puzzle
{
    // Attach to the preview panel GameObject.
    // Press P (or call Toggle() from anywhere in your game) to show/hide
    // the reference image of the full painting.
    public class PuzzlePreview : MonoBehaviour
    {
        public KeyCode toggleKey = KeyCode.P;
        public GameObject previewPanel;

        private bool visible = false;

        void Start()
        {
            if (previewPanel) previewPanel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
                Toggle();
        }

        public void Toggle()
        {
            visible = !visible;
            if (previewPanel) previewPanel.SetActive(visible);
        }
    }
}