using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>One spring/damper sample. SI units; forces are newtons.</summary>
    public struct SuspensionSample
    {
        public float CompressionMeters;
        public float RawCompressionMeters;
        public float CompressionVelocityMps;
        public float SpringForceN;
        public float DamperForceN;
        public float BumpStopForceN;
        public float BottomOutMeters;
        public float TotalForceN;
    }

    /// <summary>
    /// Equivalent one-dimensional spring/damper. Includes a progressive
    /// end-stop to prevent a flat raycast spring from losing its ability to
    /// resist further chassis penetration after its stroke is exhausted.
    /// This is a compliant stop, not a rigid collision constraint.
    /// </summary>
    public static class SuspensionSolver
    {
        public static SuspensionSample Evaluate(
            bool grounded, float radiusMeters, float travelMeters,
            float rayDistanceMeters, float springRateNPerMeter,
            float compressionDampingNsPerMeter, float reboundDampingNsPerMeter,
            float previousCompressionMeters, bool wasGrounded, float deltaTime)
        {
            return Evaluate(grounded, radiusMeters, travelMeters, rayDistanceMeters,
                springRateNPerMeter, compressionDampingNsPerMeter,
                reboundDampingNsPerMeter, previousCompressionMeters,
                wasGrounded, deltaTime, 0.85f, 4f);
        }

        public static SuspensionSample Evaluate(
            bool grounded, float radiusMeters, float travelMeters,
            float rayDistanceMeters, float springRateNPerMeter,
            float compressionDampingNsPerMeter, float reboundDampingNsPerMeter,
            float previousRawCompressionMeters, bool wasGrounded, float deltaTime,
            float bumpStopStartFraction, float bumpStopRateMultiplier)
        {
            if (!grounded || travelMeters <= 0f || deltaTime <= 0f)
                return default(SuspensionSample);

            float raw = radiusMeters + travelMeters - rayDistanceMeters;
            float compression = Mathf.Clamp(raw, 0f, travelMeters);
            float velocity = wasGrounded
                ? Mathf.Clamp((raw - previousRawCompressionMeters) / deltaTime, -5f, 5f)
                : 0f;

            float rate = Mathf.Max(0f, springRateNPerMeter);
            float spring = rate * compression;
            float damping = velocity * (velocity >= 0f
                ? Mathf.Max(0f, compressionDampingNsPerMeter)
                : Mathf.Max(0f, reboundDampingNsPerMeter));

            float softZoneStart = travelMeters * Mathf.Clamp01(bumpStopStartFraction);
            float stopTravel = Mathf.Max(0f, compression - softZoneStart);
            float penetration = Mathf.Max(0f, raw - travelMeters);
            float stopRate = rate * Mathf.Max(0f, bumpStopRateMultiplier);
            // Continuous, progressive spring: firming in the last stroke,
            // and a steeper response after full bottom-out.
            float bump = stopRate * (stopTravel + 3f * penetration);

            return new SuspensionSample
            {
                CompressionMeters = compression,
                RawCompressionMeters = raw,
                CompressionVelocityMps = velocity,
                SpringForceN = spring,
                DamperForceN = damping,
                BumpStopForceN = bump,
                BottomOutMeters = penetration,
                TotalForceN = Mathf.Max(0f, spring + damping + bump)
            };
        }
    }
}
