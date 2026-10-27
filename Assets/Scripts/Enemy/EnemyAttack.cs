/*
 * Author: Lam Nguyen
 * Created: 10/25/2026
 */

using UnityEngine;

[RequireComponent(typeof(EnemyMovement), typeof(Rigidbody), typeof(EnemyAwareness))]
[RequireComponent(typeof(AudioSource))]
public class EnemyAttack : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Animator that plays the enemy's attack. Finds an Animator on this object or its children when unassigned.")]
    [SerializeField] private Animator animator;
    [Tooltip("Animator Trigger parameter fired when an attack begins. Defaults to Attack.")]
    [SerializeField] private string attackTrigger = "Attack";
    [Tooltip("Name, full state path, or tag of the attack state on the Animator Layer.")]
    [SerializeField] private string attackState = "Attack";
    [Tooltip("Zero-based Animator layer containing the attack state. 0 is the base layer.")]
    [Min(0)]
    [SerializeField] private int animatorLayer = 0;
    [Tooltip("Cancel if the Animator has not entered the attack state within this many seconds.")]
    [Min(0.1f)]
    [SerializeField] private float animationStartTimeout = 2f;

    [Header("Hit Window")]
    [Tooltip("Dedicated child attack collider. Do not assign the enemy's body collider.")]
    [SerializeField] private Collider attackCollider;
    [Tooltip("Fraction of the attack animation at which the hit collider activates and the attack sound and optional lunge occur. 0.3 means 30%.")]
    [Range(0f, 1f)]
    [SerializeField] private float colliderEnableThreshold = 0.3f;
    [Tooltip("Fraction of the attack animation at which the hit collider deactivates. Must be after the enable threshold. 0.8 means 80%.")]
    [Range(0f, 1f)]
    [SerializeField] private float colliderDisableThreshold = 0.8f;

    [Header("Movement")]
    [Tooltip("Stop horizontal motion when the attack starts and pause navigation and turning until it ends. The optional lunge can still move the enemy.")]
    [SerializeField] private bool stopMovingDuringAttack = true;
    [Tooltip("Apply one forward Rigidbody impulse when the attack collider activates. Navigation pauses for the rest of the attack so it does not cancel the impulse.")]
    [SerializeField] private bool lunge = false;
    [Tooltip("Forward impulse strength applied when Lunge is enabled. Higher values push harder; heavier Rigidbody masses move less.")]
    [Min(0f)]
    [SerializeField] private float lungeImpulse = 5f;

    [Header("Automatic Attacks")]
    [Tooltip("Automatically attack when awareness is Active and the visible player is inside Attack Range and Attack Angle. Disable to start attacks only through Attack().")]
    [SerializeField] private bool attackAutomatically = true;
    [Tooltip("Maximum distance in world units to the player's confirmed position for starting an automatic attack.")]
    [Min(0f)]
    [SerializeField] private float attackRange = 2f;
    [Tooltip("Full horizontal angle in front of the enemy within which it can start an automatic attack.")]
    [Range(0f, 360f)]
    [SerializeField] private float attackAngle = 90f;
    [Tooltip("Seconds after an attack ends before another can start.")]
    [Min(0f)]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Attack Sound")]
    [Tooltip("AudioSource used for attack sounds. Uses the AudioSource on this enemy when unassigned. Configure its spatial blend and volume for positional audio.")]
    [SerializeField] private AudioSource attackAudioSource;
    [Tooltip("Add any number of attack clips. One random clip plays once when the attack collider activates, rather than when the animation starts. Leave empty for no attack sound.")]
    [SerializeField] private AudioClip[] attackSounds;

    public bool IsAttacking { get; private set; }
    // Leave the impulse to physics instead of letting the navigation motor brake it away.
    public bool ShouldPauseMovement => IsAttacking && (stopMovingDuringAttack || hasLunged);

    private Rigidbody rb;
    private EnemyMovement movement;
    private EnemyAwareness awareness;
    private Health health;
    private bool animationStarted;
    private bool hitWindowStarted;
    private bool hitWindowEnded;
    private bool hasLunged;
    private float startDeadline;
    private float nextAttackTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<EnemyMovement>();
        awareness = GetComponent<EnemyAwareness>();
        health = GetComponent<Health>();
        if (attackAudioSource == null)
            attackAudioSource = GetComponent<AudioSource>();
        if (attackAudioSource != null)
            attackAudioSource.playOnAwake = false;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (!HasValidCollider())
        {
            Debug.LogWarning("EnemyAttack needs a dedicated child attack collider assigned in the Inspector.", this);
            enabled = false;
            return;
        }

        attackCollider.enabled = false;
    }

    private void OnValidate()
    {
        colliderEnableThreshold = Mathf.Clamp(colliderEnableThreshold, 0f, 0.99f);
        colliderDisableThreshold = Mathf.Clamp(colliderDisableThreshold, colliderEnableThreshold + 0.01f, 1f);
    }

    private bool HasValidCollider()
    {
        return attackCollider != null && attackCollider.transform != transform &&
            attackCollider.transform.IsChildOf(transform);
    }

    private void Update()
    {
        if (IsAttacking || !attackAutomatically || awareness == null || !awareness.isActiveAndEnabled ||
            awareness.State != EnemyAwareness.AwarenessState.Active || !awareness.HasLineOfSightToPlayer)
            return;

        Vector3 offset = awareness.CurrentWaypoint - transform.position;
        if (offset.sqrMagnitude > attackRange * attackRange)
            return;

        offset.y = 0f;
        if (offset.sqrMagnitude > 0.000001f && Vector3.Angle(transform.forward, offset) > attackAngle * 0.5f)
            return;

        Attack();
    }

    // Can also be called from a Unity Event or another enemy behaviour.
    public void Attack()
    {
        if (!isActiveAndEnabled || IsAttacking || Time.timeScale == 0f || Time.time < nextAttackTime ||
            movement == null || !movement.isActiveAndEnabled || (health != null && health.IsDead) ||
            !HasValidCollider() || animator == null || !animator.isActiveAndEnabled ||
            animatorLayer < 0 || animatorLayer >= animator.layerCount)
            return;

        // Wait until the previous attack state has exited before triggering again.
        if (IsAttackState(animator.GetCurrentAnimatorStateInfo(animatorLayer)) ||
            (animator.IsInTransition(animatorLayer) && IsAttackState(animator.GetNextAnimatorStateInfo(animatorLayer))))
            return;

        IsAttacking = true;
        animationStarted = hitWindowStarted = hitWindowEnded = hasLunged = false;
        attackCollider.enabled = false;
        startDeadline = Time.time + animationStartTimeout;

        if (stopMovingDuringAttack && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        animator.speed = 1f;
        animator.SetTrigger(attackTrigger);
    }

    private bool IsAttackState(AnimatorStateInfo state)
    {
        return state.IsName(attackState) || state.IsTag(attackState);
    }

    private void LateUpdate()
    {
        if (!IsAttacking)
            return;

        if (animator == null || !animator.isActiveAndEnabled || movement == null ||
            !movement.isActiveAndEnabled || !HasValidCollider() || (health != null && health.IsDead))
        {
            CancelAttack();
            return;
        }

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        bool inAttack = IsAttackState(state);
        if (animator.IsInTransition(animatorLayer))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(animatorLayer);
            // Close immediately when the attack is interrupted or blends out.
            if (inAttack && !IsAttackState(next))
            {
                CancelAttack();
                return;
            }

            if (IsAttackState(next))
            {
                state = next;
                inAttack = true;
            }
        }

        if (!inAttack)
        {
            if (animationStarted || Time.time >= startDeadline)
                CancelAttack();
            return;
        }

        animationStarted = true;
        float progress = state.normalizedTime;
        if (progress >= colliderDisableThreshold)
        {
            attackCollider.enabled = false;
            hitWindowEnded = true;
        }
        else if (progress >= colliderEnableThreshold && !hitWindowStarted && !hitWindowEnded)
        {
            hitWindowStarted = true;
            attackCollider.enabled = true;
            GameAudio.PlayRandomOneShot(attackAudioSource, attackSounds);
            if (lunge && !rb.isKinematic)
            {
                hasLunged = true;
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                rb.AddForce(forward * lungeImpulse, ForceMode.Impulse);
            }
        }

        if (progress >= 1f)
            CancelAttack();
    }

    public void CancelAttack()
    {
        if (HasValidCollider())
            attackCollider.enabled = false;

        if (!IsAttacking)
            return;

        IsAttacking = false;
        hasLunged = false;
        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
            animator.ResetTrigger(attackTrigger);
    }

    private void OnDisable()
    {
        CancelAttack();
    }
}
