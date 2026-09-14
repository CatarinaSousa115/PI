using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class CardSocketPuzzle : MonoBehaviour
{
    [Header("Sockets")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] sockets; // arrasta os 4 CartaSocket aqui

    [Header("Evento de Conclusão")]
    public UnityEvent onPuzzleCompleted;

    private bool puzzleCompleted = false;

    void OnEnable()
    {
        foreach (var socket in sockets)
            socket.selectEntered.AddListener(OnCardPlaced);
    }

    void OnDisable()
    {
        foreach (var socket in sockets)
            socket.selectEntered.RemoveListener(OnCardPlaced);
    }

    private void OnCardPlaced(SelectEnterEventArgs args)
    {
        if (puzzleCompleted) return;

        // Verifica se todos os sockets têm uma carta
        foreach (var socket in sockets)
        {
            if (!socket.hasSelection)
                return; // ainda falta pelo menos um
        }

        puzzleCompleted = true;
        Debug.Log("Card Puzzle concluído!");
        onPuzzleCompleted?.Invoke();
    }
}