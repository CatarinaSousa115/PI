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
        private bool _isDisabled = false; // ← flag de bloqueio

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = Camera.main;
        }

        void Update()
        {
            if (_isDisabled) return; // ← para tudo durante teleporte
            HandleGaze();
        }

        // Chamado pelo GazeTeleport quando o teleporte começa
        public void DisableGaze()
        {
            _isDisabled = true;
            ClearGazeTarget();
        }

        private void HandleGaze()
        {
            if (_camera == null) return;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            // ← Proteção contra direção inválida
            if (ray.direction.sqrMagnitude < 0.0001f) return;

            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.yellow);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
            {
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