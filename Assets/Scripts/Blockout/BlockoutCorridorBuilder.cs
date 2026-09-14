using System.Collections.Generic;
using UnityEngine;

namespace MuseumGame.Blockout
{
    public enum CorridorPathMode
    {
        Auto,
        Straight,
        XThenZ,
        ZThenX
    }

    [System.Serializable]
    public class RuntimeCorridorConnection
    {
        public string label;
        public string fromRoomName;
        public string fromDoorName;
        public string toRoomName;
        public string toDoorName;
        public float width = 1.4f;
        public float leadLength = 0.7f;
        public CorridorPathMode pathMode = CorridorPathMode.Auto;
    }

    public struct CorridorBuildSettings
    {
        public string generatedRootName;
        public string markerPrefix;
        public float defaultWidth;
        public float defaultLeadLength;
        public float minimumCorridorLength;
        public float wallHeight;
        public float wallThickness;
        public float floorThickness;
        public bool createCeilings;
        public Material wallMaterial;
        public Material floorMaterial;
    }

    public static class BlockoutCorridorBuilder
    {
        private const string DefaultGeneratedRootName = "GeneratedCorridors";
        private const float MinimumSegmentLength = 0.05f;

        private struct DoorEndpoint
        {
            public Transform marker;
            public Vector3 position;
            public Vector3 normal;
        }

        private struct DoorMarkerInfo
        {
            public string roomName;
            public string roomKey;
            public string doorName;
            public string targetRoomKey;
        }

        public static void Rebuild(
            Transform parent,
            IReadOnlyList<RuntimeCorridorConnection> connections,
            CorridorBuildSettings settings)
        {
            if (parent == null)
                return;

            settings = Normalize(settings);
            ClearGeneratedCorridors(parent, settings.generatedRootName);

            if (connections == null || connections.Count == 0)
                return;

            Transform root = new GameObject(settings.generatedRootName).transform;
            root.SetParent(parent, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            for (int i = 0; i < connections.Count; i++)
            {
                RuntimeCorridorConnection connection = connections[i];
                if (!TryResolveConnection(connection, settings, out DoorEndpoint from, out DoorEndpoint to))
                    continue;

                string label = string.IsNullOrWhiteSpace(connection.label)
                    ? $"{connection.fromRoomName}_to_{connection.toRoomName}"
                    : connection.label;

                Transform corridorRoot = new GameObject(SanitizeName(label)).transform;
                corridorRoot.SetParent(root, false);
                corridorRoot.localPosition = Vector3.zero;
                corridorRoot.localRotation = Quaternion.identity;
                corridorRoot.localScale = Vector3.one;

                float width = connection.width > 0.01f ? connection.width : settings.defaultWidth;
                float leadLength = connection.leadLength > 0.01f ? connection.leadLength : settings.defaultLeadLength;
                BuildCorridor(corridorRoot, from, to, width, leadLength, connection.pathMode, settings);
            }
        }

        public static List<RuntimeCorridorConnection> CreateConnectionsFromDoorNames(
            IReadOnlyList<RuntimeRoomLayout> rooms,
            string markerPrefix)
        {
            List<RuntimeCorridorConnection> connections = new List<RuntimeCorridorConnection>();
            if (rooms == null || rooms.Count == 0)
                return connections;

            markerPrefix = string.IsNullOrWhiteSpace(markerPrefix) ? "To " : markerPrefix;
            Dictionary<string, string> roomByKey = new Dictionary<string, string>();
            List<DoorMarkerInfo> doors = new List<DoorMarkerInfo>();

            for (int i = 0; i < rooms.Count; i++)
            {
                RuntimeRoomLayout room = rooms[i];
                if (room == null || string.IsNullOrWhiteSpace(room.roomName))
                    continue;

                if (!TryExtractRoomKey(room.roomName, out string roomKey))
                    continue;

                roomByKey[roomKey] = room.roomName;

                GameObject roomObject = GameObject.Find(room.roomName);
                if (roomObject == null)
                    continue;

                CollectDoorMarkers(roomObject.transform, markerPrefix, room.roomName, roomKey, doors);
            }

            HashSet<string> addedPairs = new HashSet<string>();
            for (int i = 0; i < doors.Count; i++)
            {
                DoorMarkerInfo door = doors[i];
                if (!roomByKey.TryGetValue(door.targetRoomKey, out string targetRoomName))
                    continue;

                string reciprocalDoorName = markerPrefix + door.roomKey;
                if (!DoorExists(targetRoomName, reciprocalDoorName))
                    continue;

                string pairKey = CreatePairKey(door.roomName, door.doorName, targetRoomName, reciprocalDoorName);
                if (!addedPairs.Add(pairKey))
                    continue;

                connections.Add(new RuntimeCorridorConnection
                {
                    label = $"{door.roomName}_to_{targetRoomName}",
                    fromRoomName = door.roomName,
                    fromDoorName = door.doorName,
                    toRoomName = targetRoomName,
                    toDoorName = reciprocalDoorName
                });
            }

            return connections;
        }

        private static CorridorBuildSettings Normalize(CorridorBuildSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.generatedRootName))
                settings.generatedRootName = DefaultGeneratedRootName;
            if (string.IsNullOrWhiteSpace(settings.markerPrefix))
                settings.markerPrefix = "To ";

