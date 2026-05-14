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

        private bool subscribed;

        void Awake()
        {
            Instance = this;
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
        }

        void UpdatePlaques(int current, int total)
        {
            if (plaqueCounterText != null)
                plaqueCounterText.text = $"Placas: {current}/{total}";
        }

        void ShowFinishedMessage()
        {
            if (statusText != null)
                statusText.text = finishedMessage;
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        public void ClearStatus()
        {
            if (statusText != null)
                statusText.text = string.Empty;
        }
    }
}
