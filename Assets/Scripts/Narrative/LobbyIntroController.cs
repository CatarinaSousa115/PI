using UnityEngine;
using UnityEngine.Events;
using MuseumGame;
using Puzzle;

namespace MuseumGame.Narrative
{
    public class LobbyIntroController : MonoBehaviour
    {
        [Header("References")]
        public MuseumRoom lobbyRoom;
        public SimpleDialogueTrigger introDialogue;
        public ScenePortal firstPortal;
        public MuseumHud hud;

        [Header("Objectives")]
        [TextArea(2, 4)]
        public string introObjective =
            "Fala com Dom Pedro para descobrires o que aconteceu as placas desaparecidas.";

        [TextArea(2, 4)]
        public string afterIntroObjective =
            "Segue para a Sala da Reconstrucao e recupera a primeira placa perdida.";

        [Header("Status Messages")]
        public string lockedPortalMessage = "Fala com Dom Pedro para receber a missao inicial.";
        public string readyPortalMessage = "Segue para a Sala 2 e resolve o puzzle.";

        [Header("Room 2 Setup")]
        public string room2RootName = "Sala 2 - 1.2";
        public string room2MarkerName = "Sala 2 - 1.2 Marker";
        [TextArea(2, 4)]
        public string room2Objective =
            "Segue para a Sala da Reconstrucao e resolve o puzzle para recuperar a primeira placa.";
        [TextArea(2, 4)]
        public string room2CompletedObjective =
            "A primeira placa foi recuperada. A proxima sala sera adicionada em seguida.";
        public string room2ReadyStatus = "Segue para a Sala 2 e resolve o puzzle.";
        public string room2CompletedStatus = "Sala 2 concluida. Primeira placa recuperada.";
        public string puzzleBoardName = "PuzzleBoard_Sala2";
        public string legacyPuzzleCanvasName = "PuzzleCanvas";
        public string room2PuzzleWallName = "WestWall";
        public bool autoPlaceRoom2DisplayOnPlay;
        public Vector3 room2PuzzleLocalPosition = new Vector3(0f, -0.525f, -0.02f);
        public Vector3 puzzleBoardEulerAngles = new Vector3(0f, 90f, 0f);
        public Vector3 puzzleBoardScale = Vector3.one;
        public float puzzleBoardWidth = 0.95f;
        public float puzzleScatterRadius = 0.32f;
        public float puzzleScatterDistanceFromWall = 0.75f;

        [Header("Navigation")]
        public bool openPassageToRoom2 = true;
        public string lobbyBlockoutName = "Lobby 1.1";
        public string lobbySpawnMarkerName = "Lobby 1.1 Marker";
        public string lobbyDoorLeafName = "To 1.2_Leaf";
        public string room2DoorLeafName = "To 1.1_Leaf";

        [Header("Player Spawn")]
        public string playerName = "Player";
        public float playerSpawnHeight = 1f;

        [Header("Flow")]
        public bool resetProgressOnStart = true;
        public bool startDialogueAutomatically;

        [Header("Events")]
        public UnityEvent onIntroStarted;
        public UnityEvent onIntroFinished;

        private bool introCompleted;
        private MuseumRoom room2Room;
        private PuzzleManager puzzleManager;

        void Awake()
        {
            ResolveReferences();
            ConfigureRoom2();

            if (Application.isPlaying)
            {
                PositionPlayerAtLobby();

                if (autoPlaceRoom2DisplayOnPlay)
                    AlignRoom2Display();
            }
        }

