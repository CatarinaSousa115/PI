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
        // Mostra o Lobby novamente
        Scene lobby = SceneManager.GetSceneByName(lobbyScene);
        foreach (GameObject obj in lobby.GetRootGameObjects())
        {
            obj.SetActive(true);
        }

        // Descarrega a Painting
        SceneManager.UnloadSceneAsync(gameObject.scene.name);
    }
    private void ShowLobby()
    {
        Scene lobby = SceneManager.GetSceneByName(this.lobbyScene);
        foreach (GameObject obj in lobby.GetRootGameObjects())
        {
            if (obj.name == "XR Origin (XR Rig)" ||
                obj.name == "GameManager" ||
                obj.name == "DontDestroyOnLoad")
                continue;

            obj.SetActive(true);
        }
    }
}