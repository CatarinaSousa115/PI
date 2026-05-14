using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseumGame
{
    public class ScenePortal : MonoBehaviour
    {
        [Header("Scene")]
        public string targetSceneName;

        [Header("Access Control")]
        public bool requiresCompletedRoom;
        public MuseumRoomId requiredRoom = MuseumRoomId.None;

        [Header("Interaction")]
        public bool triggerLoadsScene = true;
        public KeyCode interactKey = KeyCode.E;
        [TextArea(2, 3)]
        public string lockedMessage = "Ainda existe uma sala por concluir antes de avancares.";

        private bool playerInside;

        void Update()
        {
            if (!triggerLoadsScene && playerInside && Input.GetKeyDown(interactKey))
                TryLoadScene();
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            playerInside = true;

            if (triggerLoadsScene)
                TryLoadScene();
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                playerInside = false;
        }

        public void TryLoadScene()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("[ScenePortal] No target scene configured.");
                return;
            }

            if (requiresCompletedRoom &&
                MuseumGameManager.Instance != null &&
                !MuseumGameManager.Instance.IsRoomComplete(requiredRoom))
            {
                Debug.Log($"[ScenePortal] {lockedMessage}");
                return;
            }

            SceneManager.LoadScene(targetSceneName);
        }
    }
}
