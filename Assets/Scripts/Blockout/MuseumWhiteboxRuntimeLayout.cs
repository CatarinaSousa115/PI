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
            public float wallHeight = 2.8f;
            public float wallThickness = 0.15f;
            public float floorThickness = 0.1f;
            public List<RuntimeRoomLayout> rooms = new List<RuntimeRoomLayout>();

            [Header("Doorways")]
            public string lobbyRoomName = "Lobby 1.1";
            public string lobbyWallName = "WestWall";
            public string lobbyDoorName = "To 1.2";
            public string room2Name = "Sala 2 - 1.2";
            public string room2WallName = "EastWall";
            public string room2DoorName = "To 1.1";
            public float doorwayHeight = 2.1f;
            public float doorwayLintelHeight = 0.28f;
            public float closedDoorThickness = 0.18f;
            public float doorLeafPlaneOffset = 0.02f;
            public float doorwayClearancePadding = 0.35f;
            public float doorwayGroundClearance = 0.08f;
            public float minimumDoorwayWidth = 1.15f;
            public float minimumDoorwayHeight = 2.3f;
            public Color doorColor = new Color(0.82f, 0.82f, 0.86f, 1f);

            [Header("Objects")]
            public string domPedroName = "DPedro";
            public string lobbyMarkerName = "Lobby 1.1 Marker";
            public string room2MarkerName = "Sala 2 - 1.2 Marker";
            public string tpLobbyName = "TP_Lobby";
            public string tpRoom2Name = "TP_Sala2";
            public string tpRoom3Name = "TP_Sala3";
            public string tpRoom4Name = "TP_Sala4";
            public string tpRoom5Name = "TP_Sala5";

            void Awake()
            {
                EnsureDefaults();
                ApplyLayouts();
                RefreshDoorMarkerPosition(lobbyRoomName, lobbyDoorName);
                RefreshDoorMarkerPosition(room2Name, room2DoorName);
                RebuildDoorway(lobbyRoomName, lobbyWallName, lobbyDoorName);
                RebuildDoorway(room2Name, room2WallName, room2DoorName);
                RepositionDomPedro();
                AlignTeleportPoint(tpLobbyName, lobbyRoomName, lobbyMarkerName);
                AlignTeleportPoint(tpRoom2Name, room2Name, room2MarkerName);
                AlignTeleportPoint(tpRoom3Name, "Sala 3 - 1.3", "Sala 3 - 1.3 Marker");
                AlignTeleportPoint(tpRoom4Name, "Sala 4 - 1.5", "Sala 4 - 1.5 Marker");
                AlignTeleportPoint(tpRoom5Name, "Sala 5 - 1.13", "Sala 5 - 1.13 Marker");
            }

            private void EnsureDefaults()
            {
                if (rooms.Count > 0)
                    return;

                rooms = new List<RuntimeRoomLayout>
                {
                    new RuntimeRoomLayout { roomName = "Lobby 1.1", center = new Vector2(5.7f, 6.6f), size = new Vector2(3.6f, 5.2f) },
                    new RuntimeRoomLayout { roomName = "Sala 2 - 1.2", center = new Vector2(0f, 6.6f), size = new Vector2(7.8f, 5.2f) },
                    new RuntimeRoomLayout { roomName = "Sala 3 - 1.3", center = new Vector2(0.45f, 0f), size = new Vector2(7.2f, 5.2f) },
                    new RuntimeRoomLayout { roomName = "Sala 4 - 1.5", center = new Vector2(6.75f, 0f), size = new Vector2(5.4f, 5.2f) },
                    new RuntimeRoomLayout { roomName = "Sala 5 - 1.13", center = new Vector2(6.75f, 12.4f), size = new Vector2(3.6f, 5.2f) }
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
                SetPrimitive(roomRoot, "Floor", new Vector3(center.x, -floorThickness * 0.5f, center.y), new Vector3(size.x, floorThickness, size.y));
                SetPrimitive(roomRoot, "NorthWall", new Vector3(center.x, wallHeight * 0.5f, center.y + size.y * 0.5f), new Vector3(size.x, wallHeight, wallThickness));
                SetPrimitive(roomRoot, "SouthWall", new Vector3(center.x, wallHeight * 0.5f, center.y - size.y * 0.5f), new Vector3(size.x, wallHeight, wallThickness));
                SetPrimitive(roomRoot, "EastWall", new Vector3(center.x + size.x * 0.5f, wallHeight * 0.5f, center.y), new Vector3(wallThickness, wallHeight, size.y));
                SetPrimitive(roomRoot, "WestWall", new Vector3(center.x - size.x * 0.5f, wallHeight * 0.5f, center.y), new Vector3(wallThickness, wallHeight, size.y));
                SetMarker(roomRoot, roomRoot.name + " Marker", new Vector3(center.x, 0.01f, center.y));
                RepositionDoorMarkers(roomRoot, center, size);
            }

            private void RepositionDoorMarkers(Transform roomRoot, Vector2 center, Vector2 size)
            {
                for (int i = 0; i < roomRoot.childCount; i++)
                {
                    Transform child = roomRoot.GetChild(i);
                    if (!child.name.StartsWith("To "))
                        continue;

                    if (!TryGetDoorLayout(roomRoot.name, child.name, out WallSide wall, out float normalizedOffset))
                        continue;

                    if (wall == WallSide.East || wall == WallSide.West)
                    {
                        float x = wall == WallSide.East ? center.x + size.x * 0.5f : center.x - size.x * 0.5f;
                        float z = Mathf.Lerp(center.y - size.y * 0.5f, center.y + size.y * 0.5f, normalizedOffset);
                        child.position = new Vector3(x, child.position.y, z);
                    }
                    else
                    {
                        float x = Mathf.Lerp(center.x - size.x * 0.5f, center.x + size.x * 0.5f, normalizedOffset);
                        float z = wall == WallSide.North ? center.y + size.y * 0.5f : center.y - size.y * 0.5f;
                        child.position = new Vector3(x, child.position.y, z);
                    }
                }
            }

            private void RefreshDoorMarkerPosition(string roomName, string doorName)
            {
                GameObject room = GameObject.Find(roomName);
                if (room == null)
                    return;

                RuntimeRoomLayout layout = rooms.Find(r => r.roomName == roomName);
                if (layout == null)
                    return;

                Transform door = room.transform.Find(doorName);
                if (door == null)
                    return;

                if (!TryGetDoorLayout(roomName, doorName, out WallSide wall, out float normalizedOffset))
                    return;

                Vector2 center = layout.center;
                Vector2 size = layout.size;

                if (wall == WallSide.East || wall == WallSide.West)
                {
                    float x = wall == WallSide.East ? center.x + size.x * 0.5f : center.x - size.x * 0.5f;
                    float z = Mathf.Lerp(center.y - size.y * 0.5f, center.y + size.y * 0.5f, normalizedOffset);
                    door.position = new Vector3(x, door.position.y, z);
                }
                else
                {
                    float x = Mathf.Lerp(center.x - size.x * 0.5f, center.x + size.x * 0.5f, normalizedOffset);
                    float z = wall == WallSide.North ? center.y + size.y * 0.5f : center.y - size.y * 0.5f;
                    door.position = new Vector3(x, door.position.y, z);
                }
            }

            private void RebuildDoorway(string roomName, string wallName, string doorName)
            {
                GameObject room = GameObject.Find(roomName);
                if (room == null)
                    return;

                Transform wall = room.transform.Find(wallName);
                Transform doorMarker = room.transform.Find(doorName);
                if (wall == null || doorMarker == null)
                    return;

                Transform oldSegments = wall.Find("DoorwaySegments");
                if (oldSegments != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(oldSegments.gameObject);
                    else
                        Destroy(oldSegments.gameObject);
                    #else
                    Destroy(oldSegments.gameObject);
                    #endif
                }

                Collider wallCollider = wall.GetComponent<Collider>();
                if (wallCollider != null)
                    wallCollider.enabled = false;

                Renderer wallRenderer = wall.GetComponent<Renderer>();
                if (wallRenderer != null)
                    wallRenderer.enabled = false;

                GameObject segmentRoot = new GameObject("DoorwaySegments");
                segmentRoot.transform.SetParent(room.transform, false);
                segmentRoot.transform.position = wall.position;
                segmentRoot.transform.rotation = wall.rotation;
                segmentRoot.transform.localScale = Vector3.one;

                Vector3 wallScale = wall.localScale;
                Vector3 doorScale = doorMarker.localScale;

                bool verticalWall = wallScale.z > wallScale.x;
                float wallLength = verticalWall ? wallScale.z : wallScale.x;
                float doorWidth = verticalWall ? doorScale.z : doorScale.x;
                float openingWidth = Mathf.Max(minimumDoorwayWidth, doorWidth + doorwayClearancePadding);
                float openingHeight = Mathf.Max(minimumDoorwayHeight, doorwayHeight);
                float openingCenter = 0f;

                if (TryGetDoorLayout(roomName, doorName, out WallSide wallSide, out float normalizedOffset))
                    openingCenter = Mathf.Lerp(-wallLength * 0.5f, wallLength * 0.5f, normalizedOffset);
                else
                {
                    Vector3 doorPos = wall.InverseTransformPoint(doorMarker.position);
                    openingCenter = verticalWall ? doorPos.z : doorPos.x;
                }

                float min = -wallLength * 0.5f;
                float max = wallLength * 0.5f;
                float doorMin = openingCenter - openingWidth * 0.5f;
                float doorMax = openingCenter + openingWidth * 0.5f;

                CreateWallSegment(segmentRoot.transform, wall, verticalWall, min, doorMin, "SegmentA");
                CreateWallSegment(segmentRoot.transform, wall, verticalWall, doorMax, max, "SegmentB");
                CreateLintel(segmentRoot.transform, wall, verticalWall, openingCenter, openingWidth, openingHeight);
                CreateDoorLeaf(room.transform, doorMarker, verticalWall, openingWidth, openingHeight, wallName);

                doorMarker.gameObject.SetActive(false);
            }

            private void CreateWallSegment(Transform parent, Transform sourceWall, bool verticalWall, float start, float end, string name)
            {
                float length = end - start;
                if (length <= 0.01f)
                    return;

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = name;
                segment.transform.SetParent(parent, false);

                Vector3 localPos = Vector3.zero;
                localPos[verticalWall ? 2 : 0] = start + length * 0.5f;
                segment.transform.localPosition = localPos;
                segment.transform.localRotation = Quaternion.identity;

                Vector3 scale = sourceWall.localScale;
                scale.y = Mathf.Max(0.01f, scale.y);
                if (verticalWall)
                    scale.z = length;
                else
                    scale.x = length;

                segment.transform.localScale = scale;

                MeshRenderer sourceRenderer = sourceWall.GetComponent<MeshRenderer>();
                if (sourceRenderer != null)
                    segment.GetComponent<MeshRenderer>().sharedMaterial = sourceRenderer.sharedMaterial;
            }

            private void CreateLintel(Transform parent, Transform sourceWall, bool verticalWall, float openingCenter, float openingWidth, float openingHeight)
            {
                float lintelHeight = Mathf.Max(0.01f, wallHeight - openingHeight);
                if (lintelHeight <= 0.01f)
                    return;

                GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lintel.name = "Lintel";
                lintel.transform.SetParent(parent, false);

                float lintelCenterY = (openingHeight * 0.5f) + (lintelHeight * 0.5f) - (wallHeight * 0.5f);
                Vector3 localPos = new Vector3(0f, lintelCenterY, 0f);
                localPos[verticalWall ? 2 : 0] = openingCenter;
                lintel.transform.localPosition = localPos;
                lintel.transform.localRotation = Quaternion.identity;

                Vector3 scale = sourceWall.localScale;
                scale.y = lintelHeight;
                if (verticalWall)
                    scale.z = openingWidth;
                else
                    scale.x = openingWidth;
                lintel.transform.localScale = scale;

                MeshRenderer sourceRenderer = sourceWall.GetComponent<MeshRenderer>();
                if (sourceRenderer != null)
                    lintel.GetComponent<MeshRenderer>().sharedMaterial = sourceRenderer.sharedMaterial;
            }

            private void CreateDoorLeaf(Transform roomRoot, Transform doorMarker, bool verticalWall, float doorWidth, float openingHeight, string wallName)
            {
                string doorLeafName = doorMarker.name + "_Leaf";
                Transform existingLeaf = roomRoot.Find(doorLeafName);
                if (existingLeaf != null)
                {
                    #if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(existingLeaf.gameObject);
                    else
                        Destroy(existingLeaf.gameObject);
                    #else
                    Destroy(existingLeaf.gameObject);
                    #endif
                }

                GameObject doorLeaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doorLeaf.name = doorLeafName;
                doorLeaf.transform.SetParent(roomRoot, false);
                doorLeaf.transform.position = doorMarker.position;
                doorLeaf.transform.rotation = doorMarker.rotation;
                float doorLeafHeight = Mathf.Max(0.01f, openingHeight - doorwayGroundClearance);
                doorLeaf.transform.localScale = verticalWall
                    ? new Vector3(closedDoorThickness, doorLeafHeight, doorWidth)
                    : new Vector3(doorWidth, doorLeafHeight, closedDoorThickness);

                Vector3 doorPosition = doorLeaf.transform.position;
                doorPosition.y = doorLeafHeight * 0.5f;
                doorLeaf.transform.position = doorPosition;

                if (verticalWall)
                {
                    float direction = wallName == "WestWall" ? 1f : -1f;
                    doorLeaf.transform.position += roomRoot.right * doorLeafPlaneOffset * direction;
                }
                else
                {
                    float direction = wallName == "SouthWall" ? 1f : -1f;
                    doorLeaf.transform.position += roomRoot.forward * doorLeafPlaneOffset * direction;
                }

                MeshRenderer renderer = doorLeaf.GetComponent<MeshRenderer>();
                renderer.material = CreateDoorMaterial();
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

            private void SetPrimitive(Transform root, string childName, Vector3 worldPosition, Vector3 localScale)
            {
                Transform child = root.Find(childName);
                if (child == null)
                    return;

                child.position = worldPosition;
                child.localScale = localScale;
            }

            private void SetMarker(Transform root, string childName, Vector3 worldPosition)
            {
                Transform child = root.Find(childName);
                if (child == null)
                    return;

                child.position = worldPosition;
            }

            private Material CreateDoorMaterial()
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                Material material = new Material(shader);
                material.color = doorColor;
                return material;
            }

            private bool TryGetDoorLayout(string roomName, string doorName, out WallSide wall, out float normalizedOffset)
            {
                wall = WallSide.North;
                normalizedOffset = 0.5f;

                if (roomName == "Lobby 1.1" && doorName == "To 1.2")
                {
                    wall = WallSide.West;
                    normalizedOffset = 0.72f;
                    return true;
                }

                if (roomName == "Lobby 1.1" && doorName == "To 1.3")
                {
                    wall = WallSide.South;
                    normalizedOffset = 0.55f;
                    return true;
                }

                if (roomName == "Sala 2 - 1.2" && doorName == "To 1.1")
                {
                    wall = WallSide.East;
                    normalizedOffset = 0.72f;
                    return true;
                }

                if (roomName == "Sala 2 - 1.2" && doorName == "To 1.3")
                {
                    wall = WallSide.South;
                    normalizedOffset = 0.08f;
                    return true;
                }

                if (roomName == "Sala 3 - 1.3" && doorName == "To 1.1")
                {
                    wall = WallSide.North;
                    normalizedOffset = 0.82f;
                    return true;
                }

                if (roomName == "Sala 3 - 1.3" && doorName == "To 1.2")
                {
                    wall = WallSide.North;
                    normalizedOffset = 0.06f;
                    return true;
                }

                if (roomName == "Sala 3 - 1.3" && doorName == "To 1.5")
                {
                    wall = WallSide.East;
                    normalizedOffset = 0.5f;
                    return true;
                }

                if (roomName == "Sala 4 - 1.5" && doorName == "To 1.3")
                {
                    wall = WallSide.West;
                    normalizedOffset = 0.5f;
                    return true;
                }

                if (roomName == "Sala 4 - 1.5" && doorName == "To 1.13")
                {
                    wall = WallSide.North;
                    normalizedOffset = 0.5f;
                    return true;
                }

                if (roomName == "Sala 5 - 1.13" && doorName == "To 1.5")
                {
                    wall = WallSide.South;
                    normalizedOffset = 0.5f;
                    return true;
                }

                return false;
            }
        }
    }
