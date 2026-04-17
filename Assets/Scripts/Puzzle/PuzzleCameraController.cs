using System.Collections;
using UnityEngine;

namespace Puzzle
{
    public class PuzzleCameraController : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector Fields
        // ─────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The camera to move. Defaults to Camera.main if left empty.")]
        public Camera targetCamera;

        [Tooltip("The frame transform (used to compute default puzzle view if puzzleViewPoint is empty).")]
        public Transform frameTransform;

        [Header("Puzzle View Point")]
        [Tooltip("Optional: place an empty GameObject in front of the frame. The camera will move here.")]
        public Transform puzzleViewPoint;

        [Tooltip("If puzzleViewPoint is empty, how far in front of the frame the camera sits.")]
        public float cameraDistance = 1.5f;

        [Tooltip("Field of view during puzzle mode.")]
        public float puzzleFOV = 40f;

        [Header("Transition")]
        [Tooltip("Duration of the camera move/rotate transition (seconds).")]
        public float transitionDuration = 0.6f;

        [Tooltip("Easing curve for the transition.")]
        public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Player Camera Reference")]
        [Tooltip("If your player has a camera rig/look script, assign its Transform here so we can " +
                 "restore its position. Leave empty to auto-save position on Start.")]
        public Transform playerCameraAnchor;

        // ─────────────────────────────────────────────
        // Private
        // ─────────────────────────────────────────────

        private Vector3 _savedPosition;
        private Quaternion _savedRotation;
        private float _savedFOV;

        private Coroutine _transitionRoutine;
        private bool _inPuzzleView;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            // Fallback for Play Mode
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Start()
        {
            SavePlayerCameraState();
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        public void TransitionToPuzzleView()
        {
            if (_inPuzzleView) return;
            _inPuzzleView = true;

            SavePlayerCameraState();

            Vector3 targetPos = GetPuzzlePosition();
            Quaternion targetRot = GetPuzzleRotation();

            StartTransition(targetPos, targetRot, puzzleFOV);
        }

        public void TransitionToPlayerView()
        {
            if (!_inPuzzleView) return;
            _inPuzzleView = false;

            StartTransition(_savedPosition, _savedRotation, _savedFOV);
        }

        public bool IsInPuzzleView => _inPuzzleView;

        // ─────────────────────────────────────────────
        // Private Logic
        // ─────────────────────────────────────────────

        private void SavePlayerCameraState()
        {
            // Use targetCamera or Camera.main safely
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Transform anchor = playerCameraAnchor != null ? playerCameraAnchor : cam.transform;
            _savedPosition = anchor.position;
            _savedRotation = anchor.rotation;
            _savedFOV = cam.fieldOfView;
        }

        private Vector3 GetPuzzlePosition()
        {
            if (puzzleViewPoint != null)
                return puzzleViewPoint.position;

            if (frameTransform != null)
                return frameTransform.position - frameTransform.forward * cameraDistance;

            // Editor/Runtime Fallback to prevent NullReferenceException
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null)
                return cam.transform.position;

            return transform.position;
        }

        private Quaternion GetPuzzleRotation()
        {
            if (puzzleViewPoint != null)
                return puzzleViewPoint.rotation;

            if (frameTransform != null)
                return Quaternion.LookRotation(frameTransform.forward);

            // Editor/Runtime Fallback
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam != null)
                return cam.transform.rotation;

            return transform.rotation;
        }

        private void StartTransition(Vector3 toPos, Quaternion toRot, float toFOV)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);

            _transitionRoutine = StartCoroutine(
                TransitionRoutine(
                    targetCamera.transform.position,
                    targetCamera.transform.rotation,
                    targetCamera.fieldOfView,
                    toPos, toRot, toFOV
                )
            );
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

            targetCamera.transform.position = toPos;
            targetCamera.transform.rotation = toRot;
            targetCamera.fieldOfView = toFOV;

            _transitionRoutine = null;
        }
    }
}