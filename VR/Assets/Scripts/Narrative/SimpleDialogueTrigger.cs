using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MuseumGame;

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
        public string promptMessage = "Pressiona Trigger para continuar";
        public string interactionHintMessage = "Pressiona Trigger para falar";
        public Color panelColor = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        public Color speakerColor = new Color(0.96f, 0.84f, 0.56f, 1f);
        public Color bodyColor = new Color(0.96f, 0.97f, 0.98f, 1f);
        public Color promptColor = new Color(0.75f, 0.8f, 0.88f, 1f);
        public Color interactionHintColor = new Color(0.98f, 0.98f, 1f, 0.96f);

        [Header("Interaction")]
        public bool requirePlayerTrigger = true;
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
            if (dialogueOpen && dialoguePanel != null)
            {
                // Force canvas to constantly track in front of the VR camera
                Camera cam = Camera.main;
                if (cam == null) cam = FindObjectOfType<Camera>();
                
                if (cam != null)
                {
                    Canvas canvas = dialoguePanel.GetComponentInParent<Canvas>();
                    if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                    {
                        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                        
                        // Smoothly lerp towards the player's face
                        Vector3 targetPos = cam.transform.position + cam.transform.forward * 1.2f;
                        canvasRect.position = Vector3.Lerp(canvasRect.position, targetPos, Time.deltaTime * 5f);
                        canvasRect.rotation = Quaternion.LookRotation(canvasRect.position - cam.transform.position);
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInside = true;
                RefreshInteractionHint();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInside = false;
                RefreshInteractionHint();
            }
        }

        public void BeginDialogue()
        {
            Debug.Log($"[SimpleDialogueTrigger] BeginDialogue called on {gameObject.name}");

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
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                
                // Position it physically in front of the PLAYER'S FACE to guarantee visibility
                Camera cam = Camera.main;
                if (cam == null) cam = FindObjectOfType<Camera>();
                
                if (cam != null)
                {
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    canvasRect.sizeDelta = new Vector2(1000, 500); // Standard VR canvas resolution
                    canvasRect.position = cam.transform.position + cam.transform.forward * 1.5f;
                    canvasRect.rotation = Quaternion.LookRotation(canvasRect.position - cam.transform.position);
                    canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f); // Scale down pixel to meter
                }
                else
                {
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    canvasRect.sizeDelta = new Vector2(1000, 500);
                    canvasRect.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.2f;
                    canvasRect.rotation = transform.rotation;
                    canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                }
            }
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
                    
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    canvasRect.sizeDelta = new Vector2(1000, 500);
                    canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
                    
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
        }

        private void ApplyDialogueStyling()
        {
            if (!autoStyleDialogue)
                return;

            if (dialoguePanel != null)
            {
                Image panelImage = dialoguePanel.GetComponent<Image>();
                if (panelImage != null)
                    panelImage.color = panelColor;

                RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0f);
                    panelRect.anchorMax = new Vector2(0.5f, 0f);
                    panelRect.pivot = new Vector2(0.5f, 0f);
                    panelRect.anchoredPosition = new Vector2(0f, 34f);
                    panelRect.sizeDelta = new Vector2(860f, 230f);
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
                speakerText.fontSize = 26;
                speakerText.alignment = TextAnchor.UpperLeft;

                RectTransform rect = speakerText.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -20f);
                    rect.sizeDelta = new Vector2(-56f, 38f);
                }
            }

            if (bodyText != null)
            {
                bodyText.color = bodyColor;
                bodyText.fontSize = 22;
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
                    rect.sizeDelta = new Vector2(-56f, -104f);
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
            promptText.fontSize = 16;
            promptText.alignment = TextAnchor.LowerRight;
            promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            promptText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = promptText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 14f);
                rect.sizeDelta = new Vector2(-48f, 22f);
            }
        }

        private void EnsureInteractionHintText()
        {
            if (interactionHintText == null)
            {
                Transform canvasTransform = dialoguePanel != null ? dialoguePanel.transform.parent : null;
                if (canvasTransform == null)
                    return;

                GameObject hint = new GameObject("DialogueHintText");
                hint.transform.SetParent(canvasTransform, false);
                interactionHintText = hint.AddComponent<Text>();
                interactionHintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            interactionHintText.text = interactionHintMessage;
            interactionHintText.color = interactionHintColor;
            interactionHintText.fontSize = 20;
            interactionHintText.alignment = TextAnchor.MiddleCenter;
            interactionHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            interactionHintText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = interactionHintText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 274f);
                rect.sizeDelta = new Vector2(420f, 28f);
            }

            Outline outline = interactionHintText.GetComponent<Outline>();
            if (outline == null)
                outline = interactionHintText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private void RefreshInteractionHint()
        {
            if (interactionHintText == null)
                return;

            bool showHint = !dialogueOpen && (!requirePlayerTrigger || playerInside);
            interactionHintText.gameObject.SetActive(showHint);
        }

        void OnDestroy()
        {
            XRSimpleInteractable interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnXRInteract);
        }
    }
}
