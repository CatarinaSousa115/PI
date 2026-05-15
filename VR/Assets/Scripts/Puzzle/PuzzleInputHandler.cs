using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleInputHandler : MonoBehaviour
{
    [Header("VR Input")]
    public Transform rayOrigin;
    public InputActionProperty triggerAction;
    public float rayDistance = 10f;

    private PuzzleDragger _currentDragger;
    private bool _wasPressedLastFrame;

    void Update()
    {
        if (PuzzleManager.Instance == null || !PuzzleManager.Instance.IsActive)
            return;

        if (rayOrigin == null)
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        bool pressed = triggerAction.action != null && triggerAction.action.IsPressed();

        if (pressed && !_wasPressedLastFrame)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                Debug.Log($"[VR Raycast] Hit: {hit.collider.gameObject.name}");

                var dragger = hit.collider.GetComponent<PuzzleDragger>();

                if (dragger != null && !dragger.piece.IsLocked)
                {
                    _currentDragger = dragger;
                    _currentDragger.StartDragging(ray);
                }
            }
            else
            {
                Debug.Log("[VR Raycast] Hit: absolutely nothing.");
            }
        }

        if (pressed && _currentDragger != null)
        {
            _currentDragger.FollowRay(ray);
        }

        if (!pressed && _wasPressedLastFrame && _currentDragger != null)
        {
            _currentDragger.StopDragging();
            _currentDragger = null;
        }

        _wasPressedLastFrame = pressed;
    }
}