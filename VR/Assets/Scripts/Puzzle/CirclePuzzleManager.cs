using UnityEngine;
using UnityEngine.SceneManagement;

public class CirclePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int totalCircles = 5;
    public string sceneToLoad = "BasicScene";
    
    private int activatedCount = 0;
    
    // We use this static variable to remember that the player just beat the puzzle
    public static bool justCompletedPuzzle = false;

    // Use a static constructor to hook into sceneLoaded so it survives scene changes
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
    }

    void Start()
    {
        // Force to 5 to avoid Unity Inspector old saved values overriding it
        totalCircles = 5;

        // Find ALL interactors, even if they are currently turned off by the simulator!
        var interactors = Resources.FindObjectsOfTypeAll<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        foreach (var interactor in interactors)
        {
            // We only want to modify interactors that are part of this scene (not prefabs in the project)
            if (interactor.gameObject.scene.isLoaded)
            {
                interactor.maxRaycastDistance = 100f;

                // IMPORTANT: We do not set the sphereCastRadius high here!
                // If it is too big, the ray immediately hits your own VR body/UI and stops at 0 distance!
                // So we ensure it is small enough to pass through your own colliders.
                interactor.sphereCastRadius = 0.1f;
            }
        }
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        // If we just loaded BasicScene after beating the puzzle
        if (justCompletedPuzzle && scene.name == "BasicScene")
        {
            justCompletedPuzzle = false; // Reset the flag
            
            // Find the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            // Try to find the Room 2 location markers from your project
            GameObject room2Marker = GameObject.Find("Sala 2 - 1.2 Marker");
            if (room2Marker == null) room2Marker = GameObject.Find("Sala 2 - 1.2/Sala 2 - 1.2 Marker");
            if (room2Marker == null) room2Marker = GameObject.Find("Sala 2 - 1.2");
            if (room2Marker == null)
            {
                // Fallback: search all objects for the name
                foreach (var obj in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (obj.name.Contains("Sala 2") && obj.name.Contains("Marker") && obj.gameObject.scene.isLoaded)
                    {
                        room2Marker = obj.gameObject;
                        break;
                    }
                }
            }

            if (player != null && room2Marker != null)
            {
                Debug.Log("[CirclePuzzleManager] Teleporting player to Room 2!");
                
                // Teleport the entire XR Origin
                player.transform.position = room2Marker.transform.position;
                
                // Add a slight height boost so they don't clip into the floor
                player.transform.position += Vector3.up * 1.5f; 
            }
            else
            {
                Debug.LogWarning("[CirclePuzzleManager] Could not find Player or Room 2 Marker to teleport!");
            }
        }
    }

    public void OnCircleActivated()
    {
        activatedCount++;
        Debug.Log($"Circle activated! {activatedCount} / {totalCircles}");

        if (activatedCount >= totalCircles)
        {
            Debug.Log("All circles activated! Teleporting...");
            
            // Set the flag before loading so the next scene knows to teleport us to Room 2
            justCompletedPuzzle = true;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
