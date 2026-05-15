using UnityEngine;
using Unity.XR.CoreUtils;

public class PuzzleCameraController : MonoBehaviour
{
    [Header("XR Origin")]
    public XROrigin xrOrigin;

    [Header("Puzzle View")]
    public Transform puzzleViewPoint;
    public Transform frameTransform;
    public float cameraDistance = 1.5f;

    private Vector3 _savedOriginPosition;
    private Quaternion _savedOriginRotation;
    private bool _inPuzzleView;

    private void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();
    }

    public void TransitionToPuzzleView()
    {
        if (_inPuzzleView || xrOrigin == null)
            return;

        _inPuzzleView = true;

        _savedOriginPosition = xrOrigin.transform.position;
        _savedOriginRotation = xrOrigin.transform.rotation;

        Vector3 targetPosition = GetPuzzlePosition();
        Quaternion targetRotation = GetPuzzleRotation(targetPosition);

        xrOrigin.transform.SetPositionAndRotation(targetPosition, targetRotation);

        Debug.Log("[PuzzleCamera] XR Origin moved to puzzle view.");
    }

    public void TransitionToPlayerView()
    {
        if (!_inPuzzleView || xrOrigin == null)
            return;

        _inPuzzleView = false;

        xrOrigin.transform.SetPositionAndRotation(_savedOriginPosition, _savedOriginRotation);

        Debug.Log("[PuzzleCamera] XR Origin restored to player view.");
    }

    public bool IsInPuzzleView => _inPuzzleView;

    private Vector3 GetPuzzlePosition()
    {
        if (puzzleViewPoint != null)
            return puzzleViewPoint.position;

        if (frameTransform != null)
            return frameTransform.position - frameTransform.forward * cameraDistance;

        return xrOrigin.transform.position;
    }

    private Quaternion GetPuzzleRotation(Vector3 fromPosition)
    {
        if (puzzleViewPoint != null)
            return puzzleViewPoint.rotation;

        if (frameTransform != null)
        {
            Vector3 lookDir = (frameTransform.position - fromPosition).normalized;

            if (lookDir == Vector3.zero)
                lookDir = frameTransform.forward;

            return Quaternion.LookRotation(lookDir, Vector3.up);
        }

        return xrOrigin.transform.rotation;
    }
}