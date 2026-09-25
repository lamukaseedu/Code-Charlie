using UnityEngine;

public class EnemyKnockback : MonoBehaviour, IKnockable
{
    [SerializeField] float thrust = 12f;
    private Rigidbody rb; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Execute(Transform ExecutionSource)
    {
        KnockbackObject(ExecutionSource);
    }

    public void KnockbackObject(Transform ExecutionSource)
    {
        Vector3 dir = ((transform.position - ExecutionSource.transform.position).normalized);
        rb.AddForce(dir * thrust, ForceMode.Impulse);
    }
}
