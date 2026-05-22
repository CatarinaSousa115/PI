using UnityEngine;
using UnityEngine.UI; // Required if you use a loading circle
using UnityEngine.SceneManagement;

public class GazeTeleport : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;      // Name of the scene for this painting
    public float gazeTime = 5f;     // How many seconds to look?

    [Header("Visual Feedback (Optional)")]
    public Image loadingBar;        // A UI Image set to "Filled" mode

    private float timer = 0f;
    private bool isPlayerLooking = false;

    void Update()
    {
        if (isPlayerLooking)
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
            // Reset if they look away
            timer = 0f;
            if (loadingBar != null) loadingBar.fillAmount = 0;
        }
    }
    
    public void OnGazeEnter()
    {
        isPlayerLooking = true;
        // GetComponent<Renderer>().material.color = Color.red;
        Debug.Log("Eye contact made with: " + gameObject.name);
    }

    public void OnGazeExit()
    {
        isPlayerLooking = false;
        // GetComponent<Renderer>().material.color = Color.white;
        Debug.Log("Lost eye contact.");
    }

    private void Teleport()
    {
        Debug.Log("Teleporting to scene: " + sceneToLoad);
        isPlayerLooking = false;
        SceneManager.LoadScene(sceneToLoad);
    }
    
    
}