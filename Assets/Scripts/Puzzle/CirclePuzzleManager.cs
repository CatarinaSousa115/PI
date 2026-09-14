using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CirclePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int totalCircles = 1;
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
        // Force to 1 to avoid Unity Inspector old saved values overriding it
        totalCircles = 1;

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
            ScheduleOpenAllDoorLeaves();
            
            // Find the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            
            // Try to find the Room 4 location markers from your project
            GameObject room4Marker = GameObject.Find("Sala 4 - 1.5 Marker");
            if (room4Marker == null) room4Marker = GameObject.Find("Sala 4 - 1.5/Sala 4 - 1.5 Marker");
            if (room4Marker == null) room4Marker = GameObject.Find("Sala 4 - 1.5");
            if (room4Marker == null)
            {
                // Fallback: search all objects for the name
                foreach (var obj in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (obj.name.Contains("Sala 4") && obj.name.Contains("Marker") && obj.gameObject.scene.isLoaded)
                    {
                        room4Marker = obj.gameObject;
                        break;
                    }
                }
            }

            if (player != null && room4Marker != null)
            {
                Debug.Log("[CirclePuzzleManager] Teleporting player to Room 4!");
                
                // Teleport the entire XR Origin
                player.transform.position = room4Marker.transform.position;
                
                // Set the orientation to match the marker's rotation
                player.transform.rotation = room4Marker.transform.rotation;
                
                // Add a very slight height boost just to avoid clipping into the floor
                player.transform.position += Vector3.up * 0.05f; 
            }
            else
            {
                Debug.LogWarning("[CirclePuzzleManager] Could not find Player or Room 4 Marker to teleport!");
            }
        }
    }

    private static void ScheduleOpenAllDoorLeaves()
    {
        GameObject runnerObject = new GameObject("Room4DoorOpenRunner");
        runnerObject.AddComponent<DoorOpenRunner>();
    }

    private sealed class DoorOpenRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            OpenAllDoorLeaves();
            Destroy(gameObject);
        }
    }

    private static void OpenAllDoorLeaves()
    {
        int openedDoors = 0;
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform current in transforms)
        {
            if (current == null || !current.gameObject.scene.isLoaded || !IsDoorLeaf(current.name))
                continue;

            foreach (Renderer renderer in current.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (Collider collider in current.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            current.gameObject.SetActive(false);
            openedDoors++;
        }

        Debug.Log($"[CirclePuzzleManager] Opened/deactivated all door leaves after Room 4 minigame: {openedDoors}");
    }

    private static bool IsDoorLeaf(string objectName)
    {
        return !string.IsNullOrWhiteSpace(objectName) &&
               objectName.StartsWith("To ", System.StringComparison.Ordinal) &&
               objectName.EndsWith("_Leaf", System.StringComparison.Ordinal);
    }

    public void OnCircleActivated()
    {
        activatedCount++;
        Debug.Log($"Circle activated! {activatedCount} / {totalCircles}");

        if (activatedCount >= totalCircles)
        {
            Debug.Log("All circles activated! Teleporting...");
            
            // Set the flag before loading so the next scene knows to teleport us to Room 4
            justCompletedPuzzle = true;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
