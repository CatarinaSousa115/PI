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
    public Renderer targetRenderer; // The object that should glow (e.g., the frame)

    private float timer = 0f;
    private bool isPlayerLooking = false;
    private Renderer _activeRenderer;
    private Color _originalColor;
    private bool _hasEmission;

    void Awake()
    {
        // Use targetRenderer if assigned, otherwise use the one on this object
        _activeRenderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
        
        if (_activeRenderer != null)
        {
            _originalColor = _activeRenderer.material.color;
            _hasEmission = _activeRenderer.material.HasProperty("_EmissionColor");
        }
    }

    void Update()
    {
        if (isPlayerLooking)
        {
            timer += Time.deltaTime;

            if (loadingBar != null)
            {
                loadingBar.fillAmount = timer / gazeTime;
            }

            if (timer >= gazeTime)
            {
                Teleport();
            }
        }
        else
        {
            timer = 0f;
            if (loadingBar != null) loadingBar.fillAmount = 0;
        }
    }
    
    public void OnGazeEnter()
    {
        isPlayerLooking = true;
        if (_activeRenderer != null) 
        {
            _activeRenderer.material.color = Color.white;
            if (_hasEmission)
            {
                _activeRenderer.material.EnableKeyword("_EMISSION");
                _activeRenderer.material.SetColor("_EmissionColor", Color.white * 0.5f); // Soft glow
            }
        }
        Debug.Log("Gaze started on: " + gameObject.name);
    }

    public void OnGazeExit()
    {
        isPlayerLooking = false;
        if (_activeRenderer != null) 
        {
            _activeRenderer.material.color = _originalColor;
            if (_hasEmission)
            {
                _activeRenderer.material.SetColor("_EmissionColor", Color.black);
            }
        }
        Debug.Log("Gaze ended.");
    }

    // Fallback for standard Unity mouse events if a PhysicsRaycaster is on the camera
    private void OnMouseEnter()
    {
        OnGazeEnter();
    }

    private void OnMouseExit()
    {
        OnGazeExit();
    }

    // Trigger fallback if player is just standing very close
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Only trigger if we aren't already looking (to avoid double timer speed)
            if (!isPlayerLooking)
            {
                isPlayerLooking = true;
                if (_activeRenderer != null) _activeRenderer.material.color = Color.blue; // Blue for trigger
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnGazeExit();
        }
    }

    private void Teleport()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad)) return;
        
        // Safety: don't load same scene twice if it's already loading
        if (SceneManager.GetActiveScene().name == sceneToLoad) return;

        Debug.Log("Teleporting to scene: " + sceneToLoad);
        isPlayerLooking = false;
        
        // Ensure time is running
        Time.timeScale = 1f;
        
        StartCoroutine(LoadSceneDelayed());
    }

    private System.Collections.IEnumerator LoadSceneDelayed()
    {
        // Wait a very short moment to finish the frame
        yield return new WaitForEndOfFrame();
        SceneManager.LoadScene(sceneToLoad);
    }
    
    
}