            settings.defaultWidth = PositiveOr(settings.defaultWidth, 1.4f);
            settings.defaultLeadLength = PositiveOr(settings.defaultLeadLength, 0.7f);
            settings.minimumCorridorLength = Mathf.Max(settings.minimumCorridorLength, 0.25f);
            settings.wallHeight = PositiveOr(settings.wallHeight, 2.1f);
            settings.wallThickness = PositiveOr(settings.wallThickness, 0.15f);
            settings.floorThickness = PositiveOr(settings.floorThickness, 0.1f);
            return settings;
        }

        private static float PositiveOr(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }

        private static bool TryResolveConnection(
            RuntimeCorridorConnection connection,
            CorridorBuildSettings settings,
            out DoorEndpoint from,
            out DoorEndpoint to)
        {
            from = default;
            to = default;

            if (connection == null)
                return false;

            GameObject fromRoom = GameObject.Find(connection.fromRoomName);
            GameObject toRoom = GameObject.Find(connection.toRoomName);
            if (fromRoom == null || toRoom == null)
                return false;

            if (!TryFindDoorEndpoint(fromRoom.transform, connection.fromDoorName, settings.markerPrefix, out from))
                return false;

            if (!TryFindDoorEndpoint(toRoom.transform, connection.toDoorName, settings.markerPrefix, out to))
                return false;

            return true;
        }

        private static bool TryFindDoorEndpoint(Transform roomRoot, string doorName, string markerPrefix, out DoorEndpoint endpoint)
        {
            endpoint = default;

            if (roomRoot == null || string.IsNullOrWhiteSpace(doorName))
                return false;

            Transform marker = FindChildRecursive(roomRoot, doorName);
            if (marker == null && !doorName.StartsWith(markerPrefix))
                marker = FindChildRecursive(roomRoot, markerPrefix + doorName);
            if (marker == null)
                return false;

            WallSide wall = FindWallFromHierarchy(marker, roomRoot);
            endpoint = new DoorEndpoint
            {
                marker = marker,
                position = marker.position,
                normal = GetWorldNormal(roomRoot, wall)
            };

            return true;
        }

        private static void CollectDoorMarkers(
            Transform current,
            string markerPrefix,
            string roomName,
            string roomKey,
            List<DoorMarkerInfo> doors)
        {
            if (current == null)
                return;

            if (IsGeneratedRoot(current.name))
                return;

            if (TryExtractDoorTargetKey(current.name, markerPrefix, out string targetRoomKey))
            {
                doors.Add(new DoorMarkerInfo
                {
                    roomName = roomName,
                    roomKey = roomKey,
                    doorName = current.name,
                    targetRoomKey = targetRoomKey
                });
            }

            for (int i = 0; i < current.childCount; i++)
                CollectDoorMarkers(current.GetChild(i), markerPrefix, roomName, roomKey, doors);
        }

        private static bool DoorExists(string roomName, string doorName)
        {
            GameObject room = GameObject.Find(roomName);
            return room != null && FindChildRecursive(room.transform, doorName) != null;
        }

        private static string CreatePairKey(string fromRoom, string fromDoor, string toRoom, string toDoor)
        {
            string a = $"{fromRoom}/{fromDoor}";
            string b = $"{toRoom}/{toDoor}";
            return string.CompareOrdinal(a, b) < 0 ? $"{a}|{b}" : $"{b}|{a}";
        }

        private static bool TryExtractRoomKey(string roomName, out string key)
        {
            key = string.Empty;
            string[] parts = roomName.Split(' ', '-');
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                string candidate = parts[i].Trim();
                if (IsRoomKey(candidate))
                {
                    key = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractDoorTargetKey(string doorName, string markerPrefix, out string targetRoomKey)
        {
            targetRoomKey = string.Empty;
            if (string.IsNullOrWhiteSpace(doorName) || !doorName.StartsWith(markerPrefix))
                return false;

            string candidate = doorName.Substring(markerPrefix.Length).Trim();
            if (!IsRoomKey(candidate))
                return false;

            targetRoomKey = candidate;
            return true;
        }

        private static bool IsRoomKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            bool hasDigit = false;
            bool hasDot = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsDigit(character))
                {
                    hasDigit = true;
                    continue;
                }

                if (character == '.')
                {
                    hasDot = true;
                    continue;
                }

                return false;
            }

