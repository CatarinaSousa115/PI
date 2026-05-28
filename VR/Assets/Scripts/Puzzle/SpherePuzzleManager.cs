using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace MuseumGame.Puzzle
{
    public class SpherePuzzleManager : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The name of the original scene to return to.")]
        public string originalSceneName = "Lobby"; // Set this to the original scene name

        [Tooltip("List of all spheres that need to turn green.")]
        public List<GazeSphere> allSpheres = new List<GazeSphere>();

        private int greenSpheresCount = 0;

        void Start()
        {
            // Optional: Auto-find spheres if list is empty
            if (allSpheres.Count == 0)
            {
                allSpheres = new List<GazeSphere>(FindObjectsOfType<GazeSphere>());
            }
        }

        public void ReportSphereGreen()
        {
            greenSpheresCount++;
            
            Debug.Log($"[SpherePuzzleManager] Spheres green: {greenSpheresCount} / {allSpheres.Count}");

            if (greenSpheresCount == allSpheres.Count && allSpheres.Count > 0)
            {
                CompletePuzzle();
            }
        }

        private void CompletePuzzle()
        {
            Debug.Log("[SpherePuzzleManager] All spheres are green! Returning to original scene.");
            
            // Trigger the scene change back to the original scene
            SceneManager.LoadScene(originalSceneName);
        }
    }
}
