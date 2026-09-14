using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class CircleNode : MonoBehaviour
{
    [Header("Connections")]
    public CirclePuzzleManager puzzleManager;

    [Header("Gaze Settings")]
    [Tooltip("How close to the center of the screen (in degrees) the player needs to look to target this sphere.")]
    public float gazeToleranceDegrees = 8f;

    private bool isActivated = false;

    void Start()
    {
        // We no longer rely on the XR Toolkit's finicky raycasts or colliders.
        // We will just use math to see if the player is looking at the sphere!
    }

    private bool wasTriggerPressedLastFrame = false;

    void Update()
    {
        // Custom Math-based Line of Sight Check (flawless, infinite distance, ignores overlapping colliders)
        bool isLooking = false;

        if (Camera.main != null)
        {
            Vector3 directionToSphere = (transform.position - Camera.main.transform.position).normalized;
            float lookAngle = Vector3.Angle(Camera.main.transform.forward, directionToSphere);

            if (lookAngle <= gazeToleranceDegrees)
            {
                isLooking = true;
            }
        }

        // Universal VR Trigger Check (Works for both left and right controllers without Inspector setup)
        bool isTriggerPressed = false;
        var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
        
        foreach (var device in devices)
        {
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool triggerValue))
            {
                if (triggerValue) isTriggerPressed = true;
            }
        }

        bool triggerPressedThisFrame = isTriggerPressed && !wasTriggerPressedLastFrame;
        wasTriggerPressedLastFrame = isTriggerPressed;

        // Check if the player is looking, it's not activated yet, and they press the VR trigger (or 'G' on keyboard as fallback)
        bool keyboardFallback = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;

        if (isLooking && !isActivated && (triggerPressedThisFrame || keyboardFallback))
        {
            ActivateCircle();
        }
    }

    // For actual VR Headsets (Trigger/Grip button) instead of the keyboard
    public void OnVRSelect()
    {
        if (!isActivated)
        {
            ActivateCircle();
        }
    }

    private void ActivateCircle()
    {
        isActivated = true;

        // Turn the circle green
        GetComponent<Renderer>().material.color = Color.green;

        // Tell the manager we activated one!
        if (puzzleManager != null)
        {
            puzzleManager.OnCircleActivated();
        }
        else
        {
            Debug.LogWarning("CircleNode is missing the PuzzleManager reference!");
        }
    }
}