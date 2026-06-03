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

        [Tooltip("The exact number of green spheres needed to complete the puzzle.")]
        public int requiredSpheres = 5;

        private int greenSpheresCount = 0;

        void Start()
        {
            // Force it to 5 to avoid Unity Inspector overriding it with old values
            requiredSpheres = 5;

            // Optional: Auto-find spheres if list is empty
            if (allSpheres.Count == 0)
            {
                allSpheres = new List<GazeSphere>(FindObjectsOfType<GazeSphere>());
            }
        }

        public void ReportSphereGreen()
        {
            greenSpheresCount++;
            
            Debug.Log($"[SpherePuzzleManager] Spheres green: {greenSpheresCount} / {requiredSpheres}");

            if (greenSpheresCount >= requiredSpheres)
            {
                CompletePuzzle();
            }
        }

        private void CompletePuzzle()
        {
            string currentScene = SceneManager.GetActiveScene().name.Trim();
            Debug.Log($"[SpherePuzzleManager] All spheres are green! Marking puzzle as completed: '{currentScene}'");
            
            // Mark the current puzzle scene as completed in PlayerPrefs for bulletproof persistence
            PlayerPrefs.SetInt("Completed_" + currentScene, 1);
            PlayerPrefs.Save();
            
            // Trigger the scene change back to the original scene
            SceneManager.LoadScene(originalSceneName);
        }
    }
}
