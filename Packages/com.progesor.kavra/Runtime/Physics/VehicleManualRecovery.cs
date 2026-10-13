using UnityEngine;

namespace Progesor.Kavra
{
    public enum VehicleRecoveryResult
    {
        Success,
        MissingVehicleOrConfiguration,
        MovingTooFast,
        GroundNotFound,
        GroundTooSteep,
        SpaceBlocked
    }

    /// <summary>
    /// EXPLICIT, user-triggered recovery only. Never called by the physics
    /// solver, FixedUpdate or rollover detection. Intentionally teleports
    /// the vehicle, which is an out-of-simulation assistance feature.
    /// </summary>
    public static class VehicleManualRecovery
    {
        /// <summary>
        /// Restore upright yaw while retaining the vehicle's horizontal heading.
        /// Forward still works when the chassis has rolled 180 degrees.
        /// </summary>
        public static Quaternion UprightRotation(Vector3 forward, Vector3 right)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.Cross(
                    Vector3.ProjectOnPlane(right, Vector3.up), Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.forward;
            return Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        /// <summary>
        /// Attempts a safe manual recovery at the same horizontal coordinates.
        /// If any wheel has no usable ground, or the body is still moving fast,
        /// no position/rotation/velocity is changed. Never auto-recovers.
        /// </summary>
        public static VehicleRecoveryResult TryRecover(
            VehicleSimulationRoot simulation, float maximumSpeedMetersPerSecond = 1.75f,
            float wheelClearanceMeters = 0.18f)
        {
            if (simulation == null)
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            Rigidbody body = simulation.GetComponent<Rigidbody>();
            VehicleRoot root = simulation.GetComponent<VehicleRoot>();
            VehicleDefinition definition = root != null ? root.Definition : null;
            if (body == null || body.isKinematic || definition == null ||
                definition.Wheels == null || definition.Wheels.Count == 0)
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            if (body.linearVelocity.sqrMagnitude >
                Mathf.Max(0f, maximumSpeedMetersPerSecond) *
                Mathf.Max(0f, maximumSpeedMetersPerSecond))
                return VehicleRecoveryResult.MovingTooFast;

            Quaternion upright = UprightRotation(
                body.transform.forward, body.transform.right);

            // Reuse buffers across wheel casts, one recovery button press only.
            RaycastHit[] hits = new RaycastHit[32];
            float targetY = float.NegativeInfinity;
            Vector3 previousPosition = body.position;
            for (int i = 0; i < definition.Wheels.Count; i++)
            {
                WheelDefinition wheel = definition.Wheels[i];
                if (wheel == null || wheel.RadiusMeters <= 0f ||
                    wheel.SuspensionTravelMeters <= 0f)
                    return VehicleRecoveryResult.MissingVehicleOrConfiguration;

                Vector3 mountOffset = upright * wheel.MountLocalPosition;
                Vector3 rayOrigin = new Vector3(
                    previousPosition.x + mountOffset.x,
                    previousPosition.y + 30f,
                    previousPosition.z + mountOffset.z);

                RaycastHit ground;
                bool found = WheelContactProbe.TryRaycast(rayOrigin, Vector3.down, 110f,
                    simulation.GroundLayers.value, simulation.transform, body, hits, out ground);
                if (!found)
                    return VehicleRecoveryResult.GroundNotFound;
                if (ground.normal.y < 0.65f)
                    return VehicleRecoveryResult.GroundTooSteep;

                // Initially leave every wheel slightly above its maximum droop
                // position. Natural gravity/suspension settles the body afterward.
                float requiredY = ground.point.y - mountOffset.y +
                    wheel.RadiusMeters + wheel.SuspensionTravelMeters +
                    Mathf.Max(0.05f, wheelClearanceMeters);
                targetY = Mathf.Max(targetY, requiredY);
            }

            if (float.IsNegativeInfinity(targetY) || float.IsNaN(targetY))
                return VehicleRecoveryResult.GroundNotFound;

            Vector3 destination = new Vector3(
                previousPosition.x, targetY, previousPosition.z);
            if (IsBodyVolumeBlocked(simulation, body, destination, upright))
                return VehicleRecoveryResult.SpaceBlocked;

            body.position = destination;
            body.rotation = upright;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            simulation.ResetTransientMotionState();
            simulation.SetInputs(0f, 0f, 0f, 0f);
            body.WakeUp();
            Physics.SyncTransforms(); // Only on an explicitly requested teleport.
            return VehicleRecoveryResult.Success;
        }

        private static bool IsBodyVolumeBlocked(VehicleSimulationRoot simulation,
            Rigidbody body, Vector3 position, Quaternion upright)
        {
            // Works for the current M0 test cube; future chassis volumes will
            // provide their own recovery collision-clearance strategy.
            BoxCollider box = simulation.GetComponentInChildren<BoxCollider>();
            if (box == null) return false;

            Transform part = box.transform;
            // The M0 primitive box is an immediate root child with identity
            // scale on the root. These local-space values match its dimensions.
            if (part.parent != simulation.transform)
                return false;

            Vector3 scale = part.localScale;
            Vector3 half = Vector3.Scale(box.size * 0.5f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 localCenter = part.localPosition +
                part.localRotation * Vector3.Scale(box.center, scale);
            Vector3 worldCenter = position + upright * localCenter;
            Quaternion boxRotation = upright * part.localRotation;

            Collider[] overlaps = new Collider[64];
            int count = Physics.OverlapBoxNonAlloc(worldCenter, half * 0.95f,
                overlaps, boxRotation, simulation.GroundLayers.value,
                QueryTriggerInteraction.Ignore);
            // A filled result buffer cannot prove the destination is empty.
            if (count >= overlaps.Length)
                return true;

            for (int i = 0; i < count; i++)
            {
                Collider other = overlaps[i];
                if (other == null || other.attachedRigidbody == body ||
                    other.transform.IsChildOf(simulation.transform))
                    continue;
                return true;
            }
            return false;
        }
    }
}
