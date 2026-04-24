using System.Collections.Generic;
using UnityEngine;

namespace MuseumGame.Blockout
{
    [System.Serializable]
    public class RuntimeRoomLayout
    {
        public string roomName;
        public Vector2 center;
        public Vector2 size;
    }

    public class MuseumWhiteboxRuntimeLayout : MonoBehaviour
    {
        [Header("Execution")]
        public bool applyLayoutOnAwake;
        public bool rebuildDoorwaysOnApply = true;
        public bool alignHelpersOnApply = true;

        [Header("Room Geometry")]
        public float wallHeight = 2.8f;
        public float wallThickness = 0.15f;
        public float floorThickness = 0.1f;
        public List<RuntimeRoomLayout> rooms = new List<RuntimeRoomLayout>();

        [Header("Doorways")]
        public string generatedDoorwaysRootName = "GeneratedDoorways";
        public string doorMarkerPrefix = "To ";
        public bool createDoorLeaves = true;
        public bool hideDoorMarkerRenderers = true;
        public float markerHeight = 2f;
        public float doorwayHeight = 2.1f;
        public float closedDoorThickness = 0.18f;
        public float doorLeafPlaneOffset = 0.02f;
        public float doorwayClearancePadding = 0.35f;
        public float doorwayGroundClearance = 0.08f;
        public float minimumDoorwayWidth = 1.15f;
        public float minimumDoorwayHeight = 2.1f;
        public Color doorColor = new Color(0.82f, 0.82f, 0.86f, 1f);
        public Material doorMaterial;

        [Header("Objects")]
        public string domPedroName = "DPedro";
        public string lobbyRoomName = "Lobby 1.1";
        public string lobbyMarkerName = "Lobby 1.1 Marker";
        public string room2MarkerName = "Sala 2 - 1.2 Marker";
        public string tpLobbyName = "TP_Lobby";
        public string tpRoom2Name = "TP_Sala2";
        public string tpRoom3Name = "TP_Sala3";
        public string tpRoom4Name = "TP_Sala4";
        public string tpRoom5Name = "TP_Sala5";

        private void Awake()
        {
            if (!applyLayoutOnAwake)
                return;

            ApplyLayoutNow();
        }

        [ContextMenu("Apply Layout Now")]
        public void ApplyLayoutNow()
        {
            EnsureDefaults();
            ApplyLayouts();

            if (rebuildDoorwaysOnApply)
                RebuildDoorways();

            if (alignHelpersOnApply)
            {
                RepositionDomPedro();
                AlignTeleportPoint(tpLobbyName, lobbyRoomName, lobbyMarkerName);
                AlignTeleportPoint(tpRoom2Name, "Sala 2 - 1.2", room2MarkerName);
                AlignTeleportPoint(tpRoom3Name, "Sala 3 - 1.3", "Sala 3 - 1.3 Marker");
                AlignTeleportPoint(tpRoom4Name, "Sala 4 - 1.5", "Sala 4 - 1.5 Marker");
                AlignTeleportPoint(tpRoom5Name, "Sala 5 - 1.13", "Sala 5 - 1.13 Marker");
            }
        }

        [ContextMenu("Rebuild Doorways Now")]
        public void RebuildDoorwaysNow()
        {
            EnsureDefaults();
            RebuildDoorways();
        }

        private void EnsureDefaults()
        {
            if (rooms.Count > 0)
                return;

            rooms = new List<RuntimeRoomLayout>
            {
                new RuntimeRoomLayout { roomName = "Lobby 1.1", center = new Vector2(5.7f, 6.6f), size = new Vector2(3.6f, 5.2f) },
                new RuntimeRoomLayout { roomName = "Sala 2 - 1.2", center = new Vector2(0f, 6.6f), size = new Vector2(7.8f, 5.2f) },
                new RuntimeRoomLayout { roomName = "Sala 3 - 1.3", center = new Vector2(0f, -1.3f), size = new Vector2(7.2f, 5.2f) },
                new RuntimeRoomLayout { roomName = "Sala 4 - 1.5", center = new Vector2(11f, -1.3f), size = new Vector2(5.4f, 5.2f) },
                new RuntimeRoomLayout { roomName = "Sala 5 - 1.13", center = new Vector2(11f, 6.5f), size = new Vector2(3.6f, 5.2f) }
            };
        }

