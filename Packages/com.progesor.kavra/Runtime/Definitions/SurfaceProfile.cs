using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Configuration for future terrain/contact modules.
    /// These values are descriptive placeholders, not yet wired into a solver.
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
