using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Resolved physical coefficients for one wheel contact.</summary>
    public struct SurfaceTireParameters
    {
        public float LongitudinalFriction;
        public float LateralFriction;
        public float RollingResistanceCoefficient;
    }

    public static class SurfaceResponse
    {
        /// <summary>
        /// Apply explicit surface multipliers only, preserving backward
        /// compatibility when profile is null. Wetness and softness are
        /// metadata until a separate terrain/soil model is introduced.
        /// </summary>
        public static SurfaceTireParameters Evaluate(
            SurfaceProfile profile, float baselineLongitudinalFriction,
            float baselineLateralFriction, float baselineRollingResistance)
        {
            return new SurfaceTireParameters
            {
                LongitudinalFriction = Mathf.Max(0f, baselineLongitudinalFriction) *
                    (profile != null ? Mathf.Max(0f, profile.LongitudinalGripMultiplier) : 1f),
                LateralFriction = Mathf.Max(0f, baselineLateralFriction) *
                    (profile != null ? Mathf.Max(0f, profile.LateralGripMultiplier) : 1f),
                RollingResistanceCoefficient = Mathf.Max(0f, baselineRollingResistance) *
                    (profile != null ? Mathf.Max(0f, profile.RollingResistanceMultiplier) : 1f)
            };
        }

        public static SurfaceProfile Resolve(Collider hitCollider)
        {
            if (hitCollider == null) return null;
            SurfaceZone zone = hitCollider.GetComponentInParent<SurfaceZone>();
            return zone != null ? zone.Profile : null;
        }
    }
}