        void Start()
        {
            SetPassageOpen(false);

            if (resetProgressOnStart && MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.ResetProgress();

            lobbyRoom?.ActivateRoom();

            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.SetObjective(introObjective);

            SetPortalEnabled(false);

            if (hud != null)
                hud.SetStatus(lockedPortalMessage);

            if (introDialogue != null)
                introDialogue.onDialogueFinished.AddListener(HandleIntroFinished);

            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.RoomCompleted += HandleRoomCompleted;

            if (startDialogueAutomatically)
                BeginIntro();
        }

        void OnDestroy()
        {
            if (introDialogue != null)
                introDialogue.onDialogueFinished.RemoveListener(HandleIntroFinished);

            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.RoomCompleted -= HandleRoomCompleted;
        }

        public void BeginIntro()
        {
            onIntroStarted?.Invoke();
            introDialogue?.BeginDialogue();
        }

        public void HandleIntroFinished()
        {
            if (introCompleted)
                return;

            introCompleted = true;

            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.SetObjective(afterIntroObjective);

            SetPortalEnabled(true);
            SetPassageOpen(true);

            if (hud != null)
                hud.SetStatus(room2ReadyStatus);

            lobbyRoom?.MarkComplete();
            onIntroFinished?.Invoke();
        }

        private void SetPortalEnabled(bool value)
        {
            if (firstPortal != null)
                firstPortal.enabled = value;
        }

        private void HandleRoomCompleted(MuseumRoomId roomId)
        {
            if (roomId != MuseumRoomId.Reconstruction)
                return;

            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.SetObjective(room2CompletedObjective);

            if (hud != null)
                hud.SetStatus(room2CompletedStatus);
        }

        private void ResolveReferences()
        {
            if (lobbyRoom == null)
            {
                GameObject lobbyRoomObject = GameObject.Find("LobbyRoom");
                if (lobbyRoomObject != null)
                    lobbyRoom = lobbyRoomObject.GetComponent<MuseumRoom>();
            }

            if (introDialogue == null)
            {
                GameObject quadro = GameObject.Find("DPedro");
                if (quadro != null)
                    introDialogue = quadro.GetComponentInChildren<SimpleDialogueTrigger>();

                if (introDialogue == null)
                {
                    GameObject fallbackQuadro = GameObject.Find("QuadroDomPedro");
                    if (fallbackQuadro != null)
                        introDialogue = fallbackQuadro.GetComponent<SimpleDialogueTrigger>();
                }

                if (introDialogue == null)
                    introDialogue = FindAnyObjectByType<SimpleDialogueTrigger>();
            }

            if (hud == null)
                hud = MuseumHud.EnsureExists();

            if (introDialogue != null)
            {
                introDialogue.enabled = true;
                introDialogue.EnsureReady();
            }
        }

        private void ConfigureRoom2()
        {
            GameObject room2Root = GameObject.Find(room2RootName);
            if (room2Root == null)
                return;

            room2Room = room2Root.GetComponent<MuseumRoom>();
            if (room2Room == null)
                room2Room = room2Root.AddComponent<MuseumRoom>();

            room2Room.roomId = MuseumRoomId.Reconstruction;
            room2Room.roomObjective = room2Objective;
            room2Room.activateOnStart = false;
            room2Room.awardsPlaqueOnComplete = true;

            GameObject puzzleBoard = GameObject.Find(puzzleBoardName);
            if (puzzleBoard == null)
                return;

            Transform legacyCanvas = puzzleBoard.transform.Find(legacyPuzzleCanvasName);
            if (legacyCanvas != null)
                legacyCanvas.gameObject.SetActive(false);

            puzzleManager = puzzleBoard.GetComponent<PuzzleManager>();
            if (puzzleManager == null)
                return;

            puzzleManager.scatterRadius = puzzleScatterRadius;

            puzzleManager.enabled = true;
        }

        [ContextMenu("Align Room 2 Display")]
        public void AlignRoom2Display()
        {
            GameObject room2Root = GameObject.Find(room2RootName);
            GameObject puzzleBoard = GameObject.Find(puzzleBoardName);
            if (room2Root == null || puzzleBoard == null)
                return;

            Transform wall = FindChildRecursive(room2Root.transform, room2PuzzleWallName);
            if (wall == null)
                return;

            PositionPuzzleBoardOnWall(puzzleBoard.transform, wall);
        }

        [ContextMenu("Capture Current Room 2 Display")]
        public void CaptureCurrentRoom2Display()
        {
            GameObject room2Root = GameObject.Find(room2RootName);
            GameObject puzzleBoard = GameObject.Find(puzzleBoardName);
            if (room2Root == null || puzzleBoard == null)
                return;

            Transform wall = FindChildRecursive(room2Root.transform, room2PuzzleWallName);
            if (wall == null)
                return;

            if (puzzleBoard.transform.parent != wall)
                puzzleBoard.transform.SetParent(wall, true);

            room2PuzzleLocalPosition = puzzleBoard.transform.localPosition;
            puzzleBoardEulerAngles = puzzleBoard.transform.localEulerAngles;
            puzzleBoardScale = puzzleBoard.transform.localScale;
        }

        private void SetPassageOpen(bool isOpen)
        {
            if (!openPassageToRoom2)
                return;

            GameObject lobbyRoot = GameObject.Find(lobbyBlockoutName);
            GameObject room2Root = GameObject.Find(room2RootName);

            SetBlockerState(lobbyRoot, lobbyDoorLeafName, !isOpen);
            SetBlockerState(room2Root, room2DoorLeafName, !isOpen);
        }

        private void PositionPlayerAtLobby()
        {
            GameObject player = GameObject.Find(playerName);
            GameObject lobbyRoot = GameObject.Find(lobbyBlockoutName);
            if (player == null || lobbyRoot == null)
                return;

            Transform marker = FindChildRecursive(lobbyRoot.transform, lobbySpawnMarkerName);
            if (marker == null)
                return;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;

            player.transform.position = marker.position + Vector3.up * playerSpawnHeight;
            player.transform.rotation = Quaternion.identity;

            if (controller != null)
                controller.enabled = true;
        }

        private void PositionPuzzleBoardOnWall(Transform puzzleBoard, Transform wall)
        {
            float boardHeight = GetPuzzleBoardHeight();
            Vector3 localPosition = room2PuzzleLocalPosition;

            // Puzzle pieces are generated from the board root's lower-left corner,
            // so we offset by half width/height to visually center the assembled image.
            localPosition.x -= puzzleBoardWidth * 0.5f;
            localPosition.y -= boardHeight * 0.5f;

            puzzleBoard.SetParent(wall, false);
            puzzleBoard.localPosition = localPosition;
            puzzleBoard.localRotation = Quaternion.Euler(puzzleBoardEulerAngles);
            puzzleBoard.localScale = puzzleBoardScale;
        }

        private float GetPuzzleBoardHeight()
        {
            if (puzzleManager != null && puzzleManager.paintingTexture != null)
            {
                float aspect = (float)puzzleManager.paintingTexture.width /
                               puzzleManager.paintingTexture.height;

                if (aspect > 0.0001f)
                    return puzzleBoardWidth / aspect;
            }

            return 1f;
        }

        private void SetBlockerState(GameObject rootObject, string childName, bool isVisibleAndSolid)
        {
            if (rootObject == null || string.IsNullOrWhiteSpace(childName))
                return;

            Transform child = FindChildRecursive(rootObject.transform, childName);
            if (child == null)
                return;

            Collider blocker = child.GetComponent<Collider>();
            if (blocker != null)
            {
                blocker.enabled = isVisibleAndSolid;
                blocker.isTrigger = !isVisibleAndSolid;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = isVisibleAndSolid;
        }

        private Transform FindChildRecursive(Transform root, string targetName)
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
    }
}
