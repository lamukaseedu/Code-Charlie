using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EventTimer : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/13/2026
     */
    [Header("Timer")]
    [Min(0f)]
    [SerializeField] private float delay = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTimerStarted;
    [SerializeField] private UnityEvent onTimerFinished;

    private Coroutine timerCoroutine;

    // This starts the couroutine for a timer between events.
    public void StartTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    //This immediately stops the timer.
    public void CancelTimer()
    {
        if (timerCoroutine == null)
            return;

        StopCoroutine(timerCoroutine);
        timerCoroutine = null;
    }

    //This is the coroutine for invoking events before and after the timer.
    private IEnumerator TimerRoutine()
    {
        onTimerStarted?.Invoke();

        yield return new WaitForSeconds(delay);

        timerCoroutine = null;
        onTimerFinished?.Invoke();
    }
}
