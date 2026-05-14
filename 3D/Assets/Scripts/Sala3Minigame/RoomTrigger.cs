using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your StatueController here.")]
    public StatueController statue;

    private bool _hasTriggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        _hasTriggered = true;
        statue.OnPlayerEnterRoom();
    }
}
