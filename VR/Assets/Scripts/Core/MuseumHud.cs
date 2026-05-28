using UnityEngine;
using UnityEngine.UI;
using MuseumGame.UI;

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

        [Header("VR Placement")]
        public bool keepHudInFrontOfCamera = true;
        public float hudDistance = 1.2f;
        public Vector2 hudOffset = new Vector2(-0.18f, 0.16f);
        public float hudFollowSpeed = 8f;

        private bool subscribed;
        private Canvas hudCanvas;

        public static MuseumHud EnsureExists()
        {
            if (Instance != null)
                return Instance;

            MuseumHud found = FindFirstObjectByType<MuseumHud>();
            if (found != null)
            {
                Instance = found;
                found.EnsureHudReferences();
                found.UpdateHudPlacement(true);
                return found;
            }

            GameObject hudObject = new GameObject("VR_RuntimeMuseumHud");
            if (Application.isPlaying)
                DontDestroyOnLoad(hudObject);

            MuseumHud hud = hudObject.AddComponent<MuseumHud>();
            hud.EnsureHudReferences();
            hud.UpdateHudPlacement(true);
            return hud;
        }

        void Awake()
        {
            Instance = this;
            EnsureHudReferences();
        }

        void OnEnable()
        {
            HookManager();
        }

        void Start()
        {
            EnsureHudReferences();
            HookManager();
            RefreshImmediate();
            UpdateHudPlacement(true);
        }

        void Update()
        {
            EnsureHudReferences();

            if (!subscribed)
            {
                HookManager();
                RefreshImmediate();
            }

            UpdateHudPlacement(false);
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
            EnsureHudReferences();

            if (objectiveText != null)
                objectiveText.text = objective;
        }

        void UpdatePlaques(int current, int total)
        {
            EnsureHudReferences();

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
            EnsureHudReferences();

            if (statusText != null)
                statusText.text = message;
        }

        public void ClearStatus()
        {
            EnsureHudReferences();

            if (statusText != null)
                statusText.text = string.Empty;
        }

        private void EnsureHudReferences()
        {
            if (objectiveText != null && plaqueCounterText != null && statusText != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = gameObject.AddComponent<Canvas>();
            }

            if (canvas.GetComponent<GraphicRaycaster>() != null)
                Destroy(canvas.GetComponent<GraphicRaycaster>());

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                canvasRect.sizeDelta = new Vector2(1000f, 600f);

            Transform panelTransform = canvas.transform.Find("HUDPanel");
            GameObject panel;
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
            else
            {
                panel = new GameObject("HUDPanel");
                panel.transform.SetParent(canvas.transform, false);

                Image image = panel.AddComponent<Image>();
                image.color = new Color(0.02f, 0.03f, 0.04f, 0.72f);
                image.raycastTarget = false; // Prevent blocking VR rays

                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(20f, -20f);
                panelRect.sizeDelta = new Vector2(560f, 210f);
            }

            if (objectiveText == null)
                objectiveText = CreateHudText(panel.transform, "ObjectiveText", new Vector2(20f, -18f), new Vector2(520f, 72f), 24, FontStyle.Bold);

            if (plaqueCounterText == null)
                plaqueCounterText = CreateHudText(panel.transform, "PlaqueCounterText", new Vector2(20f, -94f), new Vector2(520f, 34f), 20, FontStyle.Normal);

            if (statusText == null)
                statusText = CreateHudText(panel.transform, "StatusText", new Vector2(20f, -132f), new Vector2(520f, 58f), 20, FontStyle.Normal);

            hudCanvas = canvas;
        }

        private Text CreateHudText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle)
        {
            Transform existing = parent.Find(name);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            if (text == null)
                text = textObject.AddComponent<Text>();

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false; // Prevent text from blocking VR rays

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            return text;
        }

        private void UpdateHudPlacement(bool snap)
        {
            if (!keepHudInFrontOfCamera)
                return;

            if (hudCanvas == null)
                hudCanvas = GetComponentInParent<Canvas>();

            if (hudCanvas == null)
                return;

            VRUiPlacement.ConfigureWorldSpaceCanvas(hudCanvas, new Vector2(1000f, 600f), 0.001f, 180);
            VRUiPlacement.PlaceInFrontOfCamera(hudCanvas.transform, hudDistance, hudOffset, hudFollowSpeed, snap);
        }
    }
}
