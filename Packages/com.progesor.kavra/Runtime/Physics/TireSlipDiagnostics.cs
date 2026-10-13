using UnityEngine;

namespace Progesor.Kavra
{
    public struct TireSlipMetrics
    {
        /// <summary>Normalized driven slip. Positive = overspin, negative = under-rotation.</summary>
        public float LongitudinalSlipRatio;
        /// <summary>Signed lateral contact velocity expressed as an angle in degrees.</summary>
        public float SlipAngleDegrees;
        public float LongitudinalSlipSpeedMps;
        public float LateralSpeedMps;
        public float ReferenceSpeedMps;
    }

    /// <summary>
    /// BOUNDED diagnostics around the existing velocity-slip tire model.
    /// Not a new tire force formula: it cannot imply Pacejka calibration.
    /// The regularized reference speed prevents division by zero at launch
    /// and parking, and allows meaningful plots without NaN/Infinity.
    /// </summary>
    public static class TireSlipDiagnostics
    {
        public static TireSlipMetrics Calculate(
            float longitudinalSpeedMps, float lateralSpeedMps,
            float wheelAngularSpeedRadPerSec, float radiusMeters,
            float regularizationSpeedMps = 1f)
        {
            float radius = Mathf.Max(0.01f, radiusMeters);
            float wheelSpeed = wheelAngularSpeedRadPerSec * radius;
            float difference = wheelSpeed - longitudinalSpeedMps;
            // Pairwise Mathf.Max avoids params-array allocations per wheel.
            float reference = Mathf.Max(
                Mathf.Max(0.1f, Mathf.Abs(regularizationSpeedMps)),
                Mathf.Max(Mathf.Abs(longitudinalSpeedMps), Mathf.Abs(wheelSpeed)));
            float ratio = Mathf.Clamp(difference / reference, -2f, 2f);

            float referenceForAngle = Mathf.Max(
                Mathf.Max(Mathf.Abs(longitudinalSpeedMps), 0.1f),
                Mathf.Abs(regularizationSpeedMps));
            float angle = Mathf.Atan2(lateralSpeedMps, referenceForAngle) *
                          Mathf.Rad2Deg;

            return new TireSlipMetrics
            {
                LongitudinalSlipRatio = ratio,
                SlipAngleDegrees = Mathf.Clamp(angle, -89f, 89f),
                LongitudinalSlipSpeedMps = difference,
                LateralSpeedMps = lateralSpeedMps,
                ReferenceSpeedMps = reference
            };
        }
    }
}
