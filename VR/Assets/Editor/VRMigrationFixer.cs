using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class VRMigrationFixer : EditorWindow
{
    [MenuItem("VR Migration/1. Apply VR Logic Fixes")]
    public static void ApplyFixes()
    {
        Scene currentScene = EditorSceneManager.GetActiveScene();
        bool changed = false;
        
        // 1. Replace XR Origin
        GameObject oldRig = GameObject.Find("XR Origin (XR Rig)");
        if (oldRig != null)
        {
            Undo.DestroyObjectImmediate(oldRig);
            Debug.Log("[VR Migration] Removed old 'XR Origin (XR Rig)'.");
            changed = true;
        }

        GameObject handsRig = GameObject.Find("XR Origin Hands (XR Rig)");
        if (handsRig != null)
        {
            Undo.DestroyObjectImmediate(handsRig);
            Debug.Log("[VR Migration] Removed old 'XR Origin Hands (XR Rig)'.");
            changed = true;
        }

        // We check if the new one already exists
        GameObject existingRig = GameObject.Find("Complete XR Origin Set Up Variant") ?? GameObject.Find("Complete XR Origin Set Up Hands Variant");
        if (existingRig == null)
        {
            string prefabPath = "Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Variant.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject newRig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (newRig != null)
                {
                    Undo.RegisterCreatedObjectUndo(newRig, "Instantiate Complete XR Origin");
                    Debug.Log("[VR Migration] Instantiated 'Complete XR Origin Set Up Variant'. Locomotion is now ready!");
                    
                    // Attempt to find the lobby spawn point to prevent spawning outside
                    GameObject lobbySpawn = GameObject.Find("Lobby 1.1 Marker");
                    if (lobbySpawn != null)
                    {
                        newRig.transform.position = lobbySpawn.transform.position + Vector3.up * 0.1f;
                        newRig.transform.rotation = lobbySpawn.transform.rotation;
                        Debug.Log("[VR Migration] Teleported XR Origin to Lobby 1.1 Marker.");
                    }
                    existingRig = newRig;
                    changed = true;
                }
            }
            else
            {
                Debug.LogError("[VR Migration] Could not find prefab at " + prefabPath + ". Are you sure the VR Template Assets are imported?");
            }
        }
        else
        {
            Debug.Log("[VR Migration] A 'Complete XR Origin' is already in the scene.");
        }

        // 3. Add Collision Fixer to the Rig
        if (existingRig != null)
        {
            if (existingRig.GetComponent<VRCollisionFixer>() == null)
            {
                Undo.AddComponent<VRCollisionFixer>(existingRig);
                Debug.Log("[VR Migration] Added VRCollisionFixer to the XR Origin to prevent phasing through walls.");
                changed = true;
            }

            if (!existingRig.CompareTag("Player"))
            {
                Undo.RecordObject(existingRig, "Set Player Tag");
                existingRig.tag = "Player";
                Debug.Log("[VR Migration] Set XR Origin tag to 'Player' for trigger zones.");
                changed = true;
            }
        }

        // 4. Remove Desktop FPS Controller to revert to pure VR
        if (existingRig != null)
        {
            var desktopCtrl = existingRig.GetComponent("DesktopFPSController");
            if (desktopCtrl != null)
            {
                Undo.DestroyObjectImmediate(desktopCtrl);
                Debug.Log("[VR Migration] Removed DesktopFPSController. Reverting to native VR controls.");
                changed = true;
            }
        }

        // 5. Restore Missing Narrative & Game Managers (This handles opening the door after Dialogue)
        MuseumGame.MuseumGameManager gameManager = FindObjectOfType<MuseumGame.MuseumGameManager>();
        MuseumGame.Narrative.LobbyIntroController introController = FindObjectOfType<MuseumGame.Narrative.LobbyIntroController>();
        
        if (gameManager == null || introController == null)
        {
            GameObject managerObj = GameObject.Find("GameManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("GameManager");
                Undo.RegisterCreatedObjectUndo(managerObj, "Create GameManager");
            }
            
            if (gameManager == null)
            {
                Undo.AddComponent<MuseumGame.MuseumGameManager>(managerObj);
                Debug.Log("[VR Migration] Restored missing MuseumGameManager.");
            }
            
            if (introController == null)
            {
                introController = Undo.AddComponent<MuseumGame.Narrative.LobbyIntroController>(managerObj);
                Debug.Log("[VR Migration] Restored missing LobbyIntroController (This opens the door after Dom Pedro's dialogue).");
                
                // Set the default required player name to the XR Origin so the script can find it
                introController.playerName = "Complete XR Origin Set Up Variant";
            }
            changed = true;
        }

        // 2. Add VRPuzzlePiece to all puzzle pieces
        PuzzlePiece[] pieces = FindObjectsOfType<PuzzlePiece>(true);
        int addedCount = 0;
        foreach (var piece in pieces)
        {
            if (piece.gameObject.GetComponent<VRPuzzlePiece>() == null)
            {
                Undo.AddComponent<VRPuzzlePiece>(piece.gameObject);
                addedCount++;
                changed = true;
            }
        }
        
        if (addedCount > 0)
        {
            Debug.Log($"[VR Migration] Added VRPuzzlePiece (and XRGrabInteractable/Rigidbody) to {addedCount} puzzle pieces.");
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            Debug.Log("[VR Migration] VR Logic Fixes Applied Successfully! Please save your scene (Ctrl+S).");
        }
        else
        {
            Debug.Log("[VR Migration] No changes were needed. Everything looks set up.");
        }
    }
}
