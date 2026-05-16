using UnityEngine;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(XROrigin))]
public class VRCollisionFixer : MonoBehaviour
{
    private CharacterController _characterController;
    private XROrigin _xrOrigin;

    void Start()
    {
        _characterController = GetComponent<CharacterController>();
        _xrOrigin = GetComponent<XROrigin>();
        
        // Define a reasonable radius for the player's body
        _characterController.radius = 0.2f;
    }

    void Update()
    {
        UpdateCharacterController();
    }

    void UpdateCharacterController()
    {
        if (_xrOrigin == null || _xrOrigin.Camera == null) return;
        
        // Match the CharacterController height to the headset height
        float height = Mathf.Clamp(_xrOrigin.CameraInOriginSpaceHeight, 0.5f, 2.5f);
        _characterController.height = height;

        // Match the center of the CharacterController to the headset X and Z
        Vector3 center = _xrOrigin.CameraInOriginSpacePos;
        // Adjust Y so the capsule sits exactly on the floor
        center.y = height / 2f + _characterController.skinWidth;
        _characterController.center = center;
    }
}