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

    public static class VRUiPlacement
    {
        public static void ConfigureWorldSpaceCanvas(Canvas canvas, Vector2 size, float scale, int sortingOrder)
        {
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
                rect.localScale = Vector3.one * scale;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        public static void PlaceInFrontOfCamera(
            Transform target,
            float distance,
            Vector2 meterOffset,
            float followSpeed,
            bool instant,
            bool avoidGeometry = true,
            float geometryPadding = 0.08f,
            float minimumDistance = 0.18f)
        {
            if (target == null)
                return;

            Camera camera = ResolveCamera();
            if (camera == null)
                return;

            Transform cameraTransform = camera.transform;
            float targetDistance = Mathf.Max(minimumDistance, distance);

            if (avoidGeometry &&
                Physics.Raycast(
                    cameraTransform.position,
                    cameraTransform.forward,
                    out RaycastHit hit,
                    targetDistance + geometryPadding,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                targetDistance = Mathf.Clamp(hit.distance - geometryPadding, 0.05f, targetDistance);
            }

            Vector3 targetPosition = cameraTransform.position +
                                     cameraTransform.forward * targetDistance +
                                     cameraTransform.right * meterOffset.x +
                                     cameraTransform.up * meterOffset.y;

            if (instant || followSpeed <= 0f)
                target.position = targetPosition;
            else
                target.position = Vector3.Lerp(target.position, targetPosition, Time.deltaTime * followSpeed);

            Vector3 lookDirection = target.position - cameraTransform.position;
            if (lookDirection.sqrMagnitude <= 0.0001f)
                lookDirection = cameraTransform.forward;

            target.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }

        public static Camera ResolveCamera()
        {
            if (Camera.main != null)
                return Camera.main;

            return Object.FindFirstObjectByType<Camera>();
        }
    }
}
