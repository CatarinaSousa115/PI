using UnityEngine;

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

        [Header("Controls")]
        public KeyCode lobbyKey = KeyCode.Alpha1;
        public KeyCode room2Key = KeyCode.Alpha2;
        public KeyCode room3Key = KeyCode.Alpha3;
        public KeyCode room4Key = KeyCode.Alpha4;
        public KeyCode room5Key = KeyCode.Alpha5;

        [Header("Settings")]
        public CharacterController characterController;
        public float verticalOffset = 0.1f;
        public bool autoResolveMarkers = true;
        public string lobbyMarkerName = "Lobby 1.1 Marker";
        public string room2MarkerName = "Sala 2 - 1.2 Marker";
        public string room3MarkerName = "Sala 3 - 1.3 Marker";
        public string room4MarkerName = "Sala 4 - 1.5 Marker";
        public string room5MarkerName = "Sala 5 - 1.13 Marker";

        void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (autoResolveMarkers)
                ResolveMarkers();
        }

        void Update()
        {
            if (Input.GetKeyDown(lobbyKey))
                TeleportTo(lobbyPoint);
            else if (Input.GetKeyDown(room2Key))
                TeleportTo(room2Point);
            else if (Input.GetKeyDown(room3Key))
                TeleportTo(room3Point);
            else if (Input.GetKeyDown(room4Key))
                TeleportTo(room4Point);
            else if (Input.GetKeyDown(room5Key))
                TeleportTo(room5Point);
        }

        private void TeleportTo(Transform targetPoint)
        {
            if (targetPoint == null)
                return;

            Vector3 destination = targetPoint.position + Vector3.up * verticalOffset;

            if (characterController != null)
                characterController.enabled = false;

            transform.position = destination;
            transform.rotation = targetPoint.rotation;

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
