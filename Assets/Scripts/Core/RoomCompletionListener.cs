using UnityEngine;
using UnityEngine.Events;

namespace MuseumGame
{
    public class RoomCompletionListener : MonoBehaviour
    {
        public MuseumRoomId watchedRoom = MuseumRoomId.None;
        public UnityEvent onRoomCompleted;

        private bool fired;

        void OnEnable()
        {
            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.RoomCompleted += HandleRoomCompleted;
        }

        void Start()
        {
            if (MuseumGameManager.Instance != null &&
                MuseumGameManager.Instance.IsRoomComplete(watchedRoom))
            {
                Fire();
            }
        }

        void OnDisable()
        {
            if (MuseumGameManager.Instance != null)
                MuseumGameManager.Instance.RoomCompleted -= HandleRoomCompleted;
        }

        private void HandleRoomCompleted(MuseumRoomId roomId)
        {
            if (roomId != watchedRoom)
                return;

            Fire();
        }

        private void Fire()
        {
            if (fired)
                return;

            fired = true;
            onRoomCompleted?.Invoke();
        }
    }
}
