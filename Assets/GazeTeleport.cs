using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MuseumGame.Player;

public class GazeTeleport : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;
    public float gazeTime = 5f;

    [Header("Visual Feedback (Optional)")]
    public Image loadingBar;
    public Renderer targetRenderer;

    private float timer = 0f;
    private bool isPlayerLooking = false;
    private bool _isTeleporting = false; 
    private Renderer _activeRenderer;
    private Color _originalColor;
    private bool _hasEmission;

    void Awake()
    {
        _activeRenderer = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
        if (_activeRenderer != null)
        {
            _originalColor = _activeRenderer.material.color;
            _hasEmission = _activeRenderer.material.HasProperty("_EmissionColor");
        }
    }

    void Update()
    {
        if (_isTeleporting) return; // ← bloqueia durante teleporte

        if (isPlayerLooking)
        {
            timer += Time.deltaTime;
            if (loadingBar != null)
                loadingBar.fillAmount = timer / gazeTime;

            if (timer >= gazeTime)
                Teleport();
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
                _activeRenderer.material.SetColor("_EmissionColor", Color.white * 0.5f);
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
                _activeRenderer.material.SetColor("_EmissionColor", Color.black);
        }
        Debug.Log("Gaze ended.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && !isPlayerLooking)
        {
            isPlayerLooking = true;
            if (_activeRenderer != null) _activeRenderer.material.color = Color.blue;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            OnGazeExit();
    }

    private void Teleport()
    {
        if (string.IsNullOrWhiteSpace(sceneToLoad)) return;
        if (SceneManager.GetActiveScene().name == sceneToLoad) return;
        if (_isTeleporting) return;

        _isTeleporting = true;
        isPlayerLooking = false;

        GazeManager gazeManager = FindFirstObjectByType<GazeManager>(); // ← corrigido
        if (gazeManager != null) gazeManager.DisableGaze();

        Debug.Log("Teleporting to scene: " + sceneToLoad);
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneDelayed());
    }

    private System.Collections.IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForEndOfFrame();
        
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.allowSceneActivation = false;

        yield return null;
        yield return null;

        op.allowSceneActivation = true;
    }
}