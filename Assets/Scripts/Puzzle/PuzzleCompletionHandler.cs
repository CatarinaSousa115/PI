using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Puzzle
{


    public class PuzzleCompletionHandler : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("Frame Glow")]
        [Tooltip("Renderer of the frame mesh to add emission glow to.")]
        public Renderer frameRenderer;

        [Tooltip("Emission colour for the glow effect.")]
        public Color glowColor = new Color(1f, 0.85f, 0.3f);

        [Tooltip("How bright the emission gets (HDR intensity multiplier).")]
        public float glowIntensity = 2f;

        [Tooltip("Duration of the glow fade-in.")]
        public float glowFadeDuration = 1.5f;

        [Header("Completion UI")]
        [Tooltip("Root panel shown on completion.")]
        public GameObject completionPanel;

        [Tooltip("Label inside the panel.")]
        public TextMeshProUGUI completionLabel;

        [Tooltip("Text to display.")]
        public string completionText = "Painting Restored!";

        [Tooltip("How long the panel stays visible before fading.")]
        public float panelDisplayDuration = 3f;

        [Tooltip("Canvas group used to fade the panel.")]
        public CanvasGroup panelCanvasGroup;

        [Header("World Event")]
        [Tooltip("Optional: A door or mechanism Animator to trigger on completion.")]
        public Animator worldEventAnimator;

        [Tooltip("Animator trigger name to fire.")]
        public string worldEventTrigger = "Unlock";

        [Tooltip("Delay before firing the world event (seconds).")]
        public float worldEventDelay = 1.5f;

        [Header("Interaction")]
        [Tooltip("Reference to the trigger so we can close puzzle view on completion.")]
        public PuzzleInteractionTrigger interactionTrigger;

        [Tooltip("Delay before auto-closing the puzzle view after completion.")]
        public float autoCloseDelay = 2.5f;

        [Header("Piece Display")]
        [Tooltip("Objeto PiecesContainer que tem o script pieceDisplay.")]
        public PieceDisplay pieceDisplay;

        [Tooltip("Índice da peça a desbloquear (0-25).")]
        public int pieceIndexToUnlock = 5;

        // ─────────────────────────────────────────────
        // Public: called by PuzzleManager.onPuzzleComplete
        // ─────────────────────────────────────────────

        public void OnPuzzleComplete()
        {
            StartCoroutine(CompletionSequence());
        }

        // ─────────────────────────────────────────────
        // Sequence
        // ─────────────────────────────────────────────

        private IEnumerator CompletionSequence()
        {
            // Desbloqueia a peça na parede do museu
            if (pieceDisplay != null)
                pieceDisplay.UnlockPiece(pieceIndexToUnlock);

            // 1. Glow effect
            if (frameRenderer != null)
                StartCoroutine(GlowFrame());

            // 2. Show UI
            if (completionPanel != null)
                StartCoroutine(ShowCompletionUI());

            // 3. World event
            if (worldEventAnimator != null)
            {
                yield return new WaitForSeconds(worldEventDelay);
                worldEventAnimator.SetTrigger(worldEventTrigger);
            }

            // 4. Auto-close puzzle
            yield return new WaitForSeconds(autoCloseDelay);
            interactionTrigger?.ForceClose();
        }

        // ─────────────────────────────────────────────
        // Glow
        // ─────────────────────────────────────────────

        private IEnumerator GlowFrame()
        {
            Material mat = frameRenderer.material; // instance
            mat.EnableKeyword("_EMISSION");

            float elapsed = 0f;
            while (elapsed < glowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / glowFadeDuration);
                Color emission = glowColor * (glowIntensity * t);
                mat.SetColor("_EmissionColor", emission);
                yield return null;
            }

            // Pulse a few times
            for (int i = 0; i < 3; i++)
            {
                yield return PulseGlow(glowIntensity, glowIntensity * 0.3f, 0.4f);
                yield return PulseGlow(glowIntensity * 0.3f, glowIntensity, 0.4f);
            }
        }

        private IEnumerator PulseGlow(float from, float to, float duration)
        {
            Material mat = frameRenderer.material;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Color emission = glowColor * Mathf.Lerp(from, to, t);
                mat.SetColor("_EmissionColor", emission);
                yield return null;
            }
        }

        // ─────────────────────────────────────────────
        // UI
        // ─────────────────────────────────────────────

        private IEnumerator ShowCompletionUI()
        {
            if (completionLabel != null) completionLabel.text = completionText;

            completionPanel.SetActive(true);

            // Fade in
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / 0.5f);
                    yield return null;
                }
                panelCanvasGroup.alpha = 1f;
            }

            yield return new WaitForSeconds(panelDisplayDuration);

            // Fade out
            if (panelCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    panelCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.5f);
                    yield return null;
                }
            }

            completionPanel.SetActive(false);
        }
    }
}