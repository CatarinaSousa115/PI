using UnityEngine;
using UnityEngine.UI;

namespace Puzzle
{
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
        public Button exitButton;          

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onExit;  

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

        void OnExit() => onExit?.Invoke();  
        string FormatTime(float t)
        {
            int m = (int)(t / 60);
            int s = (int)(t % 60);
            return $"{m:00}:{s:00}";
        }
    }
}