using UnityEngine;

namespace MuseumGame.Narrative
{
    [RequireComponent(typeof(Renderer))]
    public class PaintingFrameMarker : MonoBehaviour
    {
        public string frameRootName = "FrameMarker";
        public float depthOffset = 0.03f;
        public float borderThickness = 0.08f;
        public float borderDepth = 0.04f;
        public Color frameColor = new Color(0.36f, 0.22f, 0.08f, 1f);

        void Start()
        {
            EnsureFrame();
        }

        [ContextMenu("Rebuild Frame")]
        public void EnsureFrame()
        {
            Transform existing = transform.Find(frameRootName);
            if (existing != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(existing.gameObject);
                else
                    Destroy(existing.gameObject);
                #else
                Destroy(existing.gameObject);
                #endif
            }

            Renderer targetRenderer = GetComponent<Renderer>();
            Bounds bounds = targetRenderer.bounds;

            GameObject frameRoot = new GameObject(frameRootName);
            frameRoot.transform.SetParent(transform, false);
            frameRoot.transform.position = bounds.center;
            frameRoot.transform.rotation = transform.rotation;

            Vector3 localCenter = frameRoot.transform.InverseTransformPoint(bounds.center);
            Vector3 right = frameRoot.transform.InverseTransformVector(transform.right).normalized;
            Vector3 up = frameRoot.transform.InverseTransformVector(transform.up).normalized;
            Vector3 forward = frameRoot.transform.InverseTransformVector(transform.forward).normalized;

            float width = Vector3.Dot(bounds.size, new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z)));
            float height = Vector3.Dot(bounds.size, new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)));

            CreateBar(frameRoot.transform, "Top", localCenter + up * (height * 0.5f + borderThickness * 0.5f) + forward * depthOffset, width + borderThickness * 2f, borderThickness);
            CreateBar(frameRoot.transform, "Bottom", localCenter - up * (height * 0.5f + borderThickness * 0.5f) + forward * depthOffset, width + borderThickness * 2f, borderThickness);
            CreateBar(frameRoot.transform, "Left", localCenter - right * (width * 0.5f + borderThickness * 0.5f) + forward * depthOffset, borderThickness, height);
            CreateBar(frameRoot.transform, "Right", localCenter + right * (width * 0.5f + borderThickness * 0.5f) + forward * depthOffset, borderThickness, height);
        }

        private void CreateBar(Transform parent, string barName, Vector3 localPosition, float width, float height)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = barName;
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = new Vector3(width, height, borderDepth);

            Collider collider = bar.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            MeshRenderer renderer = bar.GetComponent<MeshRenderer>();
            renderer.material = CreateFrameMaterial();
        }

        private Material CreateFrameMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = frameColor;
            return material;
        }
    }
}
