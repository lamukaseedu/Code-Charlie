using UnityEngine;

public readonly struct ImpactInfo
{
    public Vector3 Position { get; }
    public Vector3 Normal { get; }
    public Vector3 Direction { get; }
    public GameObject Source { get; }
    public Collider Collider { get; }
    public ImpactInfo(Vector3 position, Vector3 normal, Vector3 direction, GameObject source, Collider collider)
    {
        Position = position;
        Normal = normal;
        Direction = direction;
        Source = source;
        Collider = collider;
    }
}

// Objects own their reaction (for example glass breaking or impact noise).
public interface IImpactable
{
    void OnImpact(ImpactInfo impact);
}
