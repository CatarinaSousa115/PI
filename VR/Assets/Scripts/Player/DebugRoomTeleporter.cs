using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace MuseumGame.Player
{
    public class DebugRoomTeleporter : MonoBehaviour
    {
        [Header("Destination Points")]
        public Transform lobbyPoint;
        public Transform room2Point;
        public Transform room3Point;
        public Transform room4Point;
        public Transform room5Point;

        [Header("Settings")]
        public CharacterController characterController;
        public float verticalOffset = 0.1f;
        public bool autoResolveMarkers = true;
        public string lobbyMarkerName = "Lobby 1.1 Marker";
        public string room2MarkerName = "Sala 2 - 1.2 Marker";
        public string room3MarkerName = "Sala 3 - 1.3 Marker";
        public string room4MarkerName = "Sala 4 - 1.5 Marker";
        public string room5MarkerName = "Sala 5 - 1.13 Marker";

        public XROrigin xrOrigin;
        public InputActionProperty teleportLobbyAction;
        public InputActionProperty teleportRoom2Action;
        public InputActionProperty teleportRoom3Action;
        public InputActionProperty teleportRoom4Action;
        public InputActionProperty teleportRoom5Action;

        void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (autoResolveMarkers)
                ResolveMarkers();

            if (xrOrigin == null)
                xrOrigin = FindFirstObjectByType<XROrigin>();
        }

        void Update()
        {
            if (teleportLobbyAction.action != null && teleportLobbyAction.action.WasPressedThisFrame())
                TeleportTo(lobbyPoint);
            else if (teleportRoom2Action.action != null && teleportRoom2Action.action.WasPressedThisFrame())
                TeleportTo(room2Point);
            else if (teleportRoom3Action.action != null && teleportRoom3Action.action.WasPressedThisFrame())
                TeleportTo(room3Point);
            else if (teleportRoom4Action.action != null && teleportRoom4Action.action.WasPressedThisFrame())
                TeleportTo(room4Point);
            else if (teleportRoom5Action.action != null && teleportRoom5Action.action.WasPressedThisFrame())
                TeleportTo(room5Point);
        }

        private void TeleportTo(Transform targetPoint)
        {
            if (targetPoint == null)
                return;

            Vector3 destination = targetPoint.position + Vector3.up * verticalOffset;

            if (characterController != null)
                characterController.enabled = false;

            if (xrOrigin != null)
            {
                xrOrigin.transform.position = destination;
                xrOrigin.transform.rotation = targetPoint.rotation;
            }
            else
            {
                transform.position = destination;
                transform.rotation = targetPoint.rotation;
            }

            if (characterController != null)
                characterController.enabled = true;
        }

        private void ResolveMarkers()
        {
            if (lobbyPoint == null)
                lobbyPoint = FindMarker(lobbyMarkerName);
            if (room2Point == null)
                room2Point = FindMarker(room2MarkerName);
            if (room3Point == null)
                room3Point = FindMarker(room3MarkerName);
            if (room4Point == null)
                room4Point = FindMarker(room4MarkerName);
            if (room5Point == null)
                room5Point = FindMarker(room5MarkerName);
        }

        private Transform FindMarker(string markerName)
        {
            GameObject marker = GameObject.Find(markerName);
            return marker != null ? marker.transform : null;
        }
    }
}
