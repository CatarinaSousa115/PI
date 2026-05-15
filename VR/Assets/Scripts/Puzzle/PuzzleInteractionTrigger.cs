using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;

public class PuzzleInteractionTrigger : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public InputActionProperty interactAction;

    [Header("UI Prompt")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;
    public string enterText = "Press Trigger to inspect painting";
    public string exitText = "Press Trigger to step back";

    [Header("Scene References")]
    public PuzzleManager puzzleManager;
    public PuzzleCameraController cameraController;

    [Header("Player — Movement Lock")]
    public MonoBehaviour[] playerScriptsToDisable;
    public CharacterController playerCharacterController;
    public Rigidbody playerRigidbody;

    [Header("Player — Visibility")]
    public GameObject playerModelRoot;
    public bool hideShadowToo = true;

    private bool _playerInRange;
    private bool _puzzleOpen;
    private RigidbodyConstraints _savedConstraints;
    private bool _savedKinematic;
    private Renderer[] _playerRenderers;

    private void Start()
    {
        if (puzzleManager == null) puzzleManager = FindObjectOfType<PuzzleManager>();
        if (cameraController == null) cameraController = FindObjectOfType<PuzzleCameraController>();

        SetPromptVisible(false);

        if (playerModelRoot != null)
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    private void Update()
    {
        if (!_playerInRange)
            return;

        bool interactPressed = interactAction.action != null && interactAction.action.WasPressedThisFrame();

        if (interactPressed)
        {
            Debug.Log($"[PuzzleTrigger] Interact pressed. Open: {!_puzzleOpen}");

            if (!_puzzleOpen)
                OpenPuzzle();
            else
                ClosePuzzle();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = true;
        if (!_puzzleOpen) ShowPrompt(enterText);

        AutoDiscoverPlayerComponents(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerInRange = false;
        SetPromptVisible(false);
        if (_puzzleOpen) ClosePuzzle();
    }

    private void OpenPuzzle()
    {
        _puzzleOpen = true;

        cameraController?.TransitionToPuzzleView();
        DisablePlayerMovement();
        SetPlayerVisible(false);

        puzzleManager?.EnterPuzzleMode();

        ShowPrompt(exitText);
    }

    private void ClosePuzzle()
    {
        _puzzleOpen = false;

        cameraController?.TransitionToPlayerView();
        EnablePlayerMovement();
        SetPlayerVisible(true);

        puzzleManager?.ExitPuzzleMode();

        if (_playerInRange) ShowPrompt(enterText);
        else SetPromptVisible(false);
    }

    private void DisablePlayerMovement()
    {
        foreach (var s in playerScriptsToDisable)
            if (s != null) s.enabled = false;

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerRigidbody != null)
        {
            _savedConstraints = playerRigidbody.constraints;
            _savedKinematic = playerRigidbody.isKinematic;
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

    private void SetPlayerVisible(bool visible)
    {
        if (_playerRenderers == null) return;
        foreach (var r in _playerRenderers)
        {
            if (r == null) continue;
            r.enabled = visible;
        }
    }

    private void AutoDiscoverPlayerComponents(GameObject playerGO)
    {
        if (playerCharacterController == null)
            playerCharacterController = playerGO.GetComponentInChildren<CharacterController>();

        if (playerRigidbody == null)
            playerRigidbody = playerGO.GetComponentInChildren<Rigidbody>();

        if (playerModelRoot == null)
        {
            playerModelRoot = playerGO;
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        if (playerScriptsToDisable == null || playerScriptsToDisable.Length == 0)
        {
            var found = new System.Collections.Generic.List<MonoBehaviour>();
            foreach (var mb in playerGO.GetComponentsInChildren<MonoBehaviour>())
            {
                string typeName = mb.GetType().Name.ToLower();
                if (typeName.Contains("move") || typeName.Contains("walk") ||
                    typeName.Contains("look") || typeName.Contains("rotate") ||
                    typeName.Contains("input") || typeName.Contains("control") ||
                    typeName.Contains("jump") || typeName.Contains("player"))
                {
                    found.Add(mb);
                }
            }
            playerScriptsToDisable = found.ToArray();
        }
    }

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