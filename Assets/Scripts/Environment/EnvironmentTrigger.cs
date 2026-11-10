using UnityEngine;
using UnityEngine.Events;

public class EnvironmentTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("Only colliders whose GameObject has this tag activate the events.")]
    [SerializeField] private string targetTag = "Player";

    [Header("Events")]
    [Tooltip("Invoked when a collider with the matching tag enters the trigger.")]
    [SerializeField] private UnityEvent onTriggerEntered = new UnityEvent();
    [Tooltip("Invoked when a collider with the matching tag exits the trigger.")]
    [SerializeField] private UnityEvent onTriggerExited = new UnityEvent();

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (MatchesTag(other))
            onTriggerEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (MatchesTag(other))
            onTriggerExited?.Invoke();
    }

    private bool MatchesTag(Collider other)
    {
        return isActiveAndEnabled && !string.IsNullOrEmpty(targetTag) &&
            other.CompareTag(targetTag);
    }
}
