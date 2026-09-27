using UnityEngine;

[System.Serializable]
public class DirectionalSprites
{
    public Sprite front;
    public Sprite frontRight;
    public Sprite right;
    public Sprite backRight;
    public Sprite back;
    public Sprite backLeft;
    public Sprite left;
    public Sprite frontLeft;
}

public class AngleEnemyTowardsPlayer : MonoBehaviour
{
    /*
     * Author: Andres Rondon-Villarmosa
     * Created: 9/1/2026
     */

    private Transform playerTarget;

    [Header("Visual Reference DEBUGGING")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private DirectionalSprites directionalSprites;

    [Header("Animation")]
    [SerializeField] private Animator enemyAnimation;

    [Header("Direction Debugging")]
    [SerializeField] private float angleToPlayer;
    [SerializeField] private int directionIndex;

    [Header("Target Position and Direction")]
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 directionToPlayer;

    // Finds the player, SpriteRenderer, and Animator.
    private void Awake()
    {
        enemyAnimation = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
        else
        {
            Debug.LogError(
                "Enemy could not find a GameObject with the Player tag.",
                this
            );
        }
    }

    private void Update()
    {
        if (playerTarget == null || enemyAnimation == null)
            return;

        targetPosition = new Vector3(
            playerTarget.position.x,
            transform.position.y,
            playerTarget.position.z
        );

        directionToPlayer = targetPosition - transform.position;

        // Compare where the player is with the direction the enemy is facing.
        angleToPlayer = Vector3.SignedAngle(
            directionToPlayer,
            transform.forward,
            Vector3.up
        );

        directionIndex = GetDirectionIndex(angleToPlayer);

        enemyAnimation.SetFloat("SpriteRot", directionIndex);
    }

    // Converts the angle into one of eight directions.
    private int GetDirectionIndex(float angle)
    {
        float normalizedAngle = (angle + 360f) % 360f;

        return Mathf.RoundToInt(normalizedAngle / 45f) % 8;
    }

    // Returns a sprite for debugging.
    private Sprite GetDirectionalSprite(int index)
    {
        switch (index)
        {
            case 0:
                return directionalSprites.front;

            case 1:
                return directionalSprites.frontRight;

            case 2:
                return directionalSprites.right;

            case 3:
                return directionalSprites.backRight;

            case 4:
                return directionalSprites.back;

            case 5:
                return directionalSprites.backLeft;

            case 6:
                return directionalSprites.left;

            case 7:
                return directionalSprites.frontLeft;

            default:
                return directionalSprites.front;
        }
    }

    // Draws the enemy's forward direction and direction toward the player.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward);

        if (playerTarget == null)
            return;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}