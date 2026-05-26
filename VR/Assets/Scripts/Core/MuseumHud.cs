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
        public Vector2 hudOffset = Vector2.zero;
        public float hudFollowSpeed = 14f;
        public bool recenterOnlyWhenNeeded = false;
        public float hudRecenterAngle = 48f;
        public float hudMinDistance = 0.75f;
        public float hudMaxDistance = 2.6f;
        public float hudRecenterCooldown = 0.45f;

        private bool subscribed;
        private Canvas hudCanvas;
        private float nextHudRecenterTime;

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
                plaqueCounterText.text = $"Progresso: {current}/{total} placas";
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
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = gameObject.AddComponent<Canvas>();
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
                canvasRect.sizeDelta = new Vector2(1000f, 600f);

            GameObject objectivePanel = EnsurePanel(canvas.transform, "ObjectivePanel", "HUDPanel");
            GameObject statusPanel = EnsurePanel(canvas.transform, "StatusPanel", null);

            if (objectiveText == null)
                objectiveText = CreateHudText(objectivePanel.transform, "ObjectiveText");
            else if (objectiveText.transform.parent != objectivePanel.transform)
                objectiveText.transform.SetParent(objectivePanel.transform, false);

            if (plaqueCounterText == null)
                plaqueCounterText = CreateHudText(objectivePanel.transform, "PlaqueCounterText");
            else if (plaqueCounterText.transform.parent != objectivePanel.transform)
                plaqueCounterText.transform.SetParent(objectivePanel.transform, false);

            if (statusText == null)
                statusText = CreateHudText(statusPanel.transform, "StatusText");
            else if (statusText.transform.parent != statusPanel.transform)
                statusText.transform.SetParent(statusPanel.transform, false);

            StyleObjectivePanel(objectivePanel);
            StyleStatusPanel(statusPanel);
            StyleHudText(objectiveText, new Vector2(32f, -18f), new Vector2(460f, 72f), 21, FontStyle.Bold, new Color(0.96f, 0.98f, 1f, 1f));
            StyleHudText(plaqueCounterText, new Vector2(32f, -94f), new Vector2(410f, 34f), 18, FontStyle.Bold, new Color(1f, 0.86f, 0.48f, 1f));
            StyleStatusText(statusText);
            hudCanvas = canvas;
        }

        private GameObject EnsurePanel(Transform canvasTransform, string preferredName, string legacyName)
        {
            Transform panelTransform = canvasTransform.Find(preferredName);

            if (panelTransform == null && !string.IsNullOrWhiteSpace(legacyName))
                panelTransform = canvasTransform.Find(legacyName);

            GameObject panel = panelTransform != null ? panelTransform.gameObject : new GameObject(preferredName);
            panel.name = preferredName;
            panel.transform.SetParent(canvasTransform, false);
            return panel;
        }

        private Text CreateHudText(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            if (text == null)
                text = textObject.AddComponent<Text>();

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private void StyleObjectivePanel(GameObject panel)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null)
                image = panel.AddComponent<Image>();
            image.color = new Color(0.015f, 0.018f, 0.024f, 0.82f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(8f, -10f);
                panelRect.sizeDelta = new Vector2(520f, 150f);
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = panel.GetComponent<Shadow>();
            if (shadow == null)
                shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Transform accentTransform = panel.transform.Find("AccentBar");
            GameObject accent = accentTransform != null ? accentTransform.gameObject : new GameObject("AccentBar");
            accent.transform.SetParent(panel.transform, false);

            Image accentImage = accent.GetComponent<Image>();
            if (accentImage == null)
                accentImage = accent.AddComponent<Image>();
            accentImage.color = new Color(0.95f, 0.68f, 0.22f, 1f);

            RectTransform accentRect = accent.GetComponent<RectTransform>();
            if (accentRect != null)
            {
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 1f);
                accentRect.anchoredPosition = new Vector2(14f, -16f);
                accentRect.sizeDelta = new Vector2(6f, 112f);
            }
        }

        private void StyleStatusPanel(GameObject panel)
        {
            Image image = panel.GetComponent<Image>();
            if (image == null)
                image = panel.AddComponent<Image>();
            image.color = new Color(0.015f, 0.018f, 0.024f, 0.88f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = new Vector2(8f, -170f);
                panelRect.sizeDelta = new Vector2(520f, 82f);
            }

            Outline outline = panel.GetComponent<Outline>();
            if (outline == null)
                outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = panel.GetComponent<Shadow>();
            if (shadow == null)
                shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.36f);
            shadow.effectDistance = new Vector2(0f, -8f);
        }

        private void StyleHudText(Text text, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, Color color)
        {
            if (text == null)
                return;

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void StyleStatusText(Text text)
        {
            if (text == null)
                return;

            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = new Color(0.86f, 0.95f, 1f, 1f);
            text.fontSize = 21;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(28f, 12f);
            rect.offsetMax = new Vector2(-24f, -12f);
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

            if (!snap && recenterOnlyWhenNeeded && !ShouldRecenterHud())
            {
                FaceCamera(hudCanvas.transform);
                return;
            }

            VRUiPlacement.PlaceInFrontOfCamera(hudCanvas.transform, hudDistance, hudOffset, hudFollowSpeed, snap);
        }

        private bool ShouldRecenterHud()
        {
            Camera camera = VRUiPlacement.ResolveCamera();
            if (camera == null || hudCanvas == null)
                return false;

            Vector3 toHud = hudCanvas.transform.position - camera.transform.position;
            float sqrDistance = toHud.sqrMagnitude;
            if (sqrDistance <= 0.0001f)
                return true;

            float distance = Mathf.Sqrt(sqrDistance);
            if (distance < hudMinDistance || distance > hudMaxDistance)
                return true;

            float angle = Vector3.Angle(camera.transform.forward, toHud / distance);
            if (angle <= hudRecenterAngle)
                return false;

            if (Application.isPlaying && Time.time < nextHudRecenterTime)
                return false;

            if (Application.isPlaying)
                nextHudRecenterTime = Time.time + hudRecenterCooldown;

            return true;
        }

        private void FaceCamera(Transform target)
        {
            Camera camera = VRUiPlacement.ResolveCamera();
            if (camera == null || target == null)
                return;

            Vector3 lookDirection = target.position - camera.transform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
                lookDirection = camera.transform.forward;

            target.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }
}
