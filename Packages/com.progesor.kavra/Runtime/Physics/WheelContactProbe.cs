using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Allocation-free raycast baseline for M0. The caller owns the hit buffer.
    /// Future sphere/multi-sample strategies can replace this without changing
    /// the suspension and tire solvers.
    /// </summary>
    public static class WheelContactProbe
    {
        public static bool TryRaycast(
            Vector3 origin,
            Vector3 down,
            float maxDistance,
            int groundLayerMask,
            Transform ownVehicleRoot,
            Rigidbody ownBody,
            RaycastHit[] reusableHits,
            out RaycastHit nearest)
        {
            nearest = default(RaycastHit);
            if (reusableHits == null || reusableHits.Length == 0 || maxDistance <= 0f)
                return false;

            int count = Physics.RaycastNonAlloc(origin, down.normalized, reusableHits,
                maxDistance, groundLayerMask, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = reusableHits[i];
                Collider col = hit.collider;
                if (col == null || col.attachedRigidbody == ownBody)
                    continue;
                if (ownVehicleRoot != null && col.transform.IsChildOf(ownVehicleRoot))
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
