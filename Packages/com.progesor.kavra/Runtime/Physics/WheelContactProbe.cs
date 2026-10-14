using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Raycast baseline or experimental spherical contact sweep. Sphere mode
    /// uses a ray fallback because Unity SphereCast ignores initial overlaps.
    /// Neither mode models the full tire patch or deformable soil.
    /// </summary>
    public enum WheelContactMode
    {
        Raycast = 0,
        SphereCastWithRayFallback = 1
    }

    public struct WheelContactSample
    {
        public RaycastHit Hit;
        /// <summary>
        /// Equivalent distance from the *mount* to the surface, along the
        /// suspension axis. Can be negative if the mount passes below ground.
        /// </summary>
        public float RayEquivalentDistanceMeters;
        public bool IsSphereContact;
        public bool UsedRayFallback;
    }

    public static class WheelContactProbe
    {
        /// <summary>
        /// Probe starts above the mount to stay outside the road even if the
        /// suspension fully bottoms. Returned distance remains mount-relative.
        /// The caller provides a reusable buffer to avoid frame allocations.
        /// </summary>
        public static bool TrySample(
            Vector3 mountWorldPosition, Vector3 suspensionUp,
            float wheelRadius, float suspensionTravel, WheelContactMode mode,
            int groundLayerMask, Transform ownVehicleRoot, Rigidbody ownBody,
            RaycastHit[] reusableHits, out WheelContactSample sample)
        {
            sample = default(WheelContactSample);
            if (reusableHits == null || reusableHits.Length == 0 ||
                wheelRadius <= 0f || suspensionTravel < 0f ||
                suspensionUp.sqrMagnitude < 0.000001f)
                return false;

            Vector3 up = suspensionUp.normalized;
            // Ensures a deeply compressed sensor origin is still usually above
            // the road; the ray can see past the wheel-mount plane.
            float originOffset = wheelRadius + 0.08f;
            Vector3 castOrigin = mountWorldPosition + up * originOffset;
            Vector3 down = -up;
            float maxRayDistance = originOffset + wheelRadius + suspensionTravel + 0.03f;

            RaycastHit found;
            if (mode == WheelContactMode.SphereCastWithRayFallback)
            {
                int count = Physics.SphereCastNonAlloc(castOrigin, wheelRadius, down,
                    reusableHits, originOffset + suspensionTravel + 0.03f,
                    groundLayerMask, QueryTriggerInteraction.Ignore);
                if (FindClosestValid(reusableHits, count, ownVehicleRoot, ownBody, out found))
                {
                    sample.Hit = found;
                    // Swept sphere distance is center travel, NOT a distance
                    // to the road plane. Convert to equivalent ray length.
                    sample.RayEquivalentDistanceMeters =
                        found.distance - originOffset + wheelRadius;
                    sample.IsSphereContact = true;
                    return true;
                }
            }

            int rays = Physics.RaycastNonAlloc(castOrigin, down, reusableHits,
                maxRayDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
            if (!FindClosestValid(reusableHits, rays, ownVehicleRoot, ownBody, out found))
                return false;

            sample.Hit = found;
            sample.RayEquivalentDistanceMeters = found.distance - originOffset;
            sample.UsedRayFallback = mode != WheelContactMode.Raycast;
            return true;
        }

        /// <summary>
        /// Preserved M0 API for legacy tests/consumers. Performs a conventional
        /// ray from its caller-provided origin, without shifting the sensor.
        /// </summary>
        public static bool TryRaycast(
            Vector3 origin, Vector3 down, float maxDistance, int groundLayerMask,
            Transform ownVehicleRoot, Rigidbody ownBody,
            RaycastHit[] reusableHits, out RaycastHit nearest)
        {
            nearest = default(RaycastHit);
            if (reusableHits == null || reusableHits.Length == 0 || maxDistance <= 0f ||
                down.sqrMagnitude < 0.000001f)
                return false;

            int count = Physics.RaycastNonAlloc(origin, down.normalized, reusableHits,
                maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
            return FindClosestValid(reusableHits, count, ownVehicleRoot, ownBody,
                out nearest);
        }

        private static bool FindClosestValid(RaycastHit[] hits, int count,
            Transform ownVehicleRoot, Rigidbody ownBody, out RaycastHit nearest)
        {
            nearest = default(RaycastHit);
            float closest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = hits[i];
                Collider collider = hit.collider;
                if (collider == null || collider.attachedRigidbody == ownBody ||
                    (ownVehicleRoot != null && collider.transform.IsChildOf(ownVehicleRoot)))
                    continue;

                if (hit.distance < closest)
                {
                    nearest = hit;
                    closest = hit.distance;
                    found = true;
                }
            }
            return found;
        }
    }
}
