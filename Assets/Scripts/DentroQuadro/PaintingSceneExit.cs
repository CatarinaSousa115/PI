using UnityEngine;
using UnityEngine.SceneManagement;

public class PaintingSceneExit : MonoBehaviour
{
    [Header("Settings")]
    public string lobbyScene = "Lobby";
    public KeyCode exitKey = KeyCode.Escape;

    void Update()
    {
        if (Input.GetKeyDown(exitKey))
            ExitPainting();
    }

    private void ExitPainting()
    {
        SceneManager.LoadScene(lobbyScene);
    }
}