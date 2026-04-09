using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using MuseumGame;

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

        [Header("Interaction")]
        public KeyCode interactKey = KeyCode.E;
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

        void Start()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        void Update()
        {
            bool canInteract = !requirePlayerTrigger || playerInside;
            if (!canInteract || !Input.GetKeyDown(interactKey))
                return;

            if (!dialogueOpen)
                BeginDialogue();
            else
                AdvanceDialogue();
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                playerInside = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                playerInside = false;
        }

        public void BeginDialogue()
        {
            if (oneShot && hasFinishedOnce)
                return;

            if (lines == null || lines.Length == 0)
                return;

            currentLineIndex = 0;
            dialogueOpen = true;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (speakerText != null)
                speakerText.text = speakerName;

            ShowLine();
            onDialogueStarted?.Invoke();
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

            onDialogueFinished?.Invoke();
        }

        private void ShowLine()
        {
            if (bodyText != null)
                bodyText.text = lines[currentLineIndex];
        }
    }
}
