using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using MuseumGame;

public class MinigameManager : MonoBehaviour
{
    [Header("References")]
    public StatueController statue;

    [Header("Painting Order")]
    [Tooltip("Drag paintings here IN THE CORRECT ORDER they must be interacted with.")]
    public List<PaintingInteractable> correctOrder = new List<PaintingInteractable>();

    [Header("Events")]
    [Tooltip("Fired when the player interacts with a painting in the correct next position.")]
    public UnityEvent<int> onCorrectPainting;

    [Tooltip("Fired when the player makes a wrong-order interaction.")]
    public UnityEvent onWrongOrder;

    [Tooltip("Fired when the entire sequence is complete and the statue accepts it.")]
    public UnityEvent onMinigameComplete;

    [Header("Room Completion")]
    [Tooltip("Optional room component to mark complete. If empty, the manager marks roomIdToComplete directly.")]
    public MuseumRoom roomToComplete;
    public MuseumRoomId roomIdToComplete = MuseumRoomId.Conversations;
    public bool awardPlaqueOnComplete = true;

    [Header("Doors To Next Room")]
    public bool closeDoorsOnStart = true;
    public bool autoResolveDoors = true;
    public GameObject[] doorsToOpen = new GameObject[0];
    public string currentRoomName = "Sala 3 - 1.3";
    public string nextRoomName = "Sala 4 - 1.5";
    public string currentRoomDoorLeafName = "To 1.5_Leaf";
    public string nextRoomDoorLeafName = "To 1.3_Leaf";

    [Header("HUD Feedback")]
    public bool showHudFeedback = true;
    public string minigameStartStatus = "Segue a ordem correta dos quadros.";
    public string correctStatusFormat = "Quadro correto ({0}/{1}).";
    public string wrongStatus = "Ordem errada. Tenta outra vez desde o inicio.";
    public string paintingsSolvedStatus = "Sequencia resolvida. Volta a estatua.";
    public string minigameCompleteStatus = "Sala concluida.";

    private int _nextExpectedIndex = 0;
    private bool _isActive = false;
    private bool _sequenceComplete = false;
    private bool _completionHandled = false;

    public bool IsActive => _isActive;
    public bool SequenceComplete => _sequenceComplete;

    private void Start()
    {
        ResolveCompletionReferences();

        bool alreadyComplete = (roomToComplete != null && roomToComplete.IsCompleted) ||
                               (MuseumGameManager.Instance != null &&
                                MuseumGameManager.Instance.IsRoomComplete(roomIdToComplete));

        if (alreadyComplete)
        {
            _completionHandled = true;
            SetDoorsState(false);
        }
        else if (closeDoorsOnStart)
        {
            SetDoorsState(true);
        }
    }

    public void BeginMinigame()
    {
        _isActive = true;
        _nextExpectedIndex = 0;
        _sequenceComplete = false;

        foreach (var painting in correctOrder)
            painting.SetInteractable(true);

        SetHudStatus(minigameStartStatus);
        Debug.Log("[MinigameManager] Minigame started. " +
                  $"First painting expected: {correctOrder[0].name}");
    }

    public void CompleteMinigame()
    {
        _isActive = false;

        if (_completionHandled)
            return;

        _completionHandled = true;
        ResolveCompletionReferences();

        CompleteRoom();
        SetDoorsState(false);
        SetHudStatus(minigameCompleteStatus);
        onMinigameComplete?.Invoke();
        Debug.Log("[MinigameManager] Minigame fully complete! Passage to next room unlocked.");
    }

    public bool RegisterPaintingInteraction(PaintingInteractable painting)
    {
        if (!_isActive || _sequenceComplete)
        {
            Debug.Log("[MinigameManager] Interaction ignored — minigame not active.");
            return false;
        }

        if (correctOrder[_nextExpectedIndex] == painting)
        {
            onCorrectPainting?.Invoke(_nextExpectedIndex);
            _nextExpectedIndex++;

            SetHudStatus(string.Format(correctStatusFormat, _nextExpectedIndex, correctOrder.Count));
            Debug.Log($"[MinigameManager] Correct! ({_nextExpectedIndex}/{correctOrder.Count})");

            if (_nextExpectedIndex >= correctOrder.Count)
            {
                _sequenceComplete = true;
                statue.OnPaintingsSolved();
                SetHudStatus(paintingsSolvedStatus);
                Debug.Log("[MinigameManager] All paintings done — notifying statue.");
            }

            return true;
        }
        else
        {
            onWrongOrder?.Invoke();
            _nextExpectedIndex = 0;
            SetHudStatus(wrongStatus);

            Debug.Log($"[MinigameManager] Wrong order! Expected: {correctOrder[_nextExpectedIndex].name}. " +
                       "Resetting sequence.");
            return false;
        }
    }

    private void SetHudStatus(string message)
    {
        if (!showHudFeedback || string.IsNullOrWhiteSpace(message))
            return;

        MuseumHud.EnsureExists()?.SetStatus(message);
    }

    private void CompleteRoom()
    {
        if (roomToComplete != null)
        {
            roomToComplete.MarkComplete();
            return;
        }

        if (MuseumGameManager.Instance == null || roomIdToComplete == MuseumRoomId.None)
            return;

        MuseumGameManager.Instance.MarkRoomComplete(roomIdToComplete);

        if (awardPlaqueOnComplete)
            MuseumGameManager.Instance.TryCollectPlaque(roomIdToComplete);
    }

    private void ResolveCompletionReferences()
    {
        Transform currentRoom = ResolveRoomTransform(currentRoomName);

        if (roomToComplete == null && currentRoom != null)
            roomToComplete = currentRoom.GetComponent<MuseumRoom>();

        if (!autoResolveDoors || (doorsToOpen != null && doorsToOpen.Length > 0))
            return;

        List<GameObject> resolvedDoors = new List<GameObject>();
        AddDoorIfFound(resolvedDoors, currentRoom, currentRoomDoorLeafName);
        AddDoorIfFound(resolvedDoors, ResolveRoomTransform(nextRoomName), nextRoomDoorLeafName);

        doorsToOpen = resolvedDoors.ToArray();
    }

    private Transform ResolveRoomTransform(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            return null;

        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == roomName)
                return current;
        }

        GameObject roomObject = GameObject.Find(roomName);
        return roomObject != null ? roomObject.transform : null;
    }

    private void AddDoorIfFound(List<GameObject> doors, Transform roomRoot, string doorName)
    {
        if (roomRoot == null || string.IsNullOrWhiteSpace(doorName))
            return;

        Transform door = FindChildRecursive(roomRoot, doorName);
        if (door != null && !doors.Contains(door.gameObject))
            doors.Add(door.gameObject);
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void SetDoorsState(bool closed)
    {
        if (doorsToOpen == null)
            return;

        foreach (GameObject door in doorsToOpen)
        {
            if (door == null)
                continue;

            Renderer rend = door.GetComponent<Renderer>();
            if (rend != null)
                rend.enabled = closed;

            Collider col = door.GetComponent<Collider>();
            if (col != null)
                col.enabled = closed;

            if (rend == null && col == null)
                door.SetActive(closed);
        }
    }
}
