using UnityEngine;
using UnityEngine.Events;

namespace MuseumGame
{
    public class MuseumRoom : MonoBehaviour
    {
        [Header("Identity")]
        public MuseumRoomId roomId = MuseumRoomId.None;

        [Header("Objective")]
        [TextArea(2, 4)]
        public string roomObjective = "Conclui o desafio desta sala para recuperar a placa perdida.";
        public bool activateOnStart = true;

        [Header("Rewards")]
        public bool awardsPlaqueOnComplete = true;

        [Header("Events")]
        public UnityEvent onRoomActivated;
        public UnityEvent onRoomCompleted;

        public bool IsCompleted { get; private set; }

        void Start()
        {
            if (activateOnStart)
                ActivateRoom();
        }

        public void ActivateRoom()
        {
            if (MuseumGameManager.Instance != null && !string.IsNullOrWhiteSpace(roomObjective))
                MuseumGameManager.Instance.SetObjective(roomObjective);

            onRoomActivated?.Invoke();
        }

        public void MarkComplete()
        {
            if (IsCompleted)
                return;

            IsCompleted = true;

            if (MuseumGameManager.Instance != null)
            {
                MuseumGameManager.Instance.MarkRoomComplete(roomId);

                if (awardsPlaqueOnComplete)
                    MuseumGameManager.Instance.TryCollectPlaque(roomId);
            }

            onRoomCompleted?.Invoke();
        }
    }
}
