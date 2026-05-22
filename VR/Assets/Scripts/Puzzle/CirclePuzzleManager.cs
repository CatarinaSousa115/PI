using UnityEngine;
using UnityEngine.SceneManagement;

public class CirclePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int totalCircles = 5;
    public string sceneToLoad = "BasicScene";
    
    private int activatedCount = 0;

    public void OnCircleActivated()
    {
        activatedCount++;
        Debug.Log($"Circle activated! {activatedCount} / {totalCircles}");

        if (activatedCount >= totalCircles)
        {
            Debug.Log("All circles activated! Teleporting...");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
