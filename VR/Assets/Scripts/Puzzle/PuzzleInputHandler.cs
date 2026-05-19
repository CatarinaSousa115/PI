using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;

public class PuzzleInputHandler : MonoBehaviour
{
    [Header("VR Input")]
    public Transform rayOrigin;
    public InputActionProperty triggerAction;
    public float rayDistance = 10f;
    public bool useXRControllerButtons = true;
    public bool allowGripButton = true;
    public bool preferRightController = true;

    private PuzzleDragger _currentDragger;
    private bool _wasPressedLastFrame;
    private Camera _fallbackCamera;
    private bool _usingFallbackCameraRay;
    private readonly System.Collections.Generic.List<XRInputDevice> _xrDevices = new();

    private void Awake()
    {
        ResolveRayOrigin();
    }

    void Update()
    {
        if (PuzzleManager.Instance == null || !PuzzleManager.Instance.IsActive)
            return;

        ResolveRayOrigin();

        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool xrPressed = useXRControllerButtons && IsXRControllerPressed();
        bool pressed = (triggerAction.action != null && triggerAction.action.IsPressed()) || xrPressed || mousePressed;

        if (!TryGetRay(mousePressed, out Ray ray))
            return;

        if (pressed && !_wasPressedLastFrame)
        {
            PuzzleDragger dragger = GetPuzzleDraggerFromRay(ray);

            if (dragger != null)
            {
                Debug.Log($"[VR Raycast] Hit puzzle piece: {dragger.gameObject.name}");

                if (!dragger.piece.IsLocked)
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

    private void ResolveRayOrigin()
    {
        if (_fallbackCamera == null)
            _fallbackCamera = Camera.main;

        if (rayOrigin == null || _usingFallbackCameraRay)
        {
            Transform xrRayOrigin = FindXRRayOrigin();
            if (xrRayOrigin != null)
            {
                rayOrigin = xrRayOrigin;
                _usingFallbackCameraRay = false;
                return;
            }
        }

        if (rayOrigin == null && _fallbackCamera != null)
        {
            rayOrigin = _fallbackCamera.transform;
            _usingFallbackCameraRay = true;
        }
    }

    private bool TryGetRay(bool mousePressed, out Ray ray)
    {
        if (mousePressed && _fallbackCamera != null && Mouse.current != null)
        {
            ray = _fallbackCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }

        if (rayOrigin != null)
        {
            ray = new Ray(rayOrigin.position, rayOrigin.forward);
            return true;
        }

        ray = default;
        return false;
    }

    private PuzzleDragger GetPuzzleDraggerFromRay(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            PuzzleDragger dragger = hit.collider.GetComponentInParent<PuzzleDragger>();
            if (dragger != null)
                return dragger;
        }

        return null;
    }

    private Transform FindXRRayOrigin()
    {
        Transform origin = FindRayOriginFromNearFarInteractor();
        if (origin != null)
            return origin;

        return FindRayOriginFromRayInteractor();
    }

    private Transform FindRayOriginFromNearFarInteractor()
    {
        NearFarInteractor[] interactors = FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        NearFarInteractor best = null;
        int bestScore = int.MinValue;

        foreach (NearFarInteractor interactor in interactors)
        {
            int score = ScoreInteractor(interactor);
            if (score > bestScore)
            {
                bestScore = score;
                best = interactor;
            }
        }

        return best != null ? ((IXRRayProvider)best).GetOrCreateRayOrigin() : null;
    }

    private Transform FindRayOriginFromRayInteractor()
    {
        XRRayInteractor[] interactors = FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        XRRayInteractor best = null;
        int bestScore = int.MinValue;

        foreach (XRRayInteractor interactor in interactors)
        {
            int score = ScoreInteractor(interactor);
            if (score > bestScore)
            {
                bestScore = score;
                best = interactor;
            }
        }

        return best != null ? ((IXRRayProvider)best).GetOrCreateRayOrigin() : null;
    }

    private int ScoreInteractor(MonoBehaviour interactor)
    {
        int score = interactor.isActiveAndEnabled ? 100 : 0;
        string objectName = interactor.name.ToLowerInvariant();

        if (objectName.Contains("teleport"))
            score -= 1000;

        if (preferRightController)
        {
            if (objectName.Contains("right")) score += 20;
            if (objectName.Contains("left")) score -= 10;
        }
        else
        {
            if (objectName.Contains("left")) score += 20;
            if (objectName.Contains("right")) score -= 10;
        }

        return score;
    }

    private bool IsXRControllerPressed()
    {
        InputDeviceCharacteristics hand = preferRightController
            ? InputDeviceCharacteristics.Right
            : InputDeviceCharacteristics.Left;

        if (IsXRControllerPressed(hand))
            return true;

        InputDeviceCharacteristics otherHand = preferRightController
            ? InputDeviceCharacteristics.Left
            : InputDeviceCharacteristics.Right;

        return IsXRControllerPressed(otherHand);
    }

    private bool IsXRControllerPressed(InputDeviceCharacteristics hand)
    {
        _xrDevices.Clear();
        InputDevices.GetDevicesWithCharacteristics(hand | InputDeviceCharacteristics.Controller, _xrDevices);

        foreach (XRInputDevice device in _xrDevices)
        {
            if (device.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool triggerPressed) && triggerPressed)
                return true;

            if (allowGripButton &&
                device.TryGetFeatureValue(XRCommonUsages.gripButton, out bool gripPressed) &&
                gripPressed)
            {
                return true;
            }
        }

        return false;
    }
}
