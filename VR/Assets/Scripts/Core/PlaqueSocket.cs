using UnityEngine;
using UnityEngine.Events;

namespace MuseumGame
{
    public class PlaqueSocket : MonoBehaviour
    {
        public FinalCollectionManager collectionManager;
        public UnityEvent onPlaquePlaced;

        public bool IsFilled { get; private set; }

        public void PlacePlaque()
        {
            if (IsFilled)
                return;

            IsFilled = true;
            collectionManager?.RegisterPlaquePlacement();
            onPlaquePlaced?.Invoke();
        }

        public void ResetSocket()
        {
            IsFilled = false;
        }
    }
}
