using System;
using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    public enum NoiseLevel { Soft, Medium, Strong }
    [SerializeField] private bool logEmissions;
    public readonly struct NoiseEvent
    {
        public Vector3 Position { get; }
        public float Intensity { get; }
        public GameObject Source { get; }
        public NoiseLevel Level { get; }
        public NoiseEvent(Vector3 position, float intensity, GameObject source, NoiseLevel level)
        {
            Position = position;
            Intensity = Mathf.Max(0f, intensity);
            Source = source;
            Level = level;
        }
    }
    // New consumers use NoiseProduced. Retained for the unattached awareness draft only.
    public static event Action<NoiseEvent> NoiseProduced;
    public static event Action<Vector3, NoiseLevel> NoiseEmitted;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() { NoiseProduced = null; NoiseEmitted = null; }
    public static float GetIntensity(NoiseLevel level)
    {
        return level == NoiseLevel.Strong ? 45f : level == NoiseLevel.Medium ? 15f : 6f;
    }
    public void EmitSoft() { Emit(NoiseLevel.Soft); }
    public void EmitMedium() { Emit(NoiseLevel.Medium); }
    public void EmitStrong() { Emit(NoiseLevel.Strong); }
    public void Emit(NoiseLevel level) { EmitAt(transform.position, level, gameObject); }
    public static void EmitAt(Vector3 position, NoiseLevel level, GameObject source = null)
    {
        EmitAt(position, GetIntensity(level), source, level);
    }
    public static void EmitAt(Vector3 position, float intensity, GameObject source,
        NoiseLevel level = NoiseLevel.Medium)
    {
        if (float.IsNaN(intensity) || float.IsInfinity(intensity) || intensity <= 0f) return;
        NoiseEmitter sourceEmitter = source != null ? source.GetComponent<NoiseEmitter>() : null;
        if (sourceEmitter != null && sourceEmitter.logEmissions)
            Debug.Log($"Noise {level}: intensity {intensity}, position {position}, source {source.name}", source);
        NoiseProduced?.Invoke(new NoiseEvent(position, intensity, source, level));
        NoiseEmitted?.Invoke(position, level);
    }
}
