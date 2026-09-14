using System.Collections;
using UnityEngine;

public class PaintingInteractable : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip clueAudio;

    [Header("References")]
    public MinigameManager minigameManager;

    [Header("Visual Feedback (optional)")]
    public Material highlightMaterial;
    public Renderer paintingRenderer;

    private bool _interactable = false;
    private bool _isPlaying = false;
    private Material _originalMaterial;
    private AudioSource _audio;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.spatialBlend = 1f;
        _audio.playOnAwake = false;

        if (paintingRenderer != null)
            _originalMaterial = paintingRenderer.material;

        SetInteractable(false);
    }

    public void SetInteractable(bool value)
    {
        _interactable = value;

        if (paintingRenderer != null && highlightMaterial != null)
            paintingRenderer.material = value ? highlightMaterial : _originalMaterial;
    }

    public void Interact()
    {
        if (!_interactable)
        {
            Debug.Log($"[Painting: {name}] Not interactable yet.");
            return;
        }


        if (_isPlaying) return;
        _isPlaying = true;

        StartCoroutine(PlayClue());

        bool correct = minigameManager.RegisterPaintingInteraction(this);

        if (correct)
            OnCorrectInteraction();
        else
            OnWrongInteraction();
    }

    private IEnumerator PlayClue()
    {
        if (clueAudio == null)
        {
            _isPlaying = false;
            yield break;
        }

        _audio.clip = clueAudio;
        _audio.Play();

        yield return new WaitForSeconds(clueAudio.length);
        _isPlaying = false; 
    }

    private void OnCorrectInteraction()
    {
        Debug.Log($"[Painting: {name}] Correct order!");
    }

    private void OnWrongInteraction()
    {
        Debug.Log($"[Painting: {name}] Wrong order — sequence reset.");
    }
}