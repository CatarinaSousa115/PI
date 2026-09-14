using UnityEngine;
using MuseumGame;
using MuseumGame.UI;

namespace MuseumGame.Narrative
{
    /// <summary>
    /// Coordinates the completion of Room 2 (Reconstruction).
    /// Opens the doors to Room 3 only when the puzzle is solved and all cards are in sockets.
    /// </summary>
    public class Sala2Controller : MonoBehaviour
    {
        [Header("Room Identity")]
        public MuseumRoom room2;
        
        [Header("Doors to Room 3")]
        [Tooltip("The visual and physical leaves of the doors leading to Room 3 (usually 'To 1.3_Leaf' in Sala 2 and 'To 1.2_Leaf' in Sala 3).")]
        public GameObject[] doorsToOpen;

        [Header("Requirements")]
        public PuzzleManager puzzleManager;
        public CardSocketPuzzle cardSocketPuzzle;

        [Header("Lobby Reward")]
        [Tooltip("Referência para o display de peças no lobby.")]
        public PieceDisplay pieceDisplay;
        [Tooltip("Índice da peça a desbloquear quando a sala estiver concluída.")]
        public int pieceIndexToUnlock = 0;

        [Header("Feedback")]
        public string completionStatusMessage = "Passagem para a Sala 3 aberta!";

        private bool _puzzleDone;
        private bool _cardsDone;
        private bool _isComplete;

        private void Start()
        {
            // Ensure doors are initially closed
            SetDoorsState(true);

            // Subscribe to Painting Puzzle event
            if (puzzleManager != null)
                puzzleManager.onPuzzleComplete.AddListener(OnPuzzleComplete);
            else
                Debug.LogWarning("[Sala2Controller] PuzzleManager reference missing!");

            // Subscribe to Card Socket Puzzle event
            if (cardSocketPuzzle != null)
                cardSocketPuzzle.onPuzzleCompleted.AddListener(OnCardsComplete);
            else
                Debug.LogWarning("[Sala2Controller] CardSocketPuzzle reference missing!");
        }

        private void OnPuzzleComplete()
        {
            if (_puzzleDone) return;
            _puzzleDone = true;
            Debug.Log("[Sala2Controller] Requirement met: Painting Puzzle Complete.");
            CheckCompletion();
        }

        private void OnCardsComplete()
        {
            if (_cardsDone) return;
            _cardsDone = true;
            Debug.Log("[Sala2Controller] Requirement met: Cards placed in sockets.");
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            if (_isComplete) return;

            if (_puzzleDone && _cardsDone)
            {
                _isComplete = true;
                UnlockRoom3();
            }
        }

        private void UnlockRoom3()
        {
            // 1. Open the doors
            SetDoorsState(false);

            // 2. Notify Game System
            if (room2 != null)
                room2.MarkComplete();

            // 3. HUD Feedback
            MuseumHud.EnsureExists()?.SetStatus(completionStatusMessage);

            // 4. Lobby reward (unlock all pieces)
            if (pieceDisplay != null)
            {
                pieceDisplay.UnlockPiece(5);
                pieceDisplay.UnlockPiece(10);
                pieceDisplay.UnlockPiece(15);
                pieceDisplay.UnlockPiece(20);
            }

            Debug.Log("[Sala2Controller] Room 2 fully complete. Passage to Room 3 unlocked.");
        }

        private void SetDoorsState(bool closed)
        {
            if (doorsToOpen == null) return;

            foreach (GameObject door in doorsToOpen)
            {
                if (door == null) continue;

                Renderer rend = door.GetComponent<Renderer>();
                if (rend != null) rend.enabled = closed;

                Collider col = door.GetComponent<Collider>();
                if (col != null) col.enabled = closed;
                
                if (rend == null && col == null)
                    door.SetActive(closed);
            }
        }

        private void OnDestroy()
        {
            if (puzzleManager != null)
                puzzleManager.onPuzzleComplete.RemoveListener(OnPuzzleComplete);
            
            if (cardSocketPuzzle != null)
                cardSocketPuzzle.onPuzzleCompleted.RemoveListener(OnCardsComplete);
        }
    }
}
