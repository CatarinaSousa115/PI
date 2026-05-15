using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to each painting GameObject.
/// The painting plays an audio clue when the player interacts with it.
/// </summary>
public class PaintingInteractable : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector fields
    // ──────────────────────────────────────────────

    [Header("Audio")]
    [Tooltip("The clue audio clip this painting plays when interacted with.")]
    public AudioClip clueAudio;

    [Header("References")]
    public MinigameManager minigameManager;

    [Header("Visual Feedback (optional)")]
    [Tooltip("A highlight material to show when the painting is interactable.")]
    public Material highlightMaterial;
    [Tooltip("The Renderer to swap the material on.")]
    public Renderer paintingRenderer;

    // ──────────────────────────────────────────────
    // Internal state
    // ──────────────────────────────────────────────

    private bool _interactable = false;
    private bool _isPlaying = false;
    private Material _originalMaterial;
    private AudioSource _audio;
    private XRSimpleInteractable _xrInteractable;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.spatialBlend = 1f; // 3D audio — sound comes from the painting
        _audio.playOnAwake = false;

        _xrInteractable = GetComponent<XRSimpleInteractable>();

        if (_xrInteractable == null)
            _xrInteractable = gameObject.AddComponent<XRSimpleInteractable>();

        _xrInteractable.selectEntered.AddListener(OnXRInteract);

        if (paintingRenderer != null)
            _originalMaterial = paintingRenderer.material;

        // Paintings start locked until the minigame begins
        SetInteractable(false);
    }

    private void OnXRInteract(SelectEnterEventArgs args)
    {
        Interact();
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Enable or disable interaction on this painting.
    /// Called by MinigameManager.
    /// </summary>
    public void SetInteractable(bool value)
    {
        _interactable = value;

        // Swap material to give visual feedback
        if (paintingRenderer != null && highlightMaterial != null)
            paintingRenderer.material = value ? highlightMaterial : _originalMaterial;
    }

    /// <summary>
    /// Call this from your VR interaction system when the player
    /// presses the interact button while looking at / near this painting.
    /// </summary>
    public void Interact()
    {
        if (!_interactable)
        {
            Debug.Log($"[Painting: {name}] Not interactable yet.");
            return;
        }

        if (_isPlaying)
        {
            Debug.Log($"[Painting: {name}] Already playing, ignoring.");
            return;
        }

        // Play the clue audio regardless of order
        StartCoroutine(PlayClue());

        // Tell the manager this painting was interacted with
        bool correct = minigameManager.RegisterPaintingInteraction(this);

        if (correct)
            OnCorrectInteraction();
        else
            OnWrongInteraction();
    }

    // ──────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────

    private IEnumerator PlayClue()
    {
        if (clueAudio == null) yield break;

        _isPlaying = true;
        _audio.clip = clueAudio;
        _audio.Play();

        yield return new WaitForSeconds(clueAudio.length);
        _isPlaying = false;
    }

    /// <summary>Visual / haptic feedback for a correct interaction.</summary>
    private void OnCorrectInteraction()
    {
        Debug.Log($"[Painting: {name}] ✅ Correct order!");
        // TODO: Add haptic rumble, particle effect, or glow pulse here
    }

    /// <summary>Visual / haptic feedback for an incorrect interaction.</summary>
    private void OnWrongInteraction()
    {
        Debug.Log($"[Painting: {name}] ❌ Wrong order — sequence reset.");
        // TODO: Add a "wrong" sound effect, screen flash, or controller rumble here
    }

    private void OnDestroy()
    {
        if (_xrInteractable != null)
            _xrInteractable.selectEntered.RemoveListener(OnXRInteract);
    }
}
