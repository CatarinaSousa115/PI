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

        [Header("Completion")]
        public Text completionText; // simple text, hidden by default

        [Header("Events")]
        public UnityEngine.Events.UnityEvent onExit;

        private float elapsed;
        private bool running;

        void Awake()
        {
            Instance = this;
            if (completionText) completionText.gameObject.SetActive(false);
        }

        void Start()
        {
            running = true;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!running) return;
            elapsed += Time.deltaTime;
            if (timerText) timerText.text = FormatTime(elapsed);
        }

        public void UpdateCounter(int placed, int total)
        {
            if (counterText)
                counterText.text = $"{placed} / {total}";
        }

        public void ShowVictory(float time)
        {
            running = false;

            if (completionText)
            {
                completionText.gameObject.SetActive(true);
                completionText.text = $"Puzzle Complete!\n{FormatTime(time)}";
            }
        }

        string FormatTime(float t)
        {
            int m = (int)(t / 60);
            int s = (int)(t % 60);
            return $"{m:00}:{s:00}";
        }
    }
}