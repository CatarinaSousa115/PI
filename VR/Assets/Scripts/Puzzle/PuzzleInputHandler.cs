using UnityEngine;

public class PuzzleInputHandler : MonoBehaviour
{
    public Camera puzzleCamera;
    private PuzzleDragger _currentDragger;

    void Start()
    {
        if (puzzleCamera == null) puzzleCamera = Camera.main;
    }

    void Update()
    {
        if (PuzzleManager.Instance == null || !PuzzleManager.Instance.IsActive) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = puzzleCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit,100f))
            {
                Debug.Log($"[Raycast] Hit: {hit.collider.gameObject.name}");


                var dragger = hit.collider.GetComponent<PuzzleDragger>();
                if (dragger != null && !dragger.piece.IsLocked)
                {
                    _currentDragger = dragger;
                    _currentDragger.StartDragging(ray);
                }
            }
            else
            {
                Debug.Log("[Raycast] Hit: absolutely nothing.");
            }
        }

        if (Input.GetMouseButton(0) && _currentDragger != null)
        {
            _currentDragger.FollowMouse(puzzleCamera.ScreenPointToRay(Input.mousePosition));
        }

        if (Input.GetMouseButtonUp(0) && _currentDragger != null)
        {
            _currentDragger.StopDragging();
            _currentDragger = null;
        }
    }
}