            return hasDigit && hasDot;
        }

        private static bool IsGeneratedRoot(string name)
        {
            return name == "GeneratedDoorways" || name == "GeneratedCorridors" || name == "DoorwaySegments";
        }

        private static Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root.name == targetName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildRecursive(root.GetChild(i), targetName);
                if (match != null)
                    return match;
            }

            return null;
        }

        private static WallSide FindWallFromHierarchy(Transform marker, Transform roomRoot)
        {
            Transform current = marker.parent;
            while (current != null && current != roomRoot.parent)
            {
                if (TryParseWallName(current.name, out WallSide wall))
                    return wall;

                if (current == roomRoot)
                    break;

                current = current.parent;
            }

            return FindNearestWallByPosition(marker, roomRoot);
        }

        private static WallSide FindNearestWallByPosition(Transform marker, Transform roomRoot)
        {
            Vector3 markerLocal = roomRoot.InverseTransformPoint(marker.position);
            WallSide closestWall = WallSide.North;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                WallSide wall = (WallSide)i;
                Transform wallRoot = roomRoot.Find(GetWallName(wall));
                if (wallRoot == null)
                    continue;

                Vector3 wallLocal = wallRoot.localPosition;
                float distance = wall == WallSide.East || wall == WallSide.West
                    ? Mathf.Abs(markerLocal.x - wallLocal.x)
                    : Mathf.Abs(markerLocal.z - wallLocal.z);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWall = wall;
                }
            }

            if (closestDistance < float.MaxValue)
                return closestWall;

            float north = Mathf.Abs(markerLocal.z);
            float east = Mathf.Abs(markerLocal.x);
            return east > north
                ? (markerLocal.x >= 0f ? WallSide.East : WallSide.West)
                : (markerLocal.z >= 0f ? WallSide.North : WallSide.South);
        }

        private static bool TryParseWallName(string name, out WallSide wall)
        {
            switch (name)
            {
                case "NorthWall":
                    wall = WallSide.North;
                    return true;
                case "EastWall":
                    wall = WallSide.East;
                    return true;
                case "SouthWall":
                    wall = WallSide.South;
                    return true;
                case "WestWall":
                    wall = WallSide.West;
                    return true;
                default:
                    wall = WallSide.North;
                    return false;
            }
        }

        private static string GetWallName(WallSide wall)
        {
            switch (wall)
            {
                case WallSide.North:
                    return "NorthWall";
                case WallSide.East:
                    return "EastWall";
                case WallSide.South:
                    return "SouthWall";
                case WallSide.West:
                    return "WestWall";
                default:
                    return string.Empty;
            }
        }

        private static Vector3 GetWorldNormal(Transform roomRoot, WallSide wall)
        {
            switch (wall)
            {
                case WallSide.North:
                    return roomRoot.forward;
                case WallSide.South:
                    return -roomRoot.forward;
                case WallSide.East:
                    return roomRoot.right;
                case WallSide.West:
                    return -roomRoot.right;
                default:
                    return roomRoot.forward;
            }
        }

        private static void BuildCorridor(
            Transform parent,
            DoorEndpoint from,
            DoorEndpoint to,
            float width,
            float leadLength,
            CorridorPathMode pathMode,
            CorridorBuildSettings settings)
        {
            List<Vector3> points = BuildPath(from, to, leadLength, pathMode, settings.minimumCorridorLength);
            for (int i = 0; i < points.Count - 1; i++)
                CreateSegment(parent, points[i], points[i + 1], width, settings, i);
        }

        private static List<Vector3> BuildPath(DoorEndpoint from, DoorEndpoint to, float leadLength, CorridorPathMode pathMode, float minimumCorridorLength)
        {
            Vector3 start = Flatten(from.position);
            Vector3 end = Flatten(to.position);
            float doorDistance = Vector3.Distance(start, end);
            if (doorDistance <= minimumCorridorLength)
                return new List<Vector3>();

            Vector3 fromNormal = OrientNormalToward(from.normal, end - start);
            Vector3 toNormal = OrientNormalToward(to.normal, start - end);
            float resolvedLeadLength = Mathf.Min(leadLength, doorDistance * 0.45f);
            Vector3 startLead = Flatten(from.position + fromNormal * resolvedLeadLength);
            Vector3 endLead = Flatten(to.position + toNormal * resolvedLeadLength);

            List<Vector3> points = new List<Vector3> { start, startLead };

            CorridorPathMode resolvedMode = ResolvePathMode(pathMode, fromNormal, startLead, endLead);
            if (resolvedMode == CorridorPathMode.XThenZ)
                points.Add(new Vector3(endLead.x, 0f, startLead.z));
            else if (resolvedMode == CorridorPathMode.ZThenX)
                points.Add(new Vector3(startLead.x, 0f, endLead.z));

            points.Add(endLead);
            points.Add(end);

            return RemoveShortSegments(points);
        }

        private static Vector3 OrientNormalToward(Vector3 normal, Vector3 targetDirection)
        {
            Vector3 flatNormal = Flatten(normal).normalized;
            Vector3 flatTargetDirection = Flatten(targetDirection).normalized;
            if (flatNormal.sqrMagnitude < 0.001f)
                return flatTargetDirection.sqrMagnitude > 0.001f ? flatTargetDirection : Vector3.forward;

            if (flatTargetDirection.sqrMagnitude < 0.001f)
                return flatNormal;

            return Vector3.Dot(flatNormal, flatTargetDirection) < 0f ? -flatNormal : flatNormal;
        }

        private static CorridorPathMode ResolvePathMode(CorridorPathMode mode, Vector3 fromNormal, Vector3 startLead, Vector3 endLead)
        {
            if (mode != CorridorPathMode.Auto)
                return mode;

            bool alignedX = Mathf.Abs(startLead.z - endLead.z) < 0.05f;
            bool alignedZ = Mathf.Abs(startLead.x - endLead.x) < 0.05f;
            if (alignedX || alignedZ)
                return CorridorPathMode.Straight;

            return Mathf.Abs(fromNormal.x) > Mathf.Abs(fromNormal.z)
                ? CorridorPathMode.XThenZ
                : CorridorPathMode.ZThenX;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }

        private static List<Vector3> RemoveShortSegments(List<Vector3> points)
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
            {
                if (result.Count == 0 || Vector3.Distance(result[result.Count - 1], points[i]) > MinimumSegmentLength)
                    result.Add(points[i]);
            }

            return result;
        }

        private static void CreateSegment(Transform parent, Vector3 start, Vector3 end, float width, CorridorBuildSettings settings, int index)
        {
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length <= MinimumSegmentLength)
                return;

            Vector3 direction = delta.normalized;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 center = (start + end) * 0.5f;

            CreateFloor(parent, center, rotation, length, width, settings, index);
            CreateSideWalls(parent, center, rotation, side, length, width, settings, index);

            if (settings.createCeilings)
                CreateCeiling(parent, center, rotation, length, width, settings, index);
        }

        private static void CreateFloor(Transform parent, Vector3 center, Quaternion rotation, float length, float width, CorridorBuildSettings settings, int index)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = $"Corridor_{index}_Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(center.x, -settings.floorThickness * 0.5f, center.z);
            floor.transform.rotation = rotation;
            floor.transform.localScale = new Vector3(width, settings.floorThickness, length);
            ApplyMaterial(floor, settings.floorMaterial);
        }

        private static void CreateSideWalls(Transform parent, Vector3 center, Quaternion rotation, Vector3 side, float length, float width, CorridorBuildSettings settings, int index)
        {
            float sideOffset = width * 0.5f;
            Vector3 wallA = center + side * sideOffset;
            Vector3 wallB = center - side * sideOffset;
            wallA.y = settings.wallHeight * 0.5f;
            wallB.y = settings.wallHeight * 0.5f;

            CreateWall(parent, $"Corridor_{index}_WallA", wallA, rotation, new Vector3(settings.wallThickness, settings.wallHeight, length), settings.wallMaterial);
            CreateWall(parent, $"Corridor_{index}_WallB", wallB, rotation, new Vector3(settings.wallThickness, settings.wallHeight, length), settings.wallMaterial);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;
            wall.transform.rotation = rotation;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, material);
        }

        private static void CreateCeiling(Transform parent, Vector3 center, Quaternion rotation, float length, float width, CorridorBuildSettings settings, int index)
        {
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = $"Corridor_{index}_Ceiling";
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.position = new Vector3(center.x, settings.wallHeight + settings.floorThickness * 0.5f, center.z);
            ceiling.transform.rotation = rotation;
            ceiling.transform.localScale = new Vector3(width, settings.floorThickness, length);
            ApplyMaterial(ceiling, settings.wallMaterial);
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            if (material == null)
                return;

            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        private static string SanitizeName(string value)
        {
            return value.Replace("/", "_").Replace("\\", "_").Replace(":", "_").Trim();
        }

        private static void ClearGeneratedCorridors(Transform parent, string generatedRootName)
        {
            Transform existing = parent.Find(generatedRootName);
            if (existing == null)
                return;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(existing.gameObject);
            else
                Object.Destroy(existing.gameObject);
            #else
            Object.Destroy(existing.gameObject);
            #endif
        }
    }
}
