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

    public bool IsActive => _isActive;
    public bool SequenceComplete => _sequenceComplete;

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
        SetHudStatus(minigameCompleteStatus);
        onMinigameComplete?.Invoke();
        Debug.Log("[MinigameManager] Minigame fully complete!");
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
}
