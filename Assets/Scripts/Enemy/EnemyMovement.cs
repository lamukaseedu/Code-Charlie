using UnityEngine;

// EnemyMovement needs EnemyDetection to know WHERE and HOW to move.
[RequireComponent(typeof(EnemyDetection))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Speeds")]                          
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 3f;
    public float stopDistance = 0.2f;

    EnemyDetection detection; // Enemy movement asks "do we have somewhere to go?" "am i chasing or investigating?"

    void Awake()
    {
        detection = GetComponent<EnemyDetection>(); // This is where you place the enemy detection script in the inspector 
    }

    void Update()
    {
        if (detection == null) return; // If no script, dont do anything

        if (detection.HasWaypoint)
        {
            Vector3 target = detection.CurrentWaypoint; // "CurrentWaypoint"comes from enemy detection.
            target.y = transform.position.y;  // This marks where the sound/player is.

            float speed = detection.State == EnemyDetection.DetectionState.Chasing ? chaseSpeed : investigateSpeed; // Is the enemy investigating or chasing? if chasing, use chase speed, if investigating, use investigate speed.
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= stopDistance)
            {
                detection.ClearWaypoint(); // When you reach the player, important this part needs more work, currently no variable to assing when the enemy should atack the player, so it just stops when it reaches the player. 
            }
        }
    }
}
