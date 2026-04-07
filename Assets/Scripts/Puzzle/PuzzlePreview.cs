using UnityEngine;

namespace Puzzle
{
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