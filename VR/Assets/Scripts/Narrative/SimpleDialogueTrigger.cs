using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MuseumGame;
using MuseumGame.UI;

using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace MuseumGame.Narrative
{
    public class SimpleDialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue")]
        public string speakerName = "Dom Pedro";
        [TextArea(2, 4)]
        public string[] lines;

        [Header("UI")]
        public GameObject dialoguePanel;
        public Text speakerText;
        public Text bodyText;
        public Text promptText;
        public Text interactionHintText;

        [Header("Look")]
        public bool autoResolveUiReferences = true;
        public bool autoStyleDialogue = true;
        public string promptMessage = "Carrega [G] para continuar";
        public string interactionHintMessage = "Carrega [G] para interagir";
        public Color panelColor = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        public Color speakerColor = new Color(0.96f, 0.84f, 0.56f, 1f);
        public Color bodyColor = new Color(0.96f, 0.97f, 0.98f, 1f);
        public Color promptColor = new Color(0.75f, 0.8f, 0.88f, 1f);
        public Color interactionHintColor = new Color(0.98f, 0.98f, 1f, 0.96f);

        [Header("VR Placement")]
        public bool keepUiInFrontOfCamera = true;
        public float uiDistance = 1f;
        public Vector2 uiOffset = new Vector2(0f, -0.1f);
        public float uiFollowSpeed = 8f;

        [Header("Interaction")]
        public string playerTag = "Player";
        public bool requirePlayerTrigger = true;
        public Key keyboardInteractKey = Key.G;
        public float keyboardActivationDistance = 5f;
        public bool oneShot = true;

        [Header("Room")]
        public MuseumRoom linkedRoom;
        public bool completeRoomAfterDialogue;

        [Header("Events")]
        public UnityEvent onDialogueStarted;
        public UnityEvent onDialogueFinished;

        private int currentLineIndex;
        private bool dialogueOpen;
        private bool playerInside;
        private bool hasFinishedOnce;
        private Transform playerTransform;
        private GameObject interactionHintPanel;

        void Awake()
        {
            EnsureReady();
        }

        void Start()
        {
            EnsureReady();

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            // Regista os eventos XR
            XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
            if (interactable == null)
                interactable = gameObject.AddComponent<XRSimpleInteractable>();
            interactable.selectEntered.AddListener(OnXRInteract);
        }

        // Callback do XR — equivalente a premir E
        private void OnXRInteract(SelectEnterEventArgs args)
        {
            Debug.Log($"[SimpleDialogueTrigger] OnXRInteract triggered by VR Laser on {gameObject.name}!");
            
            // VR FIX: Ignore the 'playerInside' trigger zone requirement. 
            // If the player aimed their laser and clicked, they intend to interact.
            if (!dialogueOpen) BeginDialogue();
            else AdvanceDialogue();
        }

        void OnEnable()
        {
            EnsureReady();

            if (dialoguePanel != null && !dialogueOpen)
                dialoguePanel.SetActive(false);
        }

        void Update()
        {
            bool canInteract = CanInteractWithPlayer();
            bool keyboardPressed = keyboardInteractKey != Key.None &&
                                   Keyboard.current != null &&
                                   Keyboard.current[keyboardInteractKey].wasPressedThisFrame;

            if (keyboardPressed && (dialogueOpen || canInteract))
            {
                if (!dialogueOpen)
                    BeginDialogue();
                else
                    AdvanceDialogue();
            }

            RefreshInteractionHint();

            bool hintVisible = interactionHintPanel != null
                ? interactionHintPanel.activeInHierarchy
                : interactionHintText != null && interactionHintText.gameObject.activeInHierarchy;

            if (keepUiInFrontOfCamera && dialoguePanel != null && (dialogueOpen || hintVisible))
            {
                Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
                ConfigureDialogueCanvas(canvas, false);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInside = true;
                playerTransform = other.transform;
                RefreshInteractionHint();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                playerInside = false;
                RefreshInteractionHint();
            }
        }

        public void BeginDialogue()
        {
            Debug.Log($"[SimpleDialogueTrigger] BeginDialogue called on {gameObject.name}");

            if (oneShot && hasFinishedOnce)
            {
                RefreshInteractionHint();
                return;
            }

            if (lines == null || lines.Length == 0)
            {
                Debug.Log("[SimpleDialogueTrigger] Aborting: No dialogue lines configured.");
                return;
            }

            currentLineIndex = 0;
            dialogueOpen = true;

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
                EnsureCanvasIsWorldSpace();
                Debug.Log($"[SimpleDialogueTrigger] Activated dialogue panel at: {dialoguePanel.transform.position}");
            }
            else
            {
                Debug.LogError("[SimpleDialogueTrigger] CRITICAL ERROR: dialoguePanel is null!");
            }

            if (speakerText != null)
                speakerText.text = speakerName;

            if (promptText != null)
                promptText.text = promptMessage;

            RefreshInteractionHint();
            ShowLine();
            onDialogueStarted?.Invoke();
            Debug.Log($"[SimpleDialogueTrigger] Dialogue successfully opened. Displaying line 0: {lines[0]}");
        }

        private void EnsureCanvasIsWorldSpace()
        {
            if (dialoguePanel == null) return;
            Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
            ConfigureDialogueCanvas(canvas, true);
        }

        private void ConfigureDialogueCanvas(Canvas canvas, bool snap)
        {
            if (canvas == null)
                return;

            VRUiPlacement.ConfigureWorldSpaceCanvas(canvas, new Vector2(1200f, 620f), 0.00105f, 220);

            if (keepUiInFrontOfCamera)
                VRUiPlacement.PlaceInFrontOfCamera(canvas.transform, uiDistance, uiOffset, uiFollowSpeed, snap);
        }

        public void AdvanceDialogue()
        {
            if (!dialogueOpen)
                return;

            currentLineIndex++;

            if (currentLineIndex >= lines.Length)
            {
                FinishDialogue();
                return;
            }

            ShowLine();
        }

        public void FinishDialogue()
        {
            dialogueOpen = false;
            hasFinishedOnce = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (completeRoomAfterDialogue && linkedRoom != null)
                linkedRoom.MarkComplete();

            RefreshInteractionHint();
            onDialogueFinished?.Invoke();
        }

        private void ShowLine()
        {
            if (bodyText != null)
                bodyText.text = lines[currentLineIndex];
        }

        public void EnsureReady()
        {
            ResolveUiReferences();
            ApplyDialogueStyling();
            RefreshInteractionHint();
        }

        private void ResolveUiReferences()
        {
            if (!autoResolveUiReferences)
                return;

            if (dialoguePanel == null)
            {
                GameObject panel = GameObject.Find("DialoguePanel");
                if (panel == null)
                {
                    // CRITICAL FIX: The VR Scene doesn't have a Dialogue Canvas. We must build one from scratch.
                    GameObject canvasObj = new GameObject("VR_DialogueCanvas");
                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    canvasRect.sizeDelta = new Vector2(1200f, 620f);
                    canvasRect.localScale = Vector3.one * 0.00105f;
                    
                    panel = new GameObject("DialoguePanel");
                    panel.transform.SetParent(canvasObj.transform, false);
                    panel.AddComponent<Image>();
                    
                    Debug.Log("[SimpleDialogueTrigger] Auto-generated missing VR_DialogueCanvas.");
                }
                dialoguePanel = panel;
            }

            if (speakerText == null)
            {
                GameObject speaker = GameObject.Find("SpeakerText");
                if (speaker == null && dialoguePanel != null)
                {
                    speaker = new GameObject("SpeakerText");
                    speaker.transform.SetParent(dialoguePanel.transform, false);
                    speakerText = speaker.AddComponent<Text>();
                    speakerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                else if (speaker != null)
                {
                    speakerText = speaker.GetComponent<Text>();
                }
            }

            if (bodyText == null)
            {
                GameObject body = GameObject.Find("BodyText");
                if (body == null && dialoguePanel != null)
                {
                    body = new GameObject("BodyText");
                    body.transform.SetParent(dialoguePanel.transform, false);
                    bodyText = body.AddComponent<Text>();
                    bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                else if (body != null)
                {
                    bodyText = body.GetComponent<Text>();
                }
            }

            if (promptText == null && dialoguePanel != null)
            {
                Transform promptTransform = dialoguePanel.transform.Find("PromptText");
                if (promptTransform != null)
                    promptText = promptTransform.GetComponent<Text>();
            }

            if (interactionHintText == null)
            {
                GameObject hint = GameObject.Find("DialogueHintText");
                if (hint != null)
                    interactionHintText = hint.GetComponent<Text>();
            }

            if (dialoguePanel != null)
                ConfigureDialogueCanvas(dialoguePanel.GetComponentInParent<Canvas>(), true);
        }

        private void ApplyDialogueStyling()
        {
            if (!autoStyleDialogue)
                return;

            if (dialoguePanel != null)
            {
                Image panelImage = dialoguePanel.GetComponent<Image>();
                if (panelImage != null)
                    panelImage.color = new Color(0.04f, 0.05f, 0.07f, 0.86f);

                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0f);
                    panelRect.anchorMax = new Vector2(0.5f, 0f);
                    panelRect.pivot = new Vector2(0.5f, 0f);
                    panelRect.anchoredPosition = new Vector2(0f, 44f);
                    panelRect.sizeDelta = new Vector2(1040f, 300f);
                }

                Outline panelOutline = dialoguePanel.GetComponent<Outline>();
                if (panelOutline == null)
                    panelOutline = dialoguePanel.AddComponent<Outline>();
                panelOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
                panelOutline.effectDistance = new Vector2(2f, -2f);

                Shadow panelShadow = dialoguePanel.GetComponent<Shadow>();
                if (panelShadow == null)
                    panelShadow = dialoguePanel.AddComponent<Shadow>();
                panelShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
                panelShadow.effectDistance = new Vector2(0f, -8f);
            }

            if (speakerText != null)
            {
                speakerText.color = speakerColor;
                speakerText.fontSize = 32;
                speakerText.alignment = TextAnchor.UpperLeft;

                RectTransform rect = speakerText.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -24f);
                    rect.sizeDelta = new Vector2(-64f, 44f);
                }
            }

            if (bodyText != null)
            {
                bodyText.color = bodyColor;
                bodyText.fontSize = 28;
                bodyText.alignment = TextAnchor.UpperLeft;
                bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
                bodyText.verticalOverflow = VerticalWrapMode.Overflow;

                RectTransform rect = bodyText.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, -6f);
                    rect.sizeDelta = new Vector2(-64f, -124f);
                }
            }

            EnsurePromptText();
            EnsureInteractionHintText();
        }

        private void EnsurePromptText()
        {
            if (dialoguePanel == null)
                return;

            if (promptText == null)
            {
                GameObject prompt = new GameObject("PromptText");
                prompt.transform.SetParent(dialoguePanel.transform, false);
                promptText = prompt.AddComponent<Text>();
                promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            promptText.text = promptMessage;
            promptText.color = promptColor;
            promptText.fontSize = 22;
            promptText.alignment = TextAnchor.LowerRight;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = promptText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 18f);
                rect.sizeDelta = new Vector2(-56f, 30f);
            }
        }

        private void EnsureInteractionHintText()
        {
            Transform canvasTransform = dialoguePanel != null ? dialoguePanel.transform.parent : null;
            if (canvasTransform == null)
                return;

            interactionHintPanel = EnsureInteractionHintPanel(canvasTransform);

            if (interactionHintText == null)
            {
                Transform existingHint = interactionHintPanel.transform.Find("DialogueHintText");

                if (existingHint == null)
                {
                    Transform legacyHint = canvasTransform.Find("DialogueHintText");
                    if (legacyHint != null)
                    {
                        existingHint = legacyHint;
                        existingHint.SetParent(interactionHintPanel.transform, false);
                    }
                }

                if (existingHint == null)
                {
                    GameObject hint = new GameObject("DialogueHintText");
                    hint.transform.SetParent(interactionHintPanel.transform, false);
                    interactionHintText = hint.AddComponent<Text>();
                }
                else
                {
                    interactionHintText = existingHint.GetComponent<Text>();
                    if (interactionHintText == null)
                        interactionHintText = existingHint.gameObject.AddComponent<Text>();
                }
            }
            else if (interactionHintText.transform.parent != interactionHintPanel.transform)
            {
                interactionHintText.transform.SetParent(interactionHintPanel.transform, false);
            }

            interactionHintText.text = interactionHintMessage;
            interactionHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            interactionHintText.color = Color.white;
            interactionHintText.fontSize = 34;
            interactionHintText.fontStyle = FontStyle.Bold;
            interactionHintText.alignment = TextAnchor.MiddleCenter;
            interactionHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            interactionHintText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = interactionHintText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = new Vector2(28f, 12f);
                rect.offsetMax = new Vector2(-28f, -12f);
            }

            Outline outline = interactionHintText.GetComponent<Outline>();
            if (outline == null)
                outline = interactionHintText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private GameObject EnsureInteractionHintPanel(Transform canvasTransform)
        {
            Transform panelTransform = canvasTransform.Find("DialogueHintPanel");
            GameObject panel = panelTransform != null ? panelTransform.gameObject : new GameObject("DialogueHintPanel");
            panel.transform.SetParent(canvasTransform, false);

            Image image = panel.GetComponent<Image>();
            if (image == null)
                image = panel.AddComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.07f, 0.86f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = new Vector2(0f, -178f);
                panelRect.sizeDelta = new Vector2(760f, 116f);
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            return panel;
        }

        private void RefreshInteractionHint()
        {
            if (interactionHintText == null)
                return;

            bool showHint = !dialogueOpen && (!oneShot || !hasFinishedOnce) && CanInteractWithPlayer();
            if (interactionHintPanel != null)
                interactionHintPanel.SetActive(showHint);
            else
                interactionHintText.gameObject.SetActive(showHint);

            if (showHint && dialoguePanel != null)
                ConfigureDialogueCanvas(dialoguePanel.GetComponentInParent<Canvas>(), true);
        }

        private bool CanInteractWithPlayer()
        {
            return !requirePlayerTrigger || playerInside || IsPlayerCloseEnoughForKeyboard();
        }

        private bool IsPlayerCloseEnoughForKeyboard()
        {
            if (keyboardActivationDistance <= 0f)
                return false;

            Transform target = ResolvePlayerTransform();
            if (target == null)
                return false;

            return Vector3.Distance(target.position, transform.position) <= keyboardActivationDistance;
        }

        private Transform ResolvePlayerTransform()
        {
            if (playerTransform != null)
                return playerTransform;

            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTransform = player.transform;
                return playerTransform;
            }

            Camera camera = VRUiPlacement.ResolveCamera();
            if (camera != null)
                return camera.transform;

            return null;
        }

        void OnDestroy()
        {
            XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnXRInteract);
        }
    }
}
