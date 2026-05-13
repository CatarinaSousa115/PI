using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Collider))]
public class CardSocket : MonoBehaviour
{
    [Header("Identificação")]
    public string slotId;

    [Header("Snap")]
    [SerializeField] private float snapDistance = 0.2f;
    [SerializeField] private float snapSpeed = 10f;

    [Header("Feedback Háptico")]
    [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.5f;
    [SerializeField] private float hapticDuration = 0.12f;

    [Header("Visual")]
    [SerializeField] private GameObject slotIndicator;

    public bool IsOccupied { get; private set; }

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOccupied) return;
        var card = other.GetComponent<CardInteractable>();
        if (card == null || card.cardId != slotId) return;
        var grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;
        grab.selectExited.AddListener((args) => OnCardReleased(card, grab, args));
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsOccupied) return;
        var grab = other.GetComponent<XRGrabInteractable>();
        if (grab == null) return;
        grab.selectExited.RemoveAllListeners();
    }

    private void OnCardReleased(CardInteractable card, XRGrabInteractable grab, SelectExitEventArgs args)
    {
        if (IsOccupied) return;
        if (Vector3.Distance(card.transform.position, transform.position) > snapDistance) return;
        StartCoroutine(SmoothSnap(card, grab, args));
    }

    private IEnumerator SmoothSnap(CardInteractable card, XRGrabInteractable grab, SelectExitEventArgs args)
    {
        IsOccupied = true;
        grab.enabled = false;
        var rb = grab.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        float elapsed = 0f;
        float duration = 1f / snapSpeed;
        Vector3 startPos = card.transform.position;
        Quaternion startRot = card.transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            card.transform.position = Vector3.Lerp(startPos, transform.position, t);
            card.transform.rotation = Quaternion.Slerp(startRot, transform.rotation, t);
            yield return null;
        }

        card.transform.position = transform.position;
        card.transform.rotation = transform.rotation;
        card.transform.SetParent(transform);

        var interactor = args.interactorObject as XRBaseInputInteractor;
        interactor?.SendHapticImpulse(hapticAmplitude, hapticDuration);
        if (slotIndicator != null) slotIndicator.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsOccupied ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, snapDistance);
    }
}