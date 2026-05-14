using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MuseumGame.Player;
using MuseumGame.Blockout;
using System.Collections;

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
        if (_isTeleporting) return;

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
        if (SceneManager.GetSceneByName(sceneToLoad).isLoaded) return;
        if (_isTeleporting) return;

        _isTeleporting = true;
        isPlayerLooking = false;

        GazeManager gazeManager = FindFirstObjectByType<GazeManager>();
        if (gazeManager != null) gazeManager.DisableGaze();

        Debug.Log("Teleporting to scene: " + sceneToLoad);
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAdditive());
    }

    private IEnumerator LoadSceneAdditive()
    {
        yield return new WaitForEndOfFrame();

        Scene currentScene = SceneManager.GetActiveScene();
        foreach (GameObject obj in currentScene.GetRootGameObjects())
        {
            // Não desativas o XR Origin nem o GameManager!
            if (obj.name == "XR Origin (XR Rig)" ||
                obj.name == "GameManager" ||
                obj.name == "DontDestroyOnLoad")
                continue;

            obj.SetActive(false);
        }

        var layout = FindFirstObjectByType<MuseumWhiteboxRuntimeLayout>();
        if (layout != null) layout.ClearCorridors();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        yield return null;
        yield return null;
        op.allowSceneActivation = true;

        yield return new WaitUntil(() => op.isDone);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneToLoad));
    }
}