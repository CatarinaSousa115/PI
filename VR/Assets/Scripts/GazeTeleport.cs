using UnityEngine;
using UnityEngine.UI; // Required if you use a loading circle
using UnityEngine.SceneManagement;

public class GazeTeleport : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;      // Name of the scene for this painting
    public float gazeTime = 5f;     // How many seconds to look?

    [Header("Distance & Gaze Restrictions")]
    public float maxDistance = 3.0f; // Must be within 3 meters
    [Tooltip("If true, requires the player's head/camera to look directly at the object, ignoring controller rays.")]
    public bool requireHeadGaze = true;

    [Header("Visual Feedback (Optional)")]
    public Image loadingBar;        // A UI Image set to "Filled" mode

    private float timer = 0f;
    private bool isHovered = false;

    // Use PlayerPrefs for bulletproof persistence across scenes and editor reloads
    private string GetCompletedKey()
    {
        return "Completed_" + (sceneToLoad != null ? sceneToLoad.Trim() : "");
    }

    void Update()
    {
        // If the puzzle is already completed, do nothing
        if (PlayerPrefs.GetInt(GetCompletedKey(), 0) == 1) 
        {
            ResetTimer();
            return;
        }

        bool isCloseEnough = false;
        bool isHeadLooking = true;

        if (Camera.main != null)
        {
            // 1. Check Distance
            float dist = Vector3.Distance(Camera.main.transform.position, transform.position);
            isCloseEnough = dist <= maxDistance;

            // 2. Check Head Gaze
            if (requireHeadGaze)
            {
                // Calculate direction from camera to this object
                Vector3 dirToTarget = (transform.position - Camera.main.transform.position).normalized;
                // Dot product > 0.85 means the camera is looking roughly directly at it (within ~30 degrees)
                float dotProduct = Vector3.Dot(Camera.main.transform.forward, dirToTarget);
                isHeadLooking = dotProduct > 0.85f;
            }
        }

        // Only progress the timer if they are hovered by a ray, close enough, AND looking with their head
        if (isHovered && isCloseEnough && isHeadLooking)
        {
            timer += Time.deltaTime;

            // Update the UI loading bar if you have one
            if (loadingBar != null)
            {
                loadingBar.fillAmount = timer / gazeTime;
            }

            // Trigger the teleport
            if (timer >= gazeTime)
            {
                Teleport();
            }
        }
        else
        {
            // Reset if they look away or step too far back
            ResetTimer();
        }
    }

    private void ResetTimer()
    {
        timer = 0f;
        if (loadingBar != null) loadingBar.fillAmount = 0;
    }
    
    public void OnGazeEnter()
    {
        if (PlayerPrefs.GetInt(GetCompletedKey(), 0) == 1) return;
        isHovered = true;
    }

    public void OnGazeExit()
    {
        isHovered = false;
        ResetTimer();
    }

    private void Teleport()
    {
        Debug.Log("Teleporting to scene: " + sceneToLoad);
        isHovered = false;
        ResetTimer();
        SceneManager.LoadScene(sceneToLoad);
    }
}