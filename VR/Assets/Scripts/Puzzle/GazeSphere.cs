using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

namespace MuseumGame.Puzzle
{
    public class GazeSphere : MonoBehaviour
    {
        [Header("Settings")]
        public Material greenMaterial;
        
        [Header("References")]
        public SpherePuzzleManager puzzleManager;
        private Renderer meshRenderer;
        
        private bool isGazedAt = false;
        private bool isAlreadyGreen = false;
        private bool wasTriggerPressedLastFrame = false;

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
            // Universal VR Trigger Check (Works for both left and right controllers without Inspector setup)
            bool isTriggerPressed = false;
            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            
            foreach (var device in devices)
            {
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerValue))
                {
                    if (triggerValue) isTriggerPressed = true;
                }
            }

            bool triggerPressedThisFrame = isTriggerPressed && !wasTriggerPressedLastFrame;
            wasTriggerPressedLastFrame = isTriggerPressed;

            // Keyboard fallback just in case
            bool keyboardFallback = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;

            // If the player is looking at the sphere, hasn't activated it yet, and presses the VR Trigger
            if (isGazedAt && !isAlreadyGreen && (triggerPressedThisFrame || keyboardFallback))
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