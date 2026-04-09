using System.Collections.Generic;
using UnityEngine;

namespace MuseumGame.Blockout
{
    public enum WallSide
    {
        North,
        East,
        South,
        West
    }

    [System.Serializable]
    public class BlockoutDoorMarker
    {
        public string label = "Door";
        public WallSide wall = WallSide.North;
        [Range(0.1f, 0.9f)] public float normalizedOffset = 0.5f;
    }

    [System.Serializable]
    public class BlockoutRoomDefinition
    {
        public string label;
        public MuseumRoomId roomId = MuseumRoomId.None;
        public Vector2 position;
        public Vector2 size = new Vector2(4f, 4f);
        public List<BlockoutDoorMarker> doorMarkers = new List<BlockoutDoorMarker>();
    }

    public class MuseumBlockoutGenerator : MonoBehaviour
    {
        [Header("Blockout Settings")]
        public float wallHeight = 2.8f;
        public float wallThickness = 0.15f;
        public float floorThickness = 0.1f;
        public float markerHeight = 2f;
        public Material sharedMaterial;

        [Header("Generated Layout")]
        public List<BlockoutRoomDefinition> rooms = new List<BlockoutRoomDefinition>();

        [ContextMenu("Apply Recommended Layout")]
        public void ApplyRecommendedLayout()
        {
            rooms = new List<BlockoutRoomDefinition>
            {
                new BlockoutRoomDefinition
                {
                    label = "Lobby 1.1",
                    roomId = MuseumRoomId.Lobby,
                    position = new Vector2(4.8f, 4.0f),
                    size = new Vector2(1.8f, 2.6f),
                    doorMarkers = new List<BlockoutDoorMarker>
                    {
                        new BlockoutDoorMarker { label = "To 1.2", wall = WallSide.West, normalizedOffset = 0.55f },
                        new BlockoutDoorMarker { label = "To 1.3", wall = WallSide.South, normalizedOffset = 0.55f }
                    }
                },
                new BlockoutRoomDefinition
                {
                    label = "Sala 2 - 1.2",
                    roomId = MuseumRoomId.Reconstruction,
                    position = new Vector2(1.95f, 4.0f),
                    size = new Vector2(3.9f, 2.6f),
                    doorMarkers = new List<BlockoutDoorMarker>
                    {
                        new BlockoutDoorMarker { label = "To 1.1", wall = WallSide.East, normalizedOffset = 0.55f },
                        new BlockoutDoorMarker { label = "To 1.3", wall = WallSide.South, normalizedOffset = 0.08f }
                    }
                },
                new BlockoutRoomDefinition
                {
                    label = "Sala 3 - 1.3",
                    roomId = MuseumRoomId.Conversations,
                    position = new Vector2(2.25f, 0.0f),
                    size = new Vector2(3.6f, 2.6f),
                    doorMarkers = new List<BlockoutDoorMarker>
                    {
                        new BlockoutDoorMarker { label = "To 1.1", wall = WallSide.North, normalizedOffset = 0.82f },
                        new BlockoutDoorMarker { label = "To 1.2", wall = WallSide.North, normalizedOffset = 0.06f },
                        new BlockoutDoorMarker { label = "To 1.5", wall = WallSide.East, normalizedOffset = 0.5f }
                    }
                },
                new BlockoutRoomDefinition
                {
                    label = "Sala 4 - 1.5",
                    roomId = MuseumRoomId.Lights,
                    position = new Vector2(5.4f, 0.0f),
                    size = new Vector2(2.7f, 2.6f),
                    doorMarkers = new List<BlockoutDoorMarker>
                    {
                        new BlockoutDoorMarker { label = "To 1.3", wall = WallSide.West, normalizedOffset = 0.5f },
                        new BlockoutDoorMarker { label = "To 1.13", wall = WallSide.North, normalizedOffset = 0.5f }
                    }
                },
                new BlockoutRoomDefinition
                {
                    label = "Sala 5 - 1.13",
                    roomId = MuseumRoomId.Minigames,
                    position = new Vector2(5.4f, 7.2f),
                    size = new Vector2(1.8f, 2.6f),
                    doorMarkers = new List<BlockoutDoorMarker>
                    {
                        new BlockoutDoorMarker { label = "To 1.5", wall = WallSide.South, normalizedOffset = 0.5f }
                    }
                }
            };
        }

