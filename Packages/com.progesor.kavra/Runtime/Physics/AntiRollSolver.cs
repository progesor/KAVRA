using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Equal-and-opposite equivalent anti-roll load transfer for one pair.
    /// A negative wheel normal load would incorrectly pull it into the road;
    /// therefore the return value is bounded by available spring support.
    /// Only call this when BOTH wheels have valid road contacts.
    /// </summary>
    public static class AntiRollSolver
    {
        /// <summary>
        /// Positive result adds force to the left wheel, subtracts from right.
        /// Inputs are physical spring compression distances, not angles.
        /// </summary>
        public static float ComputeTransferN(
            float leftCompressionMeters, float rightCompressionMeters,
            float leftSupportN, float rightSupportN, float stiffnessNPerMeter)
        {
            float proposed = (leftCompressionMeters - rightCompressionMeters) *
                             Mathf.Max(0f, stiffnessNPerMeter);
            return Mathf.Clamp(proposed,
                -Mathf.Max(0f, leftSupportN),
                 Mathf.Max(0f, rightSupportN));
        }
    }
}
