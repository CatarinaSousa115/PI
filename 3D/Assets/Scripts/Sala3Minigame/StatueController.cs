using System.Collections;
using UnityEngine;

/// <summary>
/// Controls the statue's speech and interaction states.
/// Attach to the statue GameObject.
/// </summary>
public class StatueController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector fields
    // ──────────────────────────────────────────────

    [Header("Audio Clips")]
    [Tooltip("Clip played automatically when the player enters the room.")]
    public AudioClip entranceDialogue;

    [Tooltip("Clip played when the player first interacts with the statue (starts the minigame).")]
    public AudioClip minigameIntroDialogue;

    [Tooltip("Clip played when the player returns after solving the paintings.")]
    public AudioClip completionDialogue;

    [Header("References")]
    public MinigameManager minigameManager;

    [Header("Interaction")]
    [Tooltip("How close (metres) the player must be to interact.")]
    public float interactionRange = 2.5f;

    [Tooltip("Layer mask for the player.")]
    public LayerMask playerLayer;

    // ──────────────────────────────────────────────
    // Internal state
    // ──────────────────────────────────────────────

    public enum StatueState
    {
        Idle,               // Before player enters
        WaitingForFirstInteraction,
        MinigameActive,
        WaitingForFinalInteraction,
        Complete
    }

    public StatueState CurrentState { get; private set; } = StatueState.Idle;

    private AudioSource _audio;
    private bool _isPlaying = false;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.spatialBlend = 1f; 
        _audio.playOnAwake = false;
    }

    public void OnPlayerEnterRoom()
    {
        if (CurrentState != StatueState.Idle) return;

        CurrentState = StatueState.WaitingForFirstInteraction;
        PlayClip(entranceDialogue);

        Debug.Log("[Statue] Entrance dialogue started. Waiting for first interaction.");
    }

    public void OnPaintingsSolved()
    {
        if (CurrentState != StatueState.MinigameActive) return;

        CurrentState = StatueState.WaitingForFinalInteraction;
        Debug.Log("[Statue] All paintings solved! Waiting for final interaction.");
    }


    public void Interact()
    {
        Debug.Log("[Statue] Interact() called. Current state: " + CurrentState);
        
        if (_isPlaying) return;

        switch (CurrentState)
        {
            case StatueState.WaitingForFirstInteraction:
                StartCoroutine(StartMinigameSequence());
                break;

            case StatueState.WaitingForFinalInteraction:
                StartCoroutine(CompleteMinigameSequence());
                break;

            default:
                Debug.Log("[Statue] Interaction attempted in state: " + CurrentState + " — ignored.");
                break;
        }
    }

    private IEnumerator StartMinigameSequence()
    {
        CurrentState = StatueState.MinigameActive;
        yield return PlayClipAndWait(minigameIntroDialogue);

        minigameManager.BeginMinigame();
        Debug.Log("[Statue] Minigame intro done — minigame started.");
    }

    private IEnumerator CompleteMinigameSequence()
    {
        CurrentState = StatueState.Complete;
        yield return PlayClipAndWait(completionDialogue);

        minigameManager.CompleteMinigame();
        Debug.Log("[Statue] Minigame complete!");
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;
        _audio.clip = clip;
        _audio.Play();
    }

    private IEnumerator PlayClipAndWait(AudioClip clip)
    {
        if (clip == null) yield break;

        _isPlaying = true;
        _audio.clip = clip;
        _audio.Play();

        yield return new WaitForSeconds(clip.length);
        _isPlaying = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
