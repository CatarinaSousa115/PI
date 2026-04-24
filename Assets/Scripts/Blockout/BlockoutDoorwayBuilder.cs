using System.Collections.Generic;
using UnityEngine;

namespace MuseumGame.Blockout
{
    public struct DoorwayBuildSettings
    {
        public string generatedRootName;
        public string markerPrefix;
        public bool createDoorLeaves;
        public bool hideDoorMarkerRenderers;
        public float wallHeight;
        public float wallThickness;
        public float markerHeight;
        public float doorwayHeight;
        public float closedDoorThickness;
        public float doorLeafPlaneOffset;
        public float doorwayClearancePadding;
        public float doorwayGroundClearance;
        public float minimumDoorwayWidth;
        public float minimumDoorwayHeight;
        public Color doorColor;
        public Material doorMaterial;
    }

    public static class BlockoutDoorwayBuilder
    {
        private const string DefaultGeneratedRootName = "GeneratedDoorways";
        private const string LegacySegmentRootName = "DoorwaySegments";
        private const string DoorLeafSuffix = "_Leaf";
        private const float MinimumSegmentLength = 0.02f;

        private struct DoorwayInfo
        {
            public Transform marker;
            public WallSide wall;
            public float center;
            public float width;
            public float height;
            public float start;
            public float end;
        }

        public static void RebuildRoom(Transform roomRoot, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            if (roomRoot == null)
                return;

            settings = Normalize(settings);
            ClearGeneratedParts(roomRoot, settings.generatedRootName);
            SetAllWallsVisible(roomRoot, true);

            List<DoorwayInfo> doorways = CollectDoorways(roomRoot, roomSize, settings);
            if (doorways.Count == 0)
                return;

            Transform generatedRoot = new GameObject(settings.generatedRootName).transform;
            generatedRoot.SetParent(roomRoot, false);
            generatedRoot.localPosition = Vector3.zero;
            generatedRoot.localRotation = Quaternion.identity;
            generatedRoot.localScale = Vector3.one;

            for (int i = 0; i < 4; i++)
            {
                WallSide wall = (WallSide)i;
                List<DoorwayInfo> wallDoorways = doorways.FindAll(d => d.wall == wall);
                if (wallDoorways.Count == 0)
                    continue;

                Transform sourceWall = roomRoot.Find(GetWallName(wall));
                SetWallVisible(sourceWall, false);
                CreateWallSegments(generatedRoot, sourceWall, wall, wallDoorways, roomSize, settings);
            }

            foreach (DoorwayInfo doorway in doorways)
            {
                SetMarkerRendererVisible(doorway.marker, !settings.hideDoorMarkerRenderers);

                if (settings.createDoorLeaves)
                    CreateDoorLeaf(generatedRoot, doorway, roomSize, settings);
            }
        }

        private static DoorwayBuildSettings Normalize(DoorwayBuildSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.generatedRootName))
                settings.generatedRootName = DefaultGeneratedRootName;
            if (string.IsNullOrWhiteSpace(settings.markerPrefix))
                settings.markerPrefix = "To ";

            settings.wallHeight = PositiveOr(settings.wallHeight, 2.1f);
            settings.wallThickness = PositiveOr(settings.wallThickness, 0.15f);
            settings.markerHeight = PositiveOr(settings.markerHeight, 2f);
            settings.doorwayHeight = PositiveOr(settings.doorwayHeight, 2.1f);
            settings.closedDoorThickness = PositiveOr(settings.closedDoorThickness, 0.18f);
            settings.doorwayClearancePadding = Mathf.Max(0f, settings.doorwayClearancePadding);
            settings.doorwayGroundClearance = Mathf.Max(0f, settings.doorwayGroundClearance);
            settings.minimumDoorwayWidth = PositiveOr(settings.minimumDoorwayWidth, 1.15f);
            settings.minimumDoorwayHeight = PositiveOr(settings.minimumDoorwayHeight, 2.1f);

            if (settings.doorColor.a <= 0f)
                settings.doorColor = new Color(0.82f, 0.82f, 0.86f, 1f);

