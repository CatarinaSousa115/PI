using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

namespace MuseumGame.Puzzle
{
    public class GazeSphere : MonoBehaviour
    {
        [Header("Settings")]
        public Material greenMaterial;
        public Key activationKey = Key.G;
        
        [Header("References")]
        public SpherePuzzleManager puzzleManager;
        private Renderer meshRenderer;
        
        private bool isGazedAt = false;
        private bool isAlreadyGreen = false;

        void Start()
        {
            meshRenderer = GetComponent<Renderer>();
            
            if (puzzleManager == null)
            {
                puzzleManager = FindObjectOfType<SpherePuzzleManager>();
            }
        }

        void Update()
        {
            // If the player is looking at the sphere, hasn't activated it yet, and presses 'G'
            if (isGazedAt && !isAlreadyGreen && Keyboard.current != null && Keyboard.current[activationKey].wasPressedThisFrame)
            {
                TurnGreen();
            }
        }

        public void OnGazeEnter()
        {
            isGazedAt = true;
        }

        public void OnGazeExit()
        {
            isGazedAt = false;
        }

        private void TurnGreen()
        {
            isAlreadyGreen = true;
            
            // Change the material
            if (greenMaterial != null && meshRenderer != null)
            {
                meshRenderer.material = greenMaterial;
            }
            
            // Notify the manager
            if (puzzleManager != null)
            {
                puzzleManager.ReportSphereGreen();
            }
            else
            {
                Debug.LogWarning("[GazeSphere] SpherePuzzleManager is not assigned!");
            }
        }
    }
}