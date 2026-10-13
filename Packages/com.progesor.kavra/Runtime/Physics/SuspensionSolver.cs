using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>One vertical spring/damper sample. All forces are in newtons.</summary>
    public struct SuspensionSample
    {
        public float CompressionMeters;
        public float CompressionVelocityMps;
        public float SpringForceN;
        public float DamperForceN;
        public float TotalForceN;
    }

    /// <summary>
    /// Equivalent spring/damper for M0. Suspension axis and road-contact
    /// geometry are handled by VehicleSimulationRoot, not this pure solver.
    /// </summary>
    public static class SuspensionSolver
    {
        public static SuspensionSample Evaluate(
            bool grounded,
            float radiusMeters,
            float travelMeters,
            float rayDistanceMeters,
            float springRateNPerMeter,
            float compressionDampingNsPerMeter,
            float reboundDampingNsPerMeter,
            float previousCompressionMeters,
            bool wasGrounded,
            float deltaTime)
        {
            if (!grounded || travelMeters <= 0f || deltaTime <= 0f)
                return default(SuspensionSample);

            // The mount is the wheel-center position at full compression;
            // the free wheel center is travelMeters below the mount.
            float compression = Mathf.Clamp(
                radiusMeters + travelMeters - rayDistanceMeters, 0f, travelMeters);

            // On the first contact frame there is no valid previous sample.
            // Clamp measurement spikes when encountering sharp geometry.
            float compressionRate = wasGrounded
                ? Mathf.Clamp((compression - previousCompressionMeters) / deltaTime, -5f, 5f)
                : 0f;

            float spring = Mathf.Max(0f, springRateNPerMeter) * compression;
            float dampingCoefficient = compressionRate >= 0f
                ? Mathf.Max(0f, compressionDampingNsPerMeter)
                : Mathf.Max(0f, reboundDampingNsPerMeter);
            float damper = dampingCoefficient * compressionRate;

            // A raycast spring can push but cannot pull the road toward it.
            return new SuspensionSample
            {
                CompressionMeters = compression,
                CompressionVelocityMps = compressionRate,
                SpringForceN = spring,
                DamperForceN = damper,
                TotalForceN = Mathf.Max(0f, spring + damper)
            };
        }
    }
}