            return settings;
        }

        private static float PositiveOr(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }

        private static List<DoorwayInfo> CollectDoorways(Transform roomRoot, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            List<DoorwayInfo> doorways = new List<DoorwayInfo>();

            for (int i = 0; i < roomRoot.childCount; i++)
            {
                Transform child = roomRoot.GetChild(i);
                if (!IsDoorMarker(child, settings))
                    continue;

                DoorwayInfo doorway = BuildDoorwayInfo(roomRoot, child, roomSize, settings, null);
                doorways.Add(doorway);
            }

            for (int i = 0; i < 4; i++)
            {
                WallSide wall = (WallSide)i;
                Transform wallRoot = roomRoot.Find(GetWallName(wall));
                if (wallRoot == null)
                    continue;

                for (int childIndex = 0; childIndex < wallRoot.childCount; childIndex++)
                {
                    Transform child = wallRoot.GetChild(childIndex);
                    if (!IsDoorMarker(child, settings))
                        continue;

                    DoorwayInfo doorway = BuildDoorwayInfo(roomRoot, child, roomSize, settings, wall);
                    doorways.Add(doorway);
                }
            }

            return doorways;
        }

        private static bool IsDoorMarker(Transform child, DoorwayBuildSettings settings)
        {
            if (child == null)
                return false;
            if (child.name == settings.generatedRootName || child.name == LegacySegmentRootName)
                return false;
            if (child.name.EndsWith(DoorLeafSuffix))
                return false;

            return child.name.StartsWith(settings.markerPrefix);
        }

        private static DoorwayInfo BuildDoorwayInfo(Transform roomRoot, Transform marker, Vector2 roomSize, DoorwayBuildSettings settings, WallSide? explicitWall)
        {
            Vector3 roomLocalPosition = roomRoot.InverseTransformPoint(marker.position);
            WallSide wall = explicitWall ?? FindNearestWall(roomLocalPosition, roomSize);
            float wallLength = GetWallLength(wall, roomSize);
            float markerWidth = GetMarkerWidth(roomRoot, marker, wall);
            float openingWidth = Mathf.Max(settings.minimumDoorwayWidth, markerWidth + settings.doorwayClearancePadding);
            openingWidth = Mathf.Min(openingWidth, Mathf.Max(MinimumSegmentLength, wallLength - MinimumSegmentLength));

            float halfWidth = openingWidth * 0.5f;
            float center = GetWallAxisPosition(roomLocalPosition, wall);
            center = Mathf.Clamp(center, -wallLength * 0.5f + halfWidth, wallLength * 0.5f - halfWidth);

            float markerHeight = GetMarkerHeight(roomRoot, marker);
            float openingHeight = Mathf.Max(settings.doorwayHeight, settings.minimumDoorwayHeight, markerHeight);
            openingHeight = Mathf.Min(openingHeight, settings.wallHeight);

            SnapMarkerToWall(roomRoot, marker, wall, center, roomSize, settings);

            return new DoorwayInfo
            {
                marker = marker,
                wall = wall,
                center = center,
                width = openingWidth,
                height = openingHeight,
                start = center - halfWidth,
                end = center + halfWidth
            };
        }

        private static WallSide FindNearestWall(Vector3 localPosition, Vector2 roomSize)
        {
            float xHalf = roomSize.x * 0.5f;
            float zHalf = roomSize.y * 0.5f;
            float north = Mathf.Abs(localPosition.z - zHalf);
            float south = Mathf.Abs(localPosition.z + zHalf);
            float east = Mathf.Abs(localPosition.x - xHalf);
            float west = Mathf.Abs(localPosition.x + xHalf);

            float closest = north;
            WallSide side = WallSide.North;

            if (east < closest)
            {
                closest = east;
                side = WallSide.East;
            }

            if (south < closest)
            {
                closest = south;
                side = WallSide.South;
            }

            if (west < closest)
                side = WallSide.West;

            return side;
        }

        private static float GetMarkerWidth(Transform roomRoot, Transform marker, WallSide wall)
        {
            Vector3 scale = GetMarkerSizeInRoomSpace(roomRoot, marker);
            return wall == WallSide.East || wall == WallSide.West
                ? Mathf.Abs(scale.z)
                : Mathf.Abs(scale.x);
        }

        private static float GetMarkerHeight(Transform roomRoot, Transform marker)
        {
            return Mathf.Abs(GetMarkerSizeInRoomSpace(roomRoot, marker).y);
        }

        private static Vector3 GetMarkerSizeInRoomSpace(Transform roomRoot, Transform marker)
        {
            Vector3 markerScale = marker.lossyScale;
            Vector3 roomScale = roomRoot.lossyScale;

            return new Vector3(
                DivideByScale(markerScale.x, roomScale.x),
                DivideByScale(markerScale.y, roomScale.y),
                DivideByScale(markerScale.z, roomScale.z));
        }

        private static float DivideByScale(float value, float scale)
        {
            return Mathf.Abs(scale) > 0.0001f ? value / scale : value;
        }

        private static float GetWallAxisPosition(Vector3 localPosition, WallSide wall)
        {
            return wall == WallSide.East || wall == WallSide.West ? localPosition.z : localPosition.x;
        }

        private static void SnapMarkerToWall(Transform roomRoot, Transform marker, WallSide wall, float center, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            float xHalf = roomSize.x * 0.5f;
            float zHalf = roomSize.y * 0.5f;
            float markerHeight = PositiveOr(GetMarkerHeight(roomRoot, marker), settings.markerHeight);
            Vector3 position = roomRoot.InverseTransformPoint(marker.position);
            position.y = markerHeight * 0.5f;

            switch (wall)
            {
                case WallSide.North:
                    position.x = center;
                    position.z = zHalf;
                    break;
                case WallSide.South:
                    position.x = center;
                    position.z = -zHalf;
                    break;
                case WallSide.East:
                    position.x = xHalf;
                    position.z = center;
                    break;
                case WallSide.West:
                    position.x = -xHalf;
                    position.z = center;
                    break;
            }

            marker.position = roomRoot.TransformPoint(position);
        }

        private static void CreateWallSegments(Transform parent, Transform sourceWall, WallSide wall, List<DoorwayInfo> doorways, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            doorways.Sort((a, b) => a.center.CompareTo(b.center));

            float wallLength = GetWallLength(wall, roomSize);
            float cursor = -wallLength * 0.5f;

            for (int i = 0; i < doorways.Count; i++)
            {
                DoorwayInfo doorway = doorways[i];
                CreateWallSegment(parent, sourceWall, wall, cursor, doorway.start, roomSize, settings, $"{GetWallName(wall)}_Segment_{i}_A");
                CreateLintel(parent, sourceWall, doorway, roomSize, settings);
                cursor = Mathf.Max(cursor, doorway.end);
            }

            CreateWallSegment(parent, sourceWall, wall, cursor, wallLength * 0.5f, roomSize, settings, $"{GetWallName(wall)}_Segment_End");
        }

        private static void CreateWallSegment(Transform parent, Transform sourceWall, WallSide wall, float start, float end, Vector2 roomSize, DoorwayBuildSettings settings, string name)
        {
            float length = end - start;
            if (length <= MinimumSegmentLength)
                return;

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, false);
            segment.transform.localPosition = GetWallPartPosition(wall, start + length * 0.5f, settings.wallHeight * 0.5f, roomSize);
            segment.transform.localScale = GetWallPartScale(wall, length, settings.wallHeight, settings.wallThickness);
            ApplyWallMaterial(segment, sourceWall);
        }

        private static void CreateLintel(Transform parent, Transform sourceWall, DoorwayInfo doorway, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            float lintelHeight = settings.wallHeight - doorway.height;
            if (lintelHeight <= MinimumSegmentLength)
                return;

            GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = $"{doorway.marker.name}_Lintel";
            lintel.transform.SetParent(parent, false);
            lintel.transform.localPosition = GetWallPartPosition(doorway.wall, doorway.center, doorway.height + lintelHeight * 0.5f, roomSize);
            lintel.transform.localScale = GetWallPartScale(doorway.wall, doorway.width, lintelHeight, settings.wallThickness);
            ApplyWallMaterial(lintel, sourceWall);
        }

        private static void CreateDoorLeaf(Transform parent, DoorwayInfo doorway, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorLeaf.name = doorway.marker.name + DoorLeafSuffix;
            doorLeaf.transform.SetParent(parent, false);

            float height = Mathf.Max(0.01f, doorway.height - settings.doorwayGroundClearance);
            doorLeaf.transform.localPosition = GetDoorLeafPosition(doorway, height, roomSize, settings);
            doorLeaf.transform.localScale = doorway.wall == WallSide.East || doorway.wall == WallSide.West
                ? new Vector3(settings.closedDoorThickness, height, doorway.width)
                : new Vector3(doorway.width, height, settings.closedDoorThickness);

            MeshRenderer renderer = doorLeaf.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = settings.doorMaterial != null ? settings.doorMaterial : CreateDoorMaterial(settings.doorColor);
        }

        private static Vector3 GetDoorLeafPosition(DoorwayInfo doorway, float doorLeafHeight, Vector2 roomSize, DoorwayBuildSettings settings)
        {
            Vector3 position = GetWallPartPosition(doorway.wall, doorway.center, settings.doorwayGroundClearance + doorLeafHeight * 0.5f, roomSize);
            float offset = settings.doorLeafPlaneOffset;

            switch (doorway.wall)
            {
                case WallSide.North:
                    position.z -= offset;
                    break;
                case WallSide.South:
                    position.z += offset;
                    break;
                case WallSide.East:
                    position.x -= offset;
                    break;
                case WallSide.West:
                    position.x += offset;
                    break;
            }

            return position;
        }

        private static Vector3 GetWallPartPosition(WallSide wall, float axisCenter, float y, Vector2 roomSize)
        {
            float xHalf = roomSize.x * 0.5f;
            float zHalf = roomSize.y * 0.5f;

            switch (wall)
            {
                case WallSide.North:
                    return new Vector3(axisCenter, y, zHalf);
                case WallSide.South:
                    return new Vector3(axisCenter, y, -zHalf);
                case WallSide.East:
                    return new Vector3(xHalf, y, axisCenter);
                case WallSide.West:
                    return new Vector3(-xHalf, y, axisCenter);
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 GetWallPartScale(WallSide wall, float length, float height, float thickness)
        {
            return wall == WallSide.East || wall == WallSide.West
                ? new Vector3(thickness, height, length)
                : new Vector3(length, height, thickness);
        }

        private static float GetWallLength(WallSide wall, Vector2 roomSize)
        {
            return wall == WallSide.East || wall == WallSide.West ? roomSize.y : roomSize.x;
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

        private static void SetAllWallsVisible(Transform roomRoot, bool visible)
        {
            SetWallVisible(roomRoot.Find("NorthWall"), visible);
            SetWallVisible(roomRoot.Find("EastWall"), visible);
            SetWallVisible(roomRoot.Find("SouthWall"), visible);
            SetWallVisible(roomRoot.Find("WestWall"), visible);
        }

        private static void SetWallVisible(Transform wall, bool visible)
        {
            if (wall == null)
                return;

            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = visible;

            Collider collider = wall.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = visible;
        }

        private static void SetMarkerRendererVisible(Transform marker, bool visible)
        {
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = visible;
        }

        private static void ApplyWallMaterial(GameObject target, Transform sourceWall)
        {
            MeshRenderer sourceRenderer = sourceWall != null ? sourceWall.GetComponent<MeshRenderer>() : null;
            MeshRenderer targetRenderer = target.GetComponent<MeshRenderer>();
            if (sourceRenderer != null && targetRenderer != null)
                targetRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        private static Material CreateDoorMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private static void ClearGeneratedParts(Transform roomRoot, string generatedRootName)
        {
            List<GameObject> objectsToDelete = new List<GameObject>();

            for (int i = 0; i < roomRoot.childCount; i++)
            {
                Transform child = roomRoot.GetChild(i);
                if (child.name == generatedRootName || child.name == LegacySegmentRootName || child.name.EndsWith(DoorLeafSuffix))
                    objectsToDelete.Add(child.gameObject);
            }

            string[] wallNames = { "NorthWall", "EastWall", "SouthWall", "WestWall" };
            foreach (string wallName in wallNames)
            {
                Transform wall = roomRoot.Find(wallName);
                Transform legacySegments = wall != null ? wall.Find(LegacySegmentRootName) : null;
                if (legacySegments != null)
                    objectsToDelete.Add(legacySegments.gameObject);
            }

            foreach (GameObject target in objectsToDelete)
            {
                if (target != null)
                    DestroyObject(target);
            }
        }

        private static void DestroyObject(GameObject target)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(target);
            else
                Object.Destroy(target);
            #else
            Object.Destroy(target);
            #endif
        }
    }
}
