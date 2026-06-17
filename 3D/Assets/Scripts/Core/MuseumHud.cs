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

        [Header("Auto Hide")]
        [SerializeField, Min(0f)] private float visibleDuration = 20f;

        private bool subscribed;
        private CanvasGroup visibilityGroup;
        private float hideAtTime;
        private bool hidden;

        void Awake()
        {
            Instance = this;
            visibilityGroup = GetComponent<CanvasGroup>();

            if (visibilityGroup == null)
                visibilityGroup = gameObject.AddComponent<CanvasGroup>();

            ShowHud();
        }

        void OnEnable()
        {
            HookManager();
        }

        void Start()
        {
            HookManager();
            RefreshImmediate();
        }

        void Update()
        {
            if (!subscribed)
            {
                HookManager();
                RefreshImmediate();
            }

            if (!hidden && visibleDuration > 0f && Time.unscaledTime >= hideAtTime)
                SetHudVisible(false);
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
            hideAtTime = Time.unscaledTime + visibleDuration;
            SetHudVisible(true);
        }

        private void SetHudVisible(bool visible)
        {
            hidden = !visible;

            if (visibilityGroup == null)
                return;

            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.interactable = visible;
            visibilityGroup.blocksRaycasts = visible;
        }
    }
}
