using System.Collections;
using UnityEngine;

public class PuzzleCameraController : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    public Transform frameTransform;

    [Header("Puzzle View")]
    public Transform puzzleViewPoint;
    public float cameraDistance = 1.5f;
    public float puzzleFOV = 40f;

    [Header("Transition")]
    public float transitionDuration = 0.6f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private float _savedFOV;
    private Coroutine _activeTransition;
    private bool _inPuzzleView;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    public void TransitionToPuzzleView()
    {
        if (_inPuzzleView) return;
        _inPuzzleView = true;

        SavePlayerState();

        Vector3 toPos = GetPuzzlePosition();
        Quaternion toRot = GetPuzzleRotation(toPos);

        Debug.Log($"[PuzzleCamera] Transitioning to Puzzle View. Target Pos: {toPos}");
        Transition(toPos, toRot, puzzleFOV);
    }

    public void TransitionToPlayerView()
    {
        if (!_inPuzzleView) return;
        _inPuzzleView = false;

        Debug.Log("[PuzzleCamera] Transitioning back to Player View.");
        Transition(_savedPosition, _savedRotation, _savedFOV);
    }

    public bool IsInPuzzleView => _inPuzzleView;

    private void SavePlayerState()
    {
        if (targetCamera == null) return;
        _savedPosition = targetCamera.transform.position;
        _savedRotation = targetCamera.transform.rotation;
        _savedFOV = targetCamera.fieldOfView;
    }

    private Vector3 GetPuzzlePosition()
    {
        if (puzzleViewPoint != null) return puzzleViewPoint.position;

        if (frameTransform != null)
        {
            // Stand in front of the frame.
            // Using -frameTransform.forward assuming forward points into the wall.
            return frameTransform.position - frameTransform.forward * cameraDistance;
        }

        return targetCamera.transform.position;
    }

    private Quaternion GetPuzzleRotation(Vector3 fromPosition)
    {
        if (puzzleViewPoint != null) return puzzleViewPoint.rotation;

        if (frameTransform != null)
        {
            Vector3 lookDir = (frameTransform.position - fromPosition).normalized;
            if (lookDir == Vector3.zero) lookDir = frameTransform.forward;
            return Quaternion.LookRotation(lookDir, frameTransform.up);
        }

        return targetCamera.transform.rotation;
    }

    private void Transition(Vector3 toPos, Quaternion toRot, float toFOV)
    {
        if (targetCamera == null) return;
        
        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(TransitionRoutine(
            targetCamera.transform.position,
            targetCamera.transform.rotation,
            targetCamera.fieldOfView,
            toPos, toRot, toFOV));
    }

    private IEnumerator TransitionRoutine(
        Vector3 fromPos, Quaternion fromRot, float fromFOV,
        Vector3 toPos, Quaternion toRot, float toFOV)
    {
        float elapsed = 0f;
        if (transitionDuration <= 0) elapsed = 1f; // Instant

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

            targetCamera.transform.position = Vector3.Lerp(fromPos, toPos, t);
            targetCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            targetCamera.fieldOfView = Mathf.Lerp(fromFOV, toFOV, t);

            yield return null;
        }

        targetCamera.transform.position = toPos;
        targetCamera.transform.rotation = toRot;
        targetCamera.fieldOfView = toFOV;
        _activeTransition = null;
        Debug.Log("[PuzzleCamera] Transition complete.");
    }
}