        [ContextMenu("Generate Blockout")]
        public void GenerateBlockout()
        {
            ClearBlockout();

            Transform root = new GameObject("GeneratedBlockout").transform;
            root.SetParent(transform, false);

            foreach (BlockoutRoomDefinition room in rooms)
            {
                CreateRoom(root, room);
            }
        }

        [ContextMenu("Clear Blockout")]
        public void ClearBlockout()
        {
            Transform existing = transform.Find("GeneratedBlockout");
            if (existing == null)
                return;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
                Destroy(existing.gameObject);
            #else
            Destroy(existing.gameObject);
            #endif
        }

        private void Reset()
        {
            ApplyRecommendedLayout();
        }

        private void CreateRoom(Transform root, BlockoutRoomDefinition room)
        {
            Transform roomRoot = new GameObject(room.label).transform;
            roomRoot.SetParent(root, false);

            Vector3 center = new Vector3(room.position.x, 0f, room.position.y);

            CreatePrimitive(
                roomRoot,
                "Floor",
                center + new Vector3(0f, -floorThickness * 0.5f, 0f),
                new Vector3(room.size.x, floorThickness, room.size.y));

            CreatePrimitive(
                roomRoot,
                "NorthWall",
                center + new Vector3(0f, wallHeight * 0.5f, room.size.y * 0.5f),
                new Vector3(room.size.x, wallHeight, wallThickness));

            CreatePrimitive(
                roomRoot,
                "SouthWall",
                center + new Vector3(0f, wallHeight * 0.5f, -room.size.y * 0.5f),
                new Vector3(room.size.x, wallHeight, wallThickness));

            CreatePrimitive(
                roomRoot,
                "EastWall",
                center + new Vector3(room.size.x * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, room.size.y));

            CreatePrimitive(
                roomRoot,
                "WestWall",
                center + new Vector3(-room.size.x * 0.5f, wallHeight * 0.5f, 0f),
                new Vector3(wallThickness, wallHeight, room.size.y));

            CreateMarker(roomRoot, room.label + " Marker", center + new Vector3(0f, 0.01f, 0f));

            foreach (BlockoutDoorMarker marker in room.doorMarkers)
            {
                CreateDoorMarker(roomRoot, room, marker);
            }
        }

        private void CreatePrimitive(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;

            if (sharedMaterial != null)
            {
                MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = sharedMaterial;
            }
        }

        private void CreateMarker(Transform parent, string objectName, Vector3 localPosition)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = objectName;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            if (sharedMaterial != null)
            {
                MeshRenderer renderer = marker.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = sharedMaterial;
            }
        }

        private void CreateDoorMarker(Transform parent, BlockoutRoomDefinition room, BlockoutDoorMarker marker)
        {
            Vector3 position = GetDoorMarkerPosition(room, marker);

            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markerObject.name = marker.label;
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.localPosition = position;
            markerObject.transform.localScale = new Vector3(0.8f, markerHeight, 0.08f);

            if (marker.wall == WallSide.East || marker.wall == WallSide.West)
                markerObject.transform.localScale = new Vector3(0.08f, markerHeight, 0.8f);

            Collider collider = markerObject.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;

            if (sharedMaterial != null)
            {
                MeshRenderer renderer = markerObject.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = sharedMaterial;
            }
        }

        private Vector3 GetDoorMarkerPosition(BlockoutRoomDefinition room, BlockoutDoorMarker marker)
        {
            float xHalf = room.size.x * 0.5f;
            float zHalf = room.size.y * 0.5f;
            float x = Mathf.Lerp(-xHalf, xHalf, marker.normalizedOffset);
            float z = Mathf.Lerp(-zHalf, zHalf, marker.normalizedOffset);

            switch (marker.wall)
            {
                case WallSide.North:
                    return new Vector3(x, markerHeight * 0.5f, zHalf);
                case WallSide.South:
                    return new Vector3(x, markerHeight * 0.5f, -zHalf);
                case WallSide.East:
                    return new Vector3(xHalf, markerHeight * 0.5f, z);
                case WallSide.West:
                    return new Vector3(-xHalf, markerHeight * 0.5f, z);
                default:
                    return Vector3.zero;
            }
        }
    }
}
