using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRPuzzlePiece : MonoBehaviour
{
    public PuzzlePiece pieceData;
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rb;
    private PuzzleDragger _dragger;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        
        // Puzzle pieces usually shouldn't have gravity so they don't fall to the floor
        _rb.useGravity = false;
        _rb.isKinematic = true; 

        // Important for VR puzzle pieces: track position but don't let physics throw them out of the room
        _grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;

        pieceData = GetComponent<PuzzlePiece>();
        _dragger = GetComponent<PuzzleDragger>();

        _grabInteractable.selectEntered.AddListener(OnGrabbed);
        _grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (pieceData == null || pieceData.IsLocked)
        {
            // Force drop if locked
            _grabInteractable.interactionManager.SelectCancel(args.interactorObject, _grabInteractable);
            return;
        }
        
        // Remove puzzle piece from board so we can hold it
        if (_dragger != null)
        {
            _dragger.enabled = false;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (pieceData == null || pieceData.IsLocked) return;

        // When dropped, check if it's close enough to snap to the board
        if (_dragger != null)
        {
            _dragger.StopDragging(); 
        }
    }

    private void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}