        private void ApplyLayouts()
        {
            foreach (RuntimeRoomLayout layout in rooms)
            {
                GameObject room = GameObject.Find(layout.roomName);
                if (room == null)
                    continue;

                ApplyRoomLayout(room.transform, layout.center, layout.size);
            }
        }

        private void ApplyRoomLayout(Transform roomRoot, Vector2 center, Vector2 size)
        {
            roomRoot.localPosition = new Vector3(center.x, 0f, center.y);

            SetPrimitive(roomRoot, "Floor", new Vector3(0f, -floorThickness * 0.5f, 0f), new Vector3(size.x, floorThickness, size.y));
            SetPrimitive(roomRoot, "NorthWall", new Vector3(0f, wallHeight * 0.5f, size.y * 0.5f), new Vector3(size.x, wallHeight, wallThickness));
            SetPrimitive(roomRoot, "SouthWall", new Vector3(0f, wallHeight * 0.5f, -size.y * 0.5f), new Vector3(size.x, wallHeight, wallThickness));
            SetPrimitive(roomRoot, "EastWall", new Vector3(size.x * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size.y));
            SetPrimitive(roomRoot, "WestWall", new Vector3(-size.x * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, size.y));
            SetMarker(roomRoot, roomRoot.name + " Marker", new Vector3(0f, 0.01f, 0f));
        }

        private void RebuildDoorways()
        {
            DoorwayBuildSettings settings = CreateDoorwayBuildSettings();

            foreach (RuntimeRoomLayout layout in rooms)
            {
                GameObject room = GameObject.Find(layout.roomName);
                if (room == null)
                    continue;

                BlockoutDoorwayBuilder.RebuildRoom(room.transform, layout.size, settings);
            }
        }

        private DoorwayBuildSettings CreateDoorwayBuildSettings()
        {
            return new DoorwayBuildSettings
            {
                generatedRootName = generatedDoorwaysRootName,
                markerPrefix = doorMarkerPrefix,
                createDoorLeaves = createDoorLeaves,
                hideDoorMarkerRenderers = hideDoorMarkerRenderers,
                wallHeight = wallHeight,
                wallThickness = wallThickness,
                markerHeight = markerHeight,
                doorwayHeight = doorwayHeight,
                closedDoorThickness = closedDoorThickness,
                doorLeafPlaneOffset = doorLeafPlaneOffset,
                doorwayClearancePadding = doorwayClearancePadding,
                doorwayGroundClearance = doorwayGroundClearance,
                minimumDoorwayWidth = minimumDoorwayWidth,
                minimumDoorwayHeight = minimumDoorwayHeight,
                doorColor = doorColor,
                doorMaterial = doorMaterial
            };
        }

        private void RepositionDomPedro()
        {
            GameObject domPedro = GameObject.Find(domPedroName);
            GameObject lobby = GameObject.Find(lobbyRoomName);
            if (domPedro == null || lobby == null)
                return;

            Transform eastWall = lobby.transform.Find("EastWall");
            if (eastWall == null)
                return;

            domPedro.transform.position = eastWall.position + Vector3.left * 0.09f + Vector3.up * -0.35f;
            domPedro.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        }

        private void AlignTeleportPoint(string pointName, string roomName, string markerName)
        {
            GameObject point = GameObject.Find(pointName);
            GameObject room = GameObject.Find(roomName);
            if (point == null || room == null)
                return;

            Transform marker = room.transform.Find(markerName);
            if (marker == null)
                return;

            point.transform.position = marker.position + Vector3.up;
            point.transform.rotation = Quaternion.identity;
        }

        private void SetPrimitive(Transform root, string childName, Vector3 localPosition, Vector3 localScale)
        {
            Transform child = root.Find(childName);
            if (child == null)
                return;

            child.localPosition = localPosition;
            child.localScale = localScale;
        }

        private void SetMarker(Transform root, string childName, Vector3 localPosition)
        {
            Transform child = root.Find(childName);
            if (child == null)
                return;

            child.localPosition = localPosition;
        }
    }
}
