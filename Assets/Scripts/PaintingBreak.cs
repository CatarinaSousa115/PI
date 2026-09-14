using UnityEngine;

public class PaintingBreak : MonoBehaviour
{
    // MUST be 'public GameObject'
    public GameObject wholePainting;
    public GameObject messageText;   // We will drag the Canvas here
    public GameObject piecesGroup;   // We will drag the Puzzle Pieces here later

    public void BreakArt()
    {
        if (wholePainting != null) wholePainting.SetActive(false);
        if (messageText != null) messageText.SetActive(false);
        if (piecesGroup != null) piecesGroup.SetActive(true);

        Debug.Log("Art Touched! Hiding message and painting.");
    }
}