using UnityEngine;
using TMPro;

/// <summary>
/// Attach to a trigger collider near the painting frame.
/// Player presses [E] to enter/exit puzzle mode.
///
/// FIX vs. v1:
///  - Now also freezes Rigidbody (velocity + constraints) and disables CharacterController,
///    so the player truly cannot move while the puzzle is open.
///  - Hides all Renderers on the player (and optionally their children) so the player
///    model does not appear in the puzzle camera view.
///  - Re-enables/shows everything correctly when puzzle closes.
/// </summary>
public class PuzzleInteractionTrigger : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public string playerTag = "Player";

    [Header("UI Prompt")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;
    public string enterText = "Press [E] to inspect painting";
    public string exitText = "Press [E] to step back";

    [Header("Scene References")]
    [Tooltip("Auto-found if left empty.")]
    public PuzzleManager puzzleManager;
    [Tooltip("Auto-found if left empty.")]
    public PuzzleCameraController cameraController;

    [Header("Player — Movement Lock")]
    [Tooltip("MonoBehaviour scripts on the player to disable (e.g. movement, look, interaction).")]
    public MonoBehaviour[] playerScriptsToDisable;

    [Tooltip("If your player uses a CharacterController, assign it here so it's disabled too.")]
    public CharacterController playerCharacterController;

    [Tooltip("If your player uses a Rigidbody, assign it here so it's frozen.")]
    public Rigidbody playerRigidbody;

    [Header("Player — Visibility")]
    [Tooltip("Root of the player's visible model. All Renderer components on it (and children) " +
             "will be hidden during puzzle mode so the model doesn't appear in the puzzle view.")]
    public GameObject playerModelRoot;

    [Tooltip("If true, also hides the player's shadow (sets shadowCastingMode to Off).")]
    public bool hideShadowToo = true;

    // ─────────────────────────────────────────────
    // Private State
    // ─────────────────────────────────────────────

    private bool _playerInRange;
    private bool _puzzleOpen;

    // Cached rigidbody state so we can restore it exactly
    private RigidbodyConstraints _savedConstraints;
    private bool _savedKinematic;

    // Cached renderer states
    private Renderer[] _playerRenderers;

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Start()
    {
        if (puzzleManager == null) puzzleManager = FindObjectOfType<PuzzleManager>();
        if (cameraController == null) cameraController = FindObjectOfType<PuzzleCameraController>();

        SetPromptVisible(false);

        // Cache player renderers once so we don't call GetComponentsInChildren every frame
        if (playerModelRoot != null)
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);

        // Validate trigger collider
        if (GetComponent<Collider>() == null)
            Debug.LogWarning("[PuzzleTrigger] No Collider found. Add one with Is Trigger = true.");
    }

    private void Update()
    {
        if (!_playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!_puzzleOpen) OpenPuzzle();
            else ClosePuzzle();
        }
    }

    // ─────────────────────────────────────────────
    // Trigger Detection
    // ─────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = true;
        if (!_puzzleOpen) ShowPrompt(enterText);

        // Auto-discover components on the player if not assigned in Inspector
        AutoDiscoverPlayerComponents(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = false;
        SetPromptVisible(false);
        if (_puzzleOpen) ClosePuzzle();
    }

    // ─────────────────────────────────────────────
    // Open / Close
    // ─────────────────────────────────────────────

    private void OpenPuzzle()
    {
        _puzzleOpen = true;

        // 1. Transition camera first
        cameraController?.TransitionToPuzzleView();

        // 2. Lock player movement
        DisablePlayerMovement();

        // 3. Hide player model
        SetPlayerVisible(false);

        // 4. Show cursor for clicking puzzle pieces
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 5. Start puzzle logic
        puzzleManager?.EnterPuzzleMode();

        ShowPrompt(exitText);
    }

    private void ClosePuzzle()
    {
        _puzzleOpen = false;

        // 1. Return camera
        cameraController?.TransitionToPlayerView();

        // 2. Re-enable player movement
        EnablePlayerMovement();

        // 3. Show player model again
        SetPlayerVisible(true);

        // 4. Restore cursor (lock it again if your game uses mouse-look)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_playerInRange) ShowPrompt(enterText);
        else SetPromptVisible(false);
    }

    // ─────────────────────────────────────────────
    // Player Movement — disable
    // ─────────────────────────────────────────────

    private void DisablePlayerMovement()
    {
        // MonoBehaviours (movement, look, etc.)
        foreach (var s in playerScriptsToDisable)
            if (s != null) s.enabled = false;

        // CharacterController — disabling it prevents all movement
        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        // Rigidbody — freeze position & rotation and make kinematic
        if (playerRigidbody != null)
        {
            _savedConstraints = playerRigidbody.constraints;
            _savedKinematic = playerRigidbody.isKinematic;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void EnablePlayerMovement()
    {
        foreach (var s in playerScriptsToDisable)
            if (s != null) s.enabled = true;

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = _savedKinematic;
            playerRigidbody.constraints = _savedConstraints;
        }
    }

    // ─────────────────────────────────────────────
    // Player Visibility
    // ─────────────────────────────────────────────

    private void SetPlayerVisible(bool visible)
    {
        if (_playerRenderers == null) return;

        foreach (var r in _playerRenderers)
        {
            if (r == null) continue;
            r.enabled = visible;

            if (hideShadowToo)
            {
                r.shadowCastingMode = visible
                    ? UnityEngine.Rendering.ShadowCastingMode.On
                    : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Auto-Discover (fallback if Inspector fields empty)
    // ─────────────────────────────────────────────

    private void AutoDiscoverPlayerComponents(GameObject playerGO)
    {
        if (playerCharacterController == null)
            playerCharacterController = playerGO.GetComponentInChildren<CharacterController>();

        if (playerRigidbody == null)
            playerRigidbody = playerGO.GetComponentInChildren<Rigidbody>();

        if (playerModelRoot == null)
        {
            // Assume the player root IS the model root if not specified
            playerModelRoot = playerGO;
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        if (playerScriptsToDisable == null || playerScriptsToDisable.Length == 0)
        {
            // Try to find common movement script types automatically.
            // Add your own script type names here if needed.
            var found = new System.Collections.Generic.List<MonoBehaviour>();

            // Unity's built-in character controller helper scripts don't have a base type,
            // so we grab ALL MonoBehaviours and filter out our own known-safe ones.
            foreach (var mb in playerGO.GetComponentsInChildren<MonoBehaviour>())
            {
                string typeName = mb.GetType().Name.ToLower();
                // Heuristic: enable/disable scripts whose names suggest movement or input
                if (typeName.Contains("move") || typeName.Contains("walk") ||
                    typeName.Contains("look") || typeName.Contains("rotate") ||
                    typeName.Contains("input") || typeName.Contains("control") ||
                    typeName.Contains("jump") || typeName.Contains("player"))
                {
                    found.Add(mb);
                }
            }

            if (found.Count > 0)
            {
                playerScriptsToDisable = found.ToArray();
                Debug.Log($"[PuzzleTrigger] Auto-discovered {found.Count} player scripts to disable.");
            }
            else
            {
                Debug.LogWarning("[PuzzleTrigger] Could not auto-discover player movement scripts. " +
                                 "Assign them manually in the Inspector.");
            }
        }
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void ShowPrompt(string text)
    {
        if (promptText != null) promptText.text = text;
        SetPromptVisible(true);
    }

    private void SetPromptVisible(bool v)
    {
        if (promptUI != null) promptUI.SetActive(v);
    }

    // ─────────────────────────────────────────────
    // Public
    // ─────────────────────────────────────────────

    public void ForceClose()
    {
        if (_puzzleOpen) ClosePuzzle();
    }

    public bool IsPuzzleOpen => _puzzleOpen;
}