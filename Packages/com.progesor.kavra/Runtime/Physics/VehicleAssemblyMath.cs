using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Pose calculations do not query or modify the physics scene.</summary>
    public static class VehicleAssemblyMath
    {
        public static Vector3 HitchWorldPosition(
            Vector3 vehiclePosition, Quaternion vehicleRotation,
            Vector3 hitchLocal)
        {
            return vehiclePosition + vehicleRotation * hitchLocal;
        }

        /// <summary>
        /// Align two local hitch points with coincident world positions.
        /// By default both bodies share their initial heading; afterward
        /// the ConfigurableJoint permits yaw and limited pitch/roll.
        /// </summary>
        public static Vector3 CalculateTrailerRootPosition(
            Vector3 tractorPosition, Quaternion tractorRotation,
            Vector3 tractorHitchLocal, Quaternion trailerRotation,
            Vector3 trailerHitchLocal)
        {
            return HitchWorldPosition(tractorPosition, tractorRotation,
                       tractorHitchLocal) - trailerRotation * trailerHitchLocal;
        }

        public static float CalculateHitchErrorMeters(
            Vector3 tractorPosition, Quaternion tractorRotation,
            Vector3 tractorHitchLocal, Vector3 trailerPosition,
            Quaternion trailerRotation, Vector3 trailerHitchLocal)
        {
            return Vector3.Distance(
                HitchWorldPosition(tractorPosition, tractorRotation, tractorHitchLocal),
                HitchWorldPosition(trailerPosition, trailerRotation, trailerHitchLocal));
        }
    }
}
