using UnityEngine;
using UnityEngine.UI;

namespace MuseumGame
{
    public class MuseumHud : MonoBehaviour
    {
        public static MuseumHud Instance { get; private set; }

        [Header("UI")]
        public Text objectiveText;
        public Text plaqueCounterText;
        public Text statusText;

        [Header("Messages")]
        [TextArea(2, 4)]
        public string finishedMessage = "Todas as placas foram recuperadas. Regressa ao lobby para restaurar a colecao.";

        [Header("Visibility")]
        public float hudVisibleDuration = 20f;

        private bool subscribed;
        private CanvasGroup hudCanvasGroup;
        private float nextHudHideTime = -1f;
        private bool hudVisible = true;

        void Awake()
        {
            Instance = this;
            EnsureHudVisibilityGroup();
        }

        void OnEnable()
        {
            HookManager();
        }

        void Start()
        {
            HookManager();
            RefreshImmediate();
            ShowHud();
        }

        void Update()
        {
            if (!subscribed)
            {
                HookManager();
                RefreshImmediate();
            }

            UpdateHudVisibility();
        }

        void OnDisable()
        {
            UnhookManager();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void HookManager()
        {
            if (subscribed || MuseumGameManager.Instance == null)
                return;

            MuseumGameManager.Instance.ObjectiveChanged += UpdateObjective;
            MuseumGameManager.Instance.PlaqueProgressChanged += UpdatePlaques;
            MuseumGameManager.Instance.GameCompleted += ShowFinishedMessage;
            subscribed = true;
        }

        void UnhookManager()
        {
            if (!subscribed || MuseumGameManager.Instance == null)
                return;

            MuseumGameManager.Instance.ObjectiveChanged -= UpdateObjective;
            MuseumGameManager.Instance.PlaqueProgressChanged -= UpdatePlaques;
            MuseumGameManager.Instance.GameCompleted -= ShowFinishedMessage;
            subscribed = false;
        }

        void RefreshImmediate()
        {
            if (MuseumGameManager.Instance == null)
                return;

            UpdateObjective(MuseumGameManager.Instance.CurrentObjective);
            UpdatePlaques(
                MuseumGameManager.Instance.CollectedPlaques,
                MuseumGameManager.Instance.TotalPlaques);
        }

        void UpdateObjective(string objective)
        {
            if (objectiveText != null)
                objectiveText.text = objective;

            ShowHud();
        }

        void UpdatePlaques(int current, int total)
        {
            if (plaqueCounterText != null)
                plaqueCounterText.text = $"Placas: {current}/{total}";

            ShowHud();
        }

        void ShowFinishedMessage()
        {
            if (statusText != null)
                statusText.text = finishedMessage;

            ShowHud();
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;

            ShowHud();
        }

        public void ClearStatus()
        {
            if (statusText != null)
                statusText.text = string.Empty;

            ShowHud();
        }

        private void ShowHud()
        {
            hudVisible = true;
            ApplyHudVisibility();
            nextHudHideTime = Application.isPlaying && hudVisibleDuration > 0f
                ? Time.time + hudVisibleDuration
                : -1f;
        }

        private void UpdateHudVisibility()
        {
            if (!Application.isPlaying || !hudVisible || hudVisibleDuration <= 0f || nextHudHideTime < 0f)
                return;

            if (Time.time < nextHudHideTime)
                return;

            hudVisible = false;
            nextHudHideTime = -1f;
            ApplyHudVisibility();
        }

        private void EnsureHudVisibilityGroup()
        {
            if (hudCanvasGroup != null)
                return;

            hudCanvasGroup = GetComponentInParent<CanvasGroup>();
            if (hudCanvasGroup != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            GameObject target = canvas != null ? canvas.gameObject : gameObject;
            hudCanvasGroup = target.GetComponent<CanvasGroup>();
            if (hudCanvasGroup == null)
                hudCanvasGroup = target.AddComponent<CanvasGroup>();
        }

        private void ApplyHudVisibility()
        {
            if (hudCanvasGroup == null)
                EnsureHudVisibilityGroup();

            if (hudCanvasGroup == null)
                return;

            float targetAlpha = hudVisible ? 1f : 0f;
            if (!Mathf.Approximately(hudCanvasGroup.alpha, targetAlpha))
                hudCanvasGroup.alpha = targetAlpha;

            hudCanvasGroup.interactable = hudVisible;
            hudCanvasGroup.blocksRaycasts = hudVisible;
        }
    }
}
