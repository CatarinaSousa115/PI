using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseumGame
{
    public class MuseumGameManager : MonoBehaviour
    {
        public static MuseumGameManager Instance { get; private set; }

        [Header("Mission")]
        [TextArea(2, 4)]
        [SerializeField] private string initialObjective =
            "Encontra as quatro placas desaparecidas e restaura a colecao principal do museu.";

        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly HashSet<MuseumRoomId> completedRooms = new HashSet<MuseumRoomId>();
        private readonly HashSet<MuseumRoomId> collectedPlaques = new HashSet<MuseumRoomId>();
        private bool gameCompleted;

        public event Action<string> ObjectiveChanged;
        public event Action<int, int> PlaqueProgressChanged;
        public event Action<MuseumRoomId> RoomCompleted;
        public event Action GameCompleted;

        public string CurrentObjective { get; private set; }
        public int TotalPlaques => 4;
        public int CollectedPlaques => collectedPlaques.Count;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            MuseumHud.EnsureExists();
            SetObjective(initialObjective, forceNotify: true);
            NotifyPlaqueProgress();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetObjective(string objective, bool forceNotify = false)
        {
            if (!forceNotify && string.Equals(CurrentObjective, objective, StringComparison.Ordinal))
                return;

            CurrentObjective = objective;
            ObjectiveChanged?.Invoke(CurrentObjective);
        }

        public bool IsRoomComplete(MuseumRoomId roomId)
        {
            return completedRooms.Contains(roomId);
        }

        public bool HasCollectedPlaque(MuseumRoomId roomId)
        {
            return collectedPlaques.Contains(roomId);
        }

        public void MarkRoomComplete(MuseumRoomId roomId)
        {
            if (roomId == MuseumRoomId.None || completedRooms.Contains(roomId))
                return;

            completedRooms.Add(roomId);
            RoomCompleted?.Invoke(roomId);

            TryFinishGame();
        }

        public bool TryCollectPlaque(MuseumRoomId roomId)
        {
            if (roomId == MuseumRoomId.None || collectedPlaques.Contains(roomId))
                return false;

            collectedPlaques.Add(roomId);
            NotifyPlaqueProgress();

            if (CollectedPlaques >= TotalPlaques)
                SetObjective("Volta ao lobby e recoloca as quatro placas na colecao principal.");

            TryFinishGame();

            return true;
        }

        public void ResetProgress()
        {
            completedRooms.Clear();
            collectedPlaques.Clear();
            gameCompleted = false;
            SetObjective(initialObjective, forceNotify: true);
            NotifyPlaqueProgress();
        }

        private void NotifyPlaqueProgress()
        {
            PlaqueProgressChanged?.Invoke(CollectedPlaques, TotalPlaques);
        }

        private void TryFinishGame()
        {
            if (gameCompleted || CollectedPlaques < TotalPlaques)
                return;

            gameCompleted = true;
            GameCompleted?.Invoke();
        }
    }
}
