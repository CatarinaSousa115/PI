using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MuseumGame.UI;
using System.Collections.Generic;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;
using XRInputDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;

public class PuzzleInteractionTrigger : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Player";
    public InputActionProperty interactAction;
    public Key keyboardToggleKey = Key.H;
    public Key legacyKeyboardToggleKey = Key.G;
    public bool acceptLegacyKeyboardToggleKey = true;
    public Key keyboardCloseKey = Key.Escape;
    public float keyboardActivationDistance = 3f;
    public bool useXRControllerCloseButton = true;
    public bool preferRightControllerForClose = true;
    public bool useSecondaryButtonForToggle = true;
    public bool useMenuButtonForToggle = true;

    [Header("UI Prompt")]
    public GameObject promptUI;
    public TextMeshProUGUI promptText;
    public string enterText = "Press [H] / [G] or [B/Y] to inspect painting";
    public string exitText = "Press [H] / [G] or [B/Y] to step back";
    public bool autoCreatePromptUI = true;

    [Header("VR Prompt Placement")]
    public bool keepPromptInFrontOfCamera = true;
    public float promptDistance = 1f;
    public Vector2 promptOffset = new Vector2(0f, -0.32f);
    public float promptFollowSpeed = 10f;

    [Header("Scene References")]
    public PuzzleManager puzzleManager;
    public PuzzleCameraController cameraController;

    [Header("Player — Movement Lock")]
    public bool lockPlayerMovementWhilePuzzleOpen;
    public MonoBehaviour[] playerScriptsToDisable;
    public CharacterController playerCharacterController;
    public bool disableCharacterControllerWhenLocked;
    public Rigidbody playerRigidbody;
    public bool freezeRigidbodyWhenLocked;

    [Header("Player — Visibility")]
    public bool hidePlayerWhilePuzzleOpen;
    public GameObject playerModelRoot;
    public bool hideShadowToo = true;

    private bool _playerInRange;
    private bool _puzzleOpen;
    private RigidbodyConstraints _savedConstraints;
    private bool _savedKinematic;
    private bool _savedCharacterControllerEnabled;
    private bool _characterControllerStateSaved;
    private bool _rigidbodyStateSaved;
    private Renderer[] _playerRenderers;
    private Transform _playerTransform;
    private bool _xrCloseWasPressedLastFrame;
    private Canvas _promptCanvas;
    private readonly Dictionary<MonoBehaviour, bool> _disabledScriptStates = new();
    private readonly List<XRInputDevice> _xrDevices = new();

    private void Start()
    {
        if (puzzleManager == null) puzzleManager = FindFirstObjectByType<PuzzleManager>();
        if (cameraController == null) cameraController = FindFirstObjectByType<PuzzleCameraController>();

        EnsurePromptUI();
        SetPromptVisible(false);

        if (playerModelRoot != null)
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);

        ResolvePlayerReference();
    }

    private void Update()
    {
        bool interactPressed = interactAction.action != null && interactAction.action.WasPressedThisFrame();
        bool keyboardOpenPressed = Keyboard.current != null &&
                                   ((keyboardToggleKey != Key.None && Keyboard.current[keyboardToggleKey].wasPressedThisFrame) ||
                                    (acceptLegacyKeyboardToggleKey && legacyKeyboardToggleKey != Key.None && Keyboard.current[legacyKeyboardToggleKey].wasPressedThisFrame));
        bool keyboardClosePressed = keyboardCloseKey != Key.None &&
                                    Keyboard.current != null &&
                                    Keyboard.current[keyboardCloseKey].wasPressedThisFrame;
        bool xrButtonPressed = useXRControllerCloseButton && WasXRClosePressedThisFrame();

        bool canInteract = _playerInRange || _puzzleOpen || IsPlayerCloseEnoughForKeyboard();
        if (!canInteract)
        {
            _xrCloseWasPressedLastFrame = false;
            return;
        }

        UpdatePromptPlacement(false);

        if (_puzzleOpen)
        {
            if (keyboardOpenPressed || keyboardClosePressed || xrButtonPressed)
            {
                Debug.Log("[PuzzleTrigger] Close puzzle pressed.");
                ClosePuzzle();
            }

            return;
        }

        if (interactPressed || keyboardOpenPressed || xrButtonPressed)
        {
            Debug.Log("[PuzzleTrigger] Open puzzle pressed.");
            OpenPuzzle();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[PuzzleTrigger] Entered by: {other.name}, tag: {other.tag}");

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
        ResolvePlayerReference();

        cameraController?.TransitionToPuzzleView();

        if (lockPlayerMovementWhilePuzzleOpen)
            DisablePlayerMovement();

        if (hidePlayerWhilePuzzleOpen)
            SetPlayerVisible(false);

        puzzleManager?.EnterPuzzleMode();

        ShowPrompt(exitText);
    }

    private void ClosePuzzle()
    {
        _puzzleOpen = false;

        cameraController?.TransitionToPlayerView();

        if (lockPlayerMovementWhilePuzzleOpen)
            EnablePlayerMovement();

        if (hidePlayerWhilePuzzleOpen)
            SetPlayerVisible(true);

        puzzleManager?.ExitPuzzleMode();

        if (_playerInRange) ShowPrompt(enterText);
        else SetPromptVisible(false);
    }

    private void DisablePlayerMovement()
    {
        foreach (var s in playerScriptsToDisable)
        {
            if (s == null)
                continue;

            if (ShouldKeepScriptEnabledForXRControls(s))
            {
                Debug.LogWarning($"[PuzzleTrigger] Keeping XR/input script enabled: {s.GetType().Name}");
                continue;
            }

            if (!_disabledScriptStates.ContainsKey(s))
                _disabledScriptStates.Add(s, s.enabled);

            s.enabled = false;
        }

        if (disableCharacterControllerWhenLocked && playerCharacterController != null)
        {
            _savedCharacterControllerEnabled = playerCharacterController.enabled;
            _characterControllerStateSaved = true;
            playerCharacterController.enabled = false;
        }

        if (freezeRigidbodyWhenLocked && playerRigidbody != null)
        {
            _savedConstraints = playerRigidbody.constraints;
            _savedKinematic = playerRigidbody.isKinematic;
            _rigidbodyStateSaved = true;
            playerRigidbody.isKinematic = true;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void EnablePlayerMovement()
    {
        foreach (var state in _disabledScriptStates)
        {
            if (state.Key != null)
                state.Key.enabled = state.Value;
        }

        _disabledScriptStates.Clear();

        if (_characterControllerStateSaved && playerCharacterController != null)
            playerCharacterController.enabled = _savedCharacterControllerEnabled;

        _characterControllerStateSaved = false;

        if (_rigidbodyStateSaved && playerRigidbody != null)
        {
            playerRigidbody.isKinematic = _savedKinematic;
            playerRigidbody.constraints = _savedConstraints;
        }

        _rigidbodyStateSaved = false;
    }

    private bool ShouldKeepScriptEnabledForXRControls(MonoBehaviour script)
    {
        System.Type type = script.GetType();
        string typeName = type.Name.ToLowerInvariant();
        string fullName = (type.FullName ?? type.Name).ToLowerInvariant();

        bool isExplicitLocomotionProvider =
            typeName.Contains("moveprovider") ||
            typeName.Contains("turnprovider") ||
            typeName.Contains("teleportationprovider");

        if (isExplicitLocomotionProvider)
            return false;

        return fullName.Contains("unityengine.xr") ||
               typeName.Contains("xrorigin") ||
               typeName.Contains("interactor") ||
               typeName.Contains("controller") ||
               typeName.Contains("input") ||
               typeName.Contains("action") ||
               typeName.Contains("manager") ||
               typeName.Contains("simulator") ||
               typeName.Contains("gaze") ||
               typeName.Contains("hand") ||
               typeName.Contains("ray") ||
               typeName.Contains("grab") ||
               typeName.Contains("locomotion");
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
        if (playerGO == null)
            return;

        if (_playerTransform == null)
            _playerTransform = playerGO.transform;

        if (playerCharacterController == null)
            playerCharacterController = playerGO.GetComponentInChildren<CharacterController>();

        if (playerRigidbody == null)
            playerRigidbody = playerGO.GetComponentInChildren<Rigidbody>();

        if (playerModelRoot == null)
        {
            playerModelRoot = playerGO;
            _playerRenderers = playerModelRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
        }
    }

    private void ResolvePlayerReference()
    {
        if (_playerTransform != null)
            return;

        GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);

        if (playerGO == null && cameraController != null && cameraController.xrOrigin != null)
            playerGO = cameraController.xrOrigin.gameObject;

        AutoDiscoverPlayerComponents(playerGO);
    }

    private bool IsPlayerCloseEnoughForKeyboard()
    {
        ResolvePlayerReference();

        if (_playerTransform == null || keyboardActivationDistance <= 0f)
            return false;

        Vector3 targetPosition = transform.position;

        if (puzzleManager != null && puzzleManager.frameTransform != null)
            targetPosition = puzzleManager.frameTransform.position;

        return Vector3.Distance(_playerTransform.position, targetPosition) <= keyboardActivationDistance;
    }

    private bool WasXRClosePressedThisFrame()
    {
        bool pressed = IsXRCloseButtonPressed();
        bool pressedThisFrame = pressed && !_xrCloseWasPressedLastFrame;
        _xrCloseWasPressedLastFrame = pressed;
        return pressedThisFrame;
    }

    private bool IsXRCloseButtonPressed()
    {
        XRInputDeviceCharacteristics preferredHand = preferRightControllerForClose
            ? XRInputDeviceCharacteristics.Right
            : XRInputDeviceCharacteristics.Left;
        XRInputDeviceCharacteristics fallbackHand = preferRightControllerForClose
            ? XRInputDeviceCharacteristics.Left
            : XRInputDeviceCharacteristics.Right;

        return IsXRCloseButtonPressed(preferredHand) || IsXRCloseButtonPressed(fallbackHand);
    }

    private bool IsXRCloseButtonPressed(XRInputDeviceCharacteristics hand)
    {
        _xrDevices.Clear();
        XRInputDevices.GetDevicesWithCharacteristics(XRInputDeviceCharacteristics.Controller | hand, _xrDevices);

        foreach (XRInputDevice device in _xrDevices)
        {
            if (!device.isValid)
                continue;

            if (useSecondaryButtonForToggle &&
                device.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool secondaryPressed) &&
                secondaryPressed)
            {
                return true;
            }

            if (useMenuButtonForToggle &&
                device.TryGetFeatureValue(XRCommonUsages.menuButton, out bool menuPressed) &&
                menuPressed)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowPrompt(string text)
    {
        EnsurePromptUI();
        if (promptText != null) promptText.text = text;
        UpdatePromptPlacement(true);
        SetPromptVisible(true);
    }

    private void SetPromptVisible(bool v)
    {
        if (v)
        {
            EnsurePromptUI();
            UpdatePromptPlacement(true);
        }

        if (promptUI != null) promptUI.SetActive(v);
    }

    private void EnsurePromptUI()
    {
        if (promptUI == null && autoCreatePromptUI)
            CreateDefaultPromptUI();

        if (promptUI != null)
        {
            if (promptText == null)
                promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);

            AssignDefaultFont(promptText);

            _promptCanvas = promptUI.GetComponentInParent<Canvas>();
            ConfigurePromptCanvas();
        }
    }

    private void CreateDefaultPromptUI()
    {
        GameObject canvasObject = new GameObject("VR_PuzzlePromptCanvas");
        _promptCanvas = canvasObject.AddComponent<Canvas>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("PuzzlePromptPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.86f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(720f, 96f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject textObject = new GameObject("PuzzlePromptText");
        textObject.transform.SetParent(panel.transform, false);
        promptText = textObject.AddComponent<TextMeshProUGUI>();
        AssignDefaultFont(promptText);
        promptText.text = enterText;
        promptText.color = Color.white;
        promptText.fontSize = 34f;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.textWrappingMode = TextWrappingModes.Normal;

        RectTransform textRect = promptText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(28f, 12f);
        textRect.offsetMax = new Vector2(-28f, -12f);

        promptUI = panel;
    }

    private static void AssignDefaultFont(TextMeshProUGUI text)
    {
        if (text != null && text.font == null && TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
    }

    private void ConfigurePromptCanvas()
    {
        if (_promptCanvas == null)
            return;

        VRUiPlacement.ConfigureWorldSpaceCanvas(_promptCanvas, new Vector2(760f, 120f), 0.001f, 230);
    }

    private void UpdatePromptPlacement(bool snap)
    {
        if (!keepPromptInFrontOfCamera)
            return;

        if (_promptCanvas == null && promptUI != null)
            _promptCanvas = promptUI.GetComponentInParent<Canvas>();

        if (_promptCanvas == null)
            return;

        VRUiPlacement.PlaceInFrontOfCamera(_promptCanvas.transform, promptDistance, promptOffset, promptFollowSpeed, snap);
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
