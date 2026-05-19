using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(XROrigin))]
public class VRCollisionFixer : MonoBehaviour
{
    [Header("Body")]
    public float bodyRadius = 0.28f;
    public float minBodyHeight = 0.6f;
    public float maxBodyHeight = 2.4f;

    [Header("Wall Blocking")]
    public bool blockDirectCameraMovement = true;
    public LayerMask collisionLayers = ~0;
    public float wallPadding = 0.04f;

    private CharacterController _characterController;
    private XROrigin _xrOrigin;
    private readonly Collider[] _overlapResults = new Collider[16];
    private Vector3 _lastValidCameraPosition;
    private Vector3 _lastValidCapsuleBottom;
    private Vector3 _lastValidCapsuleTop;
    private bool _hasValidPose;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _xrOrigin = GetComponent<XROrigin>();

        _characterController.radius = bodyRadius;
        MarkCurrentPoseValid();
    }

    void Update()
    {
        UpdateCharacterController();
        BlockWallClipping();
    }

    void UpdateCharacterController()
    {
        if (_xrOrigin == null || _xrOrigin.Camera == null) return;
        
        // Match the CharacterController height to the headset height
        float height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, minBodyHeight, maxBodyHeight);
        _characterController.height = height;
        _characterController.radius = bodyRadius;

        // Match the center of the CharacterController to the headset X and Z
        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
        // Adjust Y so the capsule sits exactly on the floor
        center.y = height / 2f + _characterController.skinWidth;
        _characterController.center = center;
    }

    private void BlockWallClipping()
    {
        if (!blockDirectCameraMovement ||
            _characterController == null ||
            !_characterController.enabled ||
            _xrOrigin == null ||
            _xrOrigin.Camera == null)
        {
            _hasValidPose = false;
            return;
        }

        GetCurrentCapsule(out Vector3 bottom, out Vector3 top);

        if (!_hasValidPose)
        {
            if (!IsCapsuleOverlappingSolid(bottom, top))
                MarkCurrentPoseValid();

            return;
        }

        if (DidCrossSolid(bottom, top) || IsCapsuleOverlappingSolid(bottom, top))
        {
            RestoreLastValidPose();
            return;
        }

        MarkCurrentPoseValid();
    }

    private bool DidCrossSolid(Vector3 currentBottom, Vector3 currentTop)
    {
        Vector3 currentCameraPosition = _xrOrigin.Camera.transform.position;
        Vector3 movement = currentCameraPosition - _lastValidCameraPosition;
        movement.y = 0f;

        float distance = movement.magnitude;
        if (distance <= 0.001f)
            return false;

        RaycastHit[] hits = Physics.CapsuleCastAll(
            _lastValidCapsuleBottom,
            _lastValidCapsuleTop,
            bodyRadius,
            movement / distance,
            distance + wallPadding,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (IsBlockingCollider(hit.collider))
                return true;
        }

        return false;
    }

    private bool IsCapsuleOverlappingSolid(Vector3 bottom, Vector3 top)
    {
        int count = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            bodyRadius,
            _overlapResults,
            collisionLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (IsBlockingCollider(_overlapResults[i]))
                return true;
        }

        return false;
    }

    private bool IsBlockingCollider(Collider collider)
    {
        if (collider == null || collider.isTrigger)
            return false;

        return !collider.transform.IsChildOf(transform);
    }

    private void RestoreLastValidPose()
    {
        Vector3 cameraPosition = _xrOrigin.Camera.transform.position;
        Vector3 correction = _lastValidCameraPosition - cameraPosition;
        correction.y = 0f;

        transform.position += correction;
        UpdateCharacterController();
    }

    private void MarkCurrentPoseValid()
    {
        if (_xrOrigin == null || _xrOrigin.Camera == null || _characterController == null)
            return;

        _lastValidCameraPosition = _xrOrigin.Camera.transform.position;
        GetCurrentCapsule(out _lastValidCapsuleBottom, out _lastValidCapsuleTop);
        _hasValidPose = true;
    }

    private void GetCurrentCapsule(out Vector3 bottom, out Vector3 top)
    {
        Vector3 center = transform.TransformPoint(_characterController.center);
        float radius = Mathf.Max(0.01f, _characterController.radius);
        float halfSegment = Mathf.Max(0f, (_characterController.height * 0.5f) - radius);

        bottom = center + Vector3.down * halfSegment;
        top = center + Vector3.up * halfSegment;
    }
}
