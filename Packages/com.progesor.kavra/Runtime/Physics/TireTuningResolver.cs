using UnityEngine;

namespace Progesor.Kavra
{
    public struct TireTuningValues
    {
        public float LongitudinalStiffnessNsPerMeter;
        public float LateralStiffnessNsPerMeter;
        public float LongitudinalFriction;
        public float LateralFriction;
        public float RollingResistanceCoefficient;
        public float InertiaKgM2;
    }

    /// <summary>
    /// Combine wheel/tire and ground surface independently. Null tire profile
    /// preserves legacy global defaults; a given surface affects only wheels
    /// currently contacting it. No property is modified on the shared assets.
    /// </summary>
    public static class TireTuningResolver
    {
        public static TireTuningValues Resolve(
            TireTuningProfile tire, SurfaceTireParameters surface,
            float baseLongitudinalStiffness, float baseLateralStiffness,
            float baseWheelMassKg, float wheelRadiusMeters)
        {
            float radius = Mathf.Max(0.01f, wheelRadiusMeters);
            float inertia = 0.5f * Mathf.Max(0.1f, baseWheelMassKg) * radius * radius;

            if (tire == null)
            {
                return new TireTuningValues
                {
                    LongitudinalStiffnessNsPerMeter = Mathf.Max(0f, baseLongitudinalStiffness),
                    LateralStiffnessNsPerMeter = Mathf.Max(0f, baseLateralStiffness),
                    LongitudinalFriction = Mathf.Max(0f, surface.LongitudinalFriction),
                    LateralFriction = Mathf.Max(0f, surface.LateralFriction),
                    RollingResistanceCoefficient = Mathf.Max(0f, surface.RollingResistanceCoefficient),
                    InertiaKgM2 = inertia
                };
            }

            return new TireTuningValues
            {
                LongitudinalStiffnessNsPerMeter = Mathf.Max(0f, baseLongitudinalStiffness) *
                    Mathf.Max(0f, tire.LongitudinalStiffnessMultiplier),
                LateralStiffnessNsPerMeter = Mathf.Max(0f, baseLateralStiffness) *
                    Mathf.Max(0f, tire.LateralStiffnessMultiplier),
                LongitudinalFriction = Mathf.Max(0f, surface.LongitudinalFriction) *
                    Mathf.Max(0f, tire.LongitudinalGripMultiplier),
                LateralFriction = Mathf.Max(0f, surface.LateralFriction) *
                    Mathf.Max(0f, tire.LateralGripMultiplier),
                RollingResistanceCoefficient = Mathf.Max(0f, surface.RollingResistanceCoefficient) *
                    Mathf.Max(0f, tire.RollingResistanceMultiplier),
                InertiaKgM2 = inertia * Mathf.Max(0.01f, tire.WheelInertiaMultiplier)
            };
        }
    }
}
