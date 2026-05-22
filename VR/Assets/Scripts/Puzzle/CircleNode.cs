using UnityEngine;

public class CircleNode : MonoBehaviour
{
    [Header("Connections")]
    public CirclePuzzleManager puzzleManager;

    private bool isLooking = false;
    private bool isActivated = false;

    // Called when the Gaze hits the circle
    public void OnGazeEnter()
    {
        isLooking = true;
    }

    // Called when the Gaze leaves the circle
    public void OnGazeExit()
    {
        isLooking = false;
    }

    void Update()
    {
        // Check if the player is looking, it's not green yet, and they press 'G' on the keyboard
        if (isLooking && !isActivated && Input.GetKeyDown(KeyCode.G))
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
