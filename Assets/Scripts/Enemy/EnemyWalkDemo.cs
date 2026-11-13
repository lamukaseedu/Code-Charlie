/*
 * Author: Lam Nguyen
 * Created: 10/20/2026
 */

using UnityEngine;
using UnityEngine.Events;

// Attach to the enemy or one of its children for the demo scene.
public class EnemyWalkDemo : MonoBehaviour
{
    [Min(0.01f)] public float duration = 3f;

    [Header("Events")]
    [Tooltip("Invoked after each Duration interval, once the enemy returns to its starting position.")]
    [SerializeField] private UnityEvent onWalkCycleCompleted = new UnityEvent();

    private EnemyMovement movement;
    private Rigidbody rb;
    private Vector3 startPosition;
    private float timer;
    private float currentSpeed;

    private void Start()
    {
        movement = GetComponentInParent<EnemyMovement>();
        rb = movement.GetComponent<Rigidbody>();
        startPosition = rb.position;
        
        movement.enabled = false;
        rb.isKinematic = true;
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        bool cycleCompleted = timer >= duration;
        if (cycleCompleted)
        {
            rb.position = startPosition;
            timer = 0f;
            currentSpeed = 0f;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, movement.chaseSpeed,
                Mathf.Max(0f, movement.acceleration) * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + movement.transform.forward * currentSpeed * Time.fixedDeltaTime);
        }

        movement.UpdateWalkAnimation(Mathf.Abs(currentSpeed));

        if (cycleCompleted)
            onWalkCycleCompleted?.Invoke();
    }

    private void OnDisable()
    {
        if (movement != null)
            movement.UpdateWalkAnimation(0f);
    }
}
