using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Deliberate ONE-SHOT repositioning of a coupled tractor and trailer.
    /// The normal driving simulation never invokes this class. Both vehicle
    /// bodies are preflighted before touching either Rigidbody, so an unsafe
    /// reset leaves the entire assembly unchanged.
    /// </summary>
    public static class VehicleAssemblyRecovery
    {
        private const float MinRoadNormalY = 0.65f;
        private const float AdditionalClearanceMeters = 0.18f;

        public static VehicleRecoveryResult TryRecover(
            VehicleAssemblyRoot assembly, float maxSpeedMps = 1.75f)
        {
            if (assembly == null || !assembly.IsAttached ||
                assembly.Definition == null || assembly.Tractor == null ||
                assembly.Trailer == null)
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            Rigidbody towBody = assembly.Tractor.GetComponent<Rigidbody>();
            Rigidbody trailerBody = assembly.Trailer.GetComponent<Rigidbody>();
            VehicleSimulationRoot towPhysics =
                assembly.Tractor.GetComponent<VehicleSimulationRoot>();
            VehicleSimulationRoot trailerPhysics =
                assembly.Trailer.GetComponent<VehicleSimulationRoot>();
            if (towBody == null || trailerBody == null || towPhysics == null ||
                trailerPhysics == null || towBody.isKinematic || trailerBody.isKinematic ||
                VehicleAssemblyValidator.HasErrors(assembly.Definition))
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            float squaredMax = Mathf.Max(0f, maxSpeedMps) *
                               Mathf.Max(0f, maxSpeedMps);
            if (towBody.linearVelocity.sqrMagnitude > squaredMax ||
                trailerBody.linearVelocity.sqrMagnitude > squaredMax)
                return VehicleRecoveryResult.MovingTooFast;

            VehicleAssemblyDefinition config = assembly.Definition;
            Quaternion desiredYaw = VehicleManualRecovery.UprightRotation(
                towBody.transform.forward, towBody.transform.right);

            // Recovery straightens tractor and trailer; yaw articulation
            // is restored to zero only when the user explicitly presses R.
            Vector3 towBase = new Vector3(towBody.position.x, 0f, towBody.position.z);
            Vector3 trailerBase = VehicleAssemblyMath.CalculateTrailerRootPosition(
                towBase, desiredYaw, config.TractorHitchLocal,
                desiredYaw, config.TrailerHitchLocal);
            float probeStartY = Mathf.Max(towBody.position.y, trailerBody.position.y) + 35f;
            var hits = new RaycastHit[64];

            float towHeight, trailerHeight;
            VehicleRecoveryResult result = RequiredGroundHeight(
                assembly.Tractor.Definition, towPhysics, towBase, desiredYaw,
                probeStartY, assembly, hits, out towHeight);
            if (result != VehicleRecoveryResult.Success) return result;
            result = RequiredGroundHeight(
                assembly.Trailer.Definition, trailerPhysics, trailerBase, desiredYaw,
                probeStartY, assembly, hits, out trailerHeight);
            if (result != VehicleRecoveryResult.Success) return result;

            // Keep hitch anchors coincident and ensure BOTH sets of wheels
            // clear their ground sample with suspension initially extended.
            float tractorY = Mathf.Max(towHeight, trailerHeight - trailerBase.y);
            Vector3 finalTow = new Vector3(towBase.x, tractorY, towBase.z);
            Vector3 finalTrailer = VehicleAssemblyMath.CalculateTrailerRootPosition(
                finalTow, desiredYaw, config.TractorHitchLocal,
                desiredYaw, config.TrailerHitchLocal);

            if (IsBlocked(towPhysics, towBody, assembly, finalTow, desiredYaw) ||
                IsBlocked(trailerPhysics, trailerBody, assembly, finalTrailer, desiredYaw))
                return VehicleRecoveryResult.SpaceBlocked;

            towBody.position = finalTow;
            towBody.rotation = desiredYaw;
            trailerBody.position = finalTrailer;
            trailerBody.rotation = desiredYaw;
            towBody.linearVelocity = Vector3.zero;
            towBody.angularVelocity = Vector3.zero;
            trailerBody.linearVelocity = Vector3.zero;
            trailerBody.angularVelocity = Vector3.zero;
            towPhysics.ResetTransientMotionState();
            trailerPhysics.ResetTransientMotionState();
            towPhysics.SetInputs(0f, 0f, 0f, 0f);
            trailerPhysics.SetInputs(0f, 0f, 0f, 0f);
            Physics.SyncTransforms();
            towBody.WakeUp();
            trailerBody.WakeUp();
            return VehicleRecoveryResult.Success;
        }

        private static VehicleRecoveryResult RequiredGroundHeight(
            VehicleDefinition definition, VehicleSimulationRoot simulation,
            Vector3 basePosition, Quaternion rotation, float probeStartY,
            VehicleAssemblyRoot assembly, RaycastHit[] hits, out float height)
        {
            height = float.NegativeInfinity;
            if (definition == null || definition.Wheels == null)
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            foreach (WheelDefinition wheel in definition.Wheels)
            {
                if (wheel == null || wheel.RadiusMeters <= 0f ||
                    wheel.SuspensionTravelMeters <= 0f)
                    return VehicleRecoveryResult.MissingVehicleOrConfiguration;

                Vector3 worldWheel = basePosition + rotation * wheel.MountLocalPosition;
                Vector3 origin = new Vector3(worldWheel.x, probeStartY, worldWheel.z);
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits,
                    130f, simulation.GroundLayers.value, QueryTriggerInteraction.Ignore);

                bool found = false;
                RaycastHit nearest = default(RaycastHit);
                float minDistance = float.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    Collider collider = hits[i].collider;
                    if (collider == null ||
                        collider.transform.IsChildOf(assembly.Tractor.transform) ||
                        collider.transform.IsChildOf(assembly.Trailer.transform))
                        continue;
                    if (hits[i].distance < minDistance)
                    {
                        found = true;
                        minDistance = hits[i].distance;
                        nearest = hits[i];
                    }
                }
                if (!found) return VehicleRecoveryResult.GroundNotFound;
                if (nearest.normal.y < MinRoadNormalY)
                    return VehicleRecoveryResult.GroundTooSteep;

                float required = nearest.point.y -
                                 (rotation * wheel.MountLocalPosition).y +
                                 wheel.RadiusMeters + wheel.SuspensionTravelMeters +
                                 AdditionalClearanceMeters;
                height = Mathf.Max(height, required);
            }
            return VehicleRecoveryResult.Success;
        }

        private static bool IsBlocked(VehicleSimulationRoot simulation, Rigidbody body,
            VehicleAssemblyRoot assembly, Vector3 position, Quaternion rotation)
        {
            // For the current M1.3 primitive chassis box, this conservative
            // volume probe checks collisions other than our two bodies.
            // Compound imported colliders will require a richer future policy.
            BoxCollider cube = simulation.GetComponentInChildren<BoxCollider>();
            if (cube == null || cube.transform.parent != simulation.transform)
                return false;

            Vector3 scale = cube.transform.localScale;
            Vector3 half = Vector3.Scale(cube.size * 0.48f,
                new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            Vector3 localCenter = cube.transform.localPosition +
                cube.transform.localRotation * Vector3.Scale(cube.center, scale);
            Vector3 center = position + rotation * localCenter;
            Quaternion orient = rotation * cube.transform.localRotation;
            Collider[] buffer = new Collider[64];
            int count = Physics.OverlapBoxNonAlloc(center, half, buffer,
                orient, simulation.GroundLayers.value, QueryTriggerInteraction.Ignore);
            if (count >= buffer.Length) return true;
            for (int i = 0; i < count; i++)
            {
                Collider hit = buffer[i];
                if (hit != null &&
                    !hit.transform.IsChildOf(assembly.Tractor.transform) &&
                    !hit.transform.IsChildOf(assembly.Trailer.transform))
                    return true;
            }
            return false;
        }
    }
}
