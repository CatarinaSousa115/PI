using UnityEngine;

namespace MuseumGame.Player
{
    public class GazeManager : MonoBehaviour
    {
        [Header("Settings")]
        public float maxDistance = 20f;
        public LayerMask interactableLayer = ~0;

        private global::GazeTeleport _currentGazeTarget;
        private Camera _camera;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
        }

        void Update()
        {
            HandleGaze();
        }

        private void HandleGaze()
        {
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            // Debug line visible in Scene View
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow);

            if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
            {
                // Look for component on the hit object or its parents
                global::GazeTeleport target = hit.collider.GetComponentInParent<global::GazeTeleport>();

                if (target != null)
                {
                    if (_currentGazeTarget != target)
                    {
                        ClearGazeTarget();
                        _currentGazeTarget = target;
                        _currentGazeTarget.OnGazeEnter();
                        Debug.Log($"[GazeManager] Hit GazeTarget: {hit.collider.gameObject.name}");
                    }
                }
                else
                {
                    if (hit.collider.gameObject != null)
                    {
                        // Log what we are hitting that ISN'T a target
                        // Debug.Log($"[GazeManager] Hitting non-target: {hit.collider.gameObject.name}");
                    }
                    ClearGazeTarget();
                }
            }
            else
            {
                ClearGazeTarget();
            }
        }

        private void ClearGazeTarget()
        {
            if (_currentGazeTarget != null)
            {
                _currentGazeTarget.OnGazeExit();
                _currentGazeTarget = null;
            }
        }
    }
}
