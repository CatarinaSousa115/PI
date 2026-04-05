using UnityEngine;
using UnityEngine.UI;

namespace Puzzle
{
    // Attach to a Canvas that belongs to the puzzle scene/area.
    // Completely separate from any other UI in your game.
    public class PuzzleUI : MonoBehaviour
    {
        public static PuzzleUI Instance { get; private set; }

        [Header("HUD")]
        public Text timerText;
        public Text counterText;

        [Header("Victory Panel")]
        public GameObject victoryPanel;
        public Text victoryTimeText;
        public Button playAgainButton;
        public Button exitButton;          // returns to your main game

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onExit;  // hook to your game's scene loader

        private float elapsed;
        private bool running;

        void Awake()
        {
            Instance = this;
            if (victoryPanel) victoryPanel.SetActive(false);
        }

        void Start()
        {
            running = true;
            if (playAgainButton) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (exitButton) exitButton.onClick.AddListener(OnExit);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!running) return;
            elapsed += Time.deltaTime;
            if (timerText)
                timerText.text = FormatTime(elapsed);
        }

        public void UpdateCounter(int placed, int total)
        {
            if (counterText)
                counterText.text = $"{placed} / {total}";
        }

        public void ShowVictory(float time)
        {
            running = false;
            if (victoryPanel) victoryPanel.SetActive(true);
            if (victoryTimeText) victoryTimeText.text = $"Completed in {FormatTime(time)}";
        }

        void OnPlayAgain()
        {
            elapsed = 0f;
            running = true;
            if (victoryPanel) victoryPanel.SetActive(false);
            PuzzleManager.Instance?.RestartPuzzle();
        }

        void OnExit() => onExit?.Invoke();  // your game handles scene transition

        string FormatTime(float t)
        {
            int m = (int)(t / 60);
            int s = (int)(t % 60);
            return $"{m:00}:{s:00}";
        }
    }
}