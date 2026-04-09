using UnityEngine;
using UnityEngine.Events;

namespace MuseumGame
{
    public class FinalCollectionManager : MonoBehaviour
    {
        [Header("Room")]
        public MuseumRoom finalRoom;

        [Header("Collection")]
        [Min(1)] public int requiredPlaques = 4;

        [Header("Events")]
        public UnityEvent onCollectionRestored;

        private int placedPlaques;

        public int PlacedPlaques => placedPlaques;

        public void RegisterPlaquePlacement()
        {
            placedPlaques = Mathf.Min(placedPlaques + 1, requiredPlaques);

            if (placedPlaques < requiredPlaques)
                return;

            finalRoom?.MarkComplete();
            onCollectionRestored?.Invoke();
        }

        public void ResetCollection()
        {
            placedPlaques = 0;
        }
    }
}
