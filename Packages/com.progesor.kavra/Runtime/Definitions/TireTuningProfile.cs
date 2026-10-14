using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Optional per-wheel multipliers for tuning the provisional M0 tire.
    /// Unity assets may be shared across any number of wheels/vehicles.
    /// All multipliers default to 1 and have no units. This does not encode
    /// calibrated Pacejka coefficients or rubber deformation.
    /// </summary>
    [CreateAssetMenu(fileName = "TireTuningProfile", menuName = "KAVRA/Tire Tuning Profile")]
    public sealed class TireTuningProfile : ScriptableObject
    {
        [Min(0f)] public float LongitudinalStiffnessMultiplier = 1f;
        [Min(0f)] public float LateralStiffnessMultiplier = 1f;
        [Min(0f)] public float LongitudinalGripMultiplier = 1f;
        [Min(0f)] public float LateralGripMultiplier = 1f;
        [Min(0f)] public float RollingResistanceMultiplier = 1f;
        [Min(0.01f)] public float WheelInertiaMultiplier = 1f;
    }
}
