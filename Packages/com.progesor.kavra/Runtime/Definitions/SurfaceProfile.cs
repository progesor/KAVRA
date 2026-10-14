using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Contact surface multipliers applied PER WHEEL to tire grip and rolling
    /// resistance. Softness/Wetness remain descriptive values for future soil
    /// response and are NOT automatically double-counted as extra friction.
    /// </summary>
    [CreateAssetMenu(fileName = "SurfaceProfile", menuName = "KAVRA/Surface Profile")]
    public sealed class SurfaceProfile : ScriptableObject
    {
        [Min(0f)] public float LongitudinalGripMultiplier = 1f;
        [Min(0f)] public float LateralGripMultiplier = 1f;
        [Range(0f, 1f)] public float Softness = 0f;
        [Range(0f, 1f)] public float Wetness = 0f;
        [Min(0f)] public float RollingResistanceMultiplier = 1f;
    }
}
