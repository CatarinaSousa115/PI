using UnityEngine;
using UnityEngine.UI;

namespace MuseumGame.UI
{
    public class HudLayoutBootstrap : MonoBehaviour
    {
        public RectTransform objectiveText;
        public RectTransform plaqueText;
        public RectTransform statusText;

        void Start()
        {
            ApplyLayout(objectiveText, new Vector2(20f, -20f), new Vector2(520f, 60f));
            ApplyLayout(plaqueText, new Vector2(20f, -88f), new Vector2(260f, 36f));
            ApplyLayout(statusText, new Vector2(20f, -132f), new Vector2(520f, 60f));

            SetAlignment(objectiveText, TextAnchor.UpperLeft);
            SetAlignment(plaqueText, TextAnchor.UpperLeft);
            SetAlignment(statusText, TextAnchor.UpperLeft);
        }

        private void ApplyLayout(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private void SetAlignment(RectTransform rect, TextAnchor alignment)
        {
            if (rect == null)
                return;

            Text text = rect.GetComponent<Text>();
            if (text != null)
                text.alignment = alignment;
        }
    }
}
