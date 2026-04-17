using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Puzzle;

namespace Puzzle
{
    public class PuzzleInteractionTrigger : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("Interaction")]
        [Tooltip("Key the player presses to start/stop the puzzle.")]
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("Tag used to identify the player GameObject.")]
        public string playerTag = "Player";

        [Header("UI Prompt")]
        [Tooltip("Optional UI element shown when player is in range (e.g. 'Press E to inspect').")]
        public GameObject promptUI;

        [Tooltip("TextMeshPro label inside the prompt (optional).")]
        public TextMeshProUGUI promptText;

        [Tooltip("Text shown when player is near and puzzle is inactive.")]
        public string enterText = "Press [E] to inspect painting";

        [Tooltip("Text shown when puzzle is active.")]
        public string exitText = "Press [E] to step back";

        [Header("References")]
        [Tooltip("The PuzzleManager in the scene. Auto-found if left empty.")]
        public PuzzleManager puzzleManager;

        [Tooltip("The PuzzleCameraController in the scene. Auto-found if left empty.")]
        public PuzzleCameraController cameraController;

        [Header("Player Control")]
        [Tooltip("Script(s) on the player to disable while puzzle is active (e.g. movement controller).")]
        public MonoBehaviour[] playerScriptsToDisable;

        // ─────────────────────────────────────────────
        // Private State
        // ─────────────────────────────────────────────

        private bool _playerInRange;
        private bool _puzzleOpen;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            if (puzzleManager == null) puzzleManager = FindObjectOfType<PuzzleManager>();
            if (cameraController == null) cameraController = FindObjectOfType<PuzzleCameraController>();

            SetPromptVisible(false);

            // Make sure there IS a trigger collider
            var col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogWarning("[PuzzleInteractionTrigger] No Collider found on this GameObject. " +
                                 "Add a trigger Collider (e.g. SphereCollider, Is Trigger = true).");
            }
        }

        private void Update()
        {
            if (!_playerInRange) return;

            if (Input.GetKeyDown(interactKey))
            {
                if (!_puzzleOpen)
                    OpenPuzzle();
                else
                    ClosePuzzle();
            }
        }

        // ─────────────────────────────────────────────
        // Trigger Detection
        // ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInRange = true;
            if (!_puzzleOpen)
                ShowPrompt(enterText);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInRange = false;
            SetPromptVisible(false);

            // Auto-close if player walks away
            if (_puzzleOpen)
                ClosePuzzle();
        }

        // ─────────────────────────────────────────────
        // Open / Close
        // ─────────────────────────────────────────────

        private void OpenPuzzle()
        {
            _puzzleOpen = true;

            // Transition camera
            cameraController?.TransitionToPuzzleView();

            // Start puzzle logic
            puzzleManager?.StartPuzzle();

            // Disable player movement
            SetPlayerScripts(false);

            ShowPrompt(exitText);

            // Lock & hide cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ClosePuzzle()
        {
            _puzzleOpen = false;

            // Return camera to player
            cameraController?.TransitionToPlayerView();

            // Re-enable player movement
            SetPlayerScripts(true);

            if (_playerInRange)
                ShowPrompt(enterText);
            else
                SetPromptVisible(false);

            // NOTE: we do NOT reset the puzzle here — progress is preserved.
            // Call puzzleManager.ResetPuzzle() explicitly if you want a fresh start.
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        private void ShowPrompt(string text)
        {
            if (promptText != null) promptText.text = text;
            SetPromptVisible(true);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptUI != null) promptUI.SetActive(visible);
        }

        private void SetPlayerScripts(bool enabled)
        {
            foreach (var s in playerScriptsToDisable)
                if (s != null) s.enabled = enabled;
        }

        // ─────────────────────────────────────────────
        // Public: close from external code (e.g. puzzle completion)
        // ─────────────────────────────────────────────

        public void ForceClose()
        {
            if (_puzzleOpen) ClosePuzzle();
        }

        public bool IsPuzzleOpen => _puzzleOpen;
    }
}