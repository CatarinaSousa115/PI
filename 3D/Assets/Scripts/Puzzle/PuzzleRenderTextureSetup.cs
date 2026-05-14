using UnityEngine;

namespace Puzzle
{
    public class PuzzleRenderTextureSetup : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Dedicated camera that only sees puzzle pieces (Culling Mask = PuzzleLayer).")]
        public Camera puzzleCamera;

        [Tooltip("RenderTexture asset the puzzle camera renders into.")]
        public RenderTexture renderTexture;

        [Tooltip("Renderer of the frame quad/plane mesh on the wall.")]
        public Renderer frameRenderer;

        [Header("Material Settings")]
        [Tooltip("Name of the texture property on the frame's material.")]
        public string texturePropertyName = "_MainTex";

        [Tooltip("Material index on the frameRenderer to update (usually 0).")]
        public int materialIndex = 0;

        [Header("Puzzle Camera Framing")]
        [Tooltip("World-space size of the puzzle area (should match PuzzleManager.frameSize).")]
        public Vector2 puzzleAreaSize = new Vector2(2f, 2f);

        [Tooltip("The puzzle camera must be Orthographic. This sets its orthographic size automatically.")]
        public bool autoSetOrthographicSize = true;

        // ─────────────────────────────────────────────

        private void Start()
        {
            ApplyRenderTexture();
            FramePuzzleCamera();
        }

        private void ApplyRenderTexture()
        {
            if (frameRenderer == null || renderTexture == null) return;

            // Get or duplicate the material (avoid modifying shared assets)
            Material mat = frameRenderer.materials[materialIndex];
            mat = new Material(mat); // instance copy
            mat.SetTexture(texturePropertyName, renderTexture);

            // Apply back
            var mats = frameRenderer.materials;
            mats[materialIndex] = mat;
            frameRenderer.materials = mats;
        }

        private void FramePuzzleCamera()
        {
            if (puzzleCamera == null) return;

            puzzleCamera.orthographic = true;

            if (autoSetOrthographicSize)
            {
                // orthographicSize = half the vertical extent
                float aspectRatio = (renderTexture != null)
                    ? (float)renderTexture.width / renderTexture.height
                    : puzzleCamera.aspect;

                float halfH = puzzleAreaSize.y / 2f;
                puzzleCamera.orthographicSize = halfH;
            }

            // Position puzzle camera directly facing the puzzle area (local Z-axis of frame)
            if (PuzzleManager.Instance != null && PuzzleManager.Instance.frameTransform != null)
            {
                Transform frame = PuzzleManager.Instance.frameTransform;
                puzzleCamera.transform.position = frame.position - frame.forward * 0.5f;
                puzzleCamera.transform.rotation = Quaternion.LookRotation(frame.forward);
            }
        }

        // ─────────────────────────────────────────────
        // Called externally when puzzle starts (optional hook)
        // ─────────────────────────────────────────────

        public void EnablePuzzleCamera(bool enable)
        {
            if (puzzleCamera != null)
                puzzleCamera.enabled = enable;
        }
    }
}
