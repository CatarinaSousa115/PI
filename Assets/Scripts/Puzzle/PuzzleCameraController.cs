using System.Collections;
using UnityEngine;

/// <summary>
/// Smoothly transitions the camera between normal player view and a close-up puzzle view.
///
/// FIX vs. v1:
///  - GetPuzzleRotation() now derives look direction as (framePos - camPos).normalized,
///    so the camera always looks TOWARD the frame regardless of which direction
///    the frame's forward axis happens to point.
///  - SavePlayerCameraState() is called just before transitioning out (not in Start),
///    so it captures the exact position at the moment the player interacts.
/// </summary>
public class PuzzleCameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector Fields
    // ─────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Camera to move. Defaults to Camera.main.")]
    public Camera targetCamera;

    [Tooltip("The frame transform — used to compute the puzzle view position/rotation.")]
    public Transform frameTransform;

    [Header("Puzzle View")]
    [Tooltip("Optional: place an empty GameObject in front of the frame. " +
             "The camera will move to this exact position and rotation.")]
    public Transform puzzleViewPoint;

    [Tooltip("If no puzzleViewPoint: distance in front of the frame the camera sits.")]
    public float cameraDistance = 1.5f;

    [Tooltip("FOV during puzzle mode.")]
    public float puzzleFOV = 40f;

    [Header("Transition")]
    public float transitionDuration = 0.6f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private Vector3 _savedPosition;
    private Quaternion _savedRotation;
    private float _savedFOV;

    private Coroutine _activeTransition;
    private bool _inPuzzleView;

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    public void TransitionToPuzzleView()
    {
        if (_inPuzzleView) return;
        _inPuzzleView = true;

        // Capture player camera state NOW (not in Start) so we get the actual current position.
        SavePlayerState();

        Vector3 toPos = GetPuzzlePosition();
        Quaternion toRot = GetPuzzleRotation(toPos);

        Transition(toPos, toRot, puzzleFOV);
    }

    public void TransitionToPlayerView()
    {
        if (!_inPuzzleView) return;
        _inPuzzleView = false;

        Transition(_savedPosition, _savedRotation, _savedFOV);
    }

    public bool IsInPuzzleView => _inPuzzleView;

    // ─────────────────────────────────────────────
    // Private
    // ─────────────────────────────────────────────

    private void SavePlayerState()
    {
        _savedPosition = targetCamera.transform.position;
        _savedRotation = targetCamera.transform.rotation;
        _savedFOV = targetCamera.fieldOfView;
    }

    private Vector3 GetPuzzlePosition()
    {
        if (puzzleViewPoint != null)
            return puzzleViewPoint.position;

        if (frameTransform != null)
        {
            // Stand in front of the frame along its -forward axis.
            // frame.forward typically points INTO the wall (away from the room),
            // so -forward points toward the room / toward the player.
            return frameTransform.position - frameTransform.forward * cameraDistance;
        }

        return targetCamera.transform.position;
    }

    private Quaternion GetPuzzleRotation(Vector3 fromPosition)
    {
        if (puzzleViewPoint != null)
            return puzzleViewPoint.rotation;

        if (frameTransform != null)
        {
            // FIX: look direction = from camera TO frame centre.
            // This is correct regardless of how frameTransform.forward is oriented.
            Vector3 lookDir = (frameTransform.position - fromPosition).normalized;

            // Use frame's up axis to keep the camera upright relative to the painting.
            return Quaternion.LookRotation(lookDir, frameTransform.up);
        }

        return targetCamera.transform.rotation;
    }

    private void Transition(Vector3 toPos, Quaternion toRot, float toFOV)
    {
        if (_activeTransition != null)
            StopCoroutine(_activeTransition);

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

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));

            targetCamera.transform.position = Vector3.Lerp(fromPos, toPos, t);
            targetCamera.transform.rotation = Quaternion.Slerp(fromRot, toRot, t);
            targetCamera.fieldOfView = Mathf.Lerp(fromFOV, toFOV, t);

            yield return null;
        }

        // Snap to exact values
        targetCamera.transform.position = toPos;
        targetCamera.transform.rotation = toRot;
        targetCamera.fieldOfView = toFOV;
        _activeTransition = null;
    }

    // ─────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (targetCamera == null) return;

        Vector3    pos = GetPuzzlePosition();
        Quaternion rot = GetPuzzleRotation(pos);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(pos, 0.05f);

        // Draw the camera's look direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos, rot * Vector3.forward * 0.5f);

        if (frameTransform != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawLine(pos, frameTransform.position);
        }
    }
#endif
}