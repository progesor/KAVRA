using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// R-only safe recovery of the connected LEAD PREFIX (tractor and each
    /// still-attached downstream vehicle). Detached tail segments remain
    /// untouched. Every candidate is preflighted BEFORE moving any body.
    /// Never called from FixedUpdate, collision or rollover detection.
    /// </summary>
    public static class VehicleAssemblyChainRecovery
    {
        private const float MinRoadNormalY = 0.65f;
        private const float WheelClearanceMeters = 0.18f;

        public static VehicleRecoveryResult TryRecover(
            VehicleAssemblyChainRoot chain, float maxSpeedMetersPerSecond = 1.75f)
        {
            if (chain == null || chain.Definition == null ||
                chain.Vehicles == null || chain.Couplings == null ||
                VehicleAssemblyChainValidator.HasErrors(chain.Definition))
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            int count = chain.ConnectedLeadVehicleCount();
            if (count < 1 || count > chain.Vehicles.Length ||
                count > chain.Definition.VehicleCount)
                return VehicleRecoveryResult.MissingVehicleOrConfiguration;

            var bodies = new Rigidbody[count];
            var simulators = new VehicleSimulationRoot[count];
            var targetPositions = new Vector3[count];

            float maxSpeedSquared = Mathf.Max(0f, maxSpeedMetersPerSecond);
            maxSpeedSquared *= maxSpeedSquared;
            float probeY = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                VehicleRoot vehicle = chain.Vehicles[i];
                if (vehicle == null || vehicle.Definition == null ||
                    vehicle.Definition.Wheels == null ||
                    vehicle.Definition.Wheels.Count == 0)
                    return VehicleRecoveryResult.MissingVehicleOrConfiguration;

                bodies[i] = vehicle.GetComponent<Rigidbody>();
                simulators[i] = vehicle.GetComponent<VehicleSimulationRoot>();
                if (bodies[i] == null || simulators[i] == null ||
                    bodies[i].isKinematic)
                    return VehicleRecoveryResult.MissingVehicleOrConfiguration;

                if (bodies[i].linearVelocity.sqrMagnitude > maxSpeedSquared)
                    return VehicleRecoveryResult.MovingTooFast;
                probeY = Mathf.Max(probeY, bodies[i].position.y);
            }
            probeY += 35f;

            Rigidbody lead = bodies[0];
            Quaternion upright = VehicleManualRecovery.UprightRotation(
                lead.transform.forward, lead.transform.right);
            targetPositions[0] = new Vector3(lead.position.x, 0f, lead.position.z);
            for (int i = 1; i < count; i++)
            {
                VehicleAssemblyDefinition link = chain.Definition.Links[i - 1];
                targetPositions[i] = VehicleAssemblyMath.CalculateTrailerRootPosition(
                    targetPositions[i - 1], upright, link.TractorHitchLocal,
                    upright, link.TrailerHitchLocal);
            }

            var groundHits = new RaycastHit[64];
            float requiredVerticalLift = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                VehicleRecoveryResult outcome = ProbeWheels(
                    chain.Vehicles[i].Definition, simulators[i], targetPositions[i],
                    upright, probeY, chain.transform, groundHits,
                    out float minimumBodyWorldY);
                if (outcome != VehicleRecoveryResult.Success) return outcome;
                requiredVerticalLift = Mathf.Max(requiredVerticalLift,
                    minimumBodyWorldY - targetPositions[i].y);
            }

            for (int i = 0; i < count; i++)
                targetPositions[i] += Vector3.up * requiredVerticalLift;

            var nearby = new Collider[64];
            for (int i = 0; i < count; i++)
                if (IsVolumeBlocked(simulators[i], targetPositions[i],
                    upright, chain.Vehicles, count, nearby))
                    return VehicleRecoveryResult.SpaceBlocked;

            // All checks passed. Relative hitch transform is EXACTLY
            // preserved by the shared yaw and vertical lift.
            for (int i = 0; i < count; i++)
            {
                bodies[i].position = targetPositions[i];
                bodies[i].rotation = upright;
                bodies[i].linearVelocity = Vector3.zero;
                bodies[i].angularVelocity = Vector3.zero;
                simulators[i].ResetTransientMotionState();
                simulators[i].SetInputs(0f, 0f, 0f, 0f);
            }
            Physics.SyncTransforms();
            for (int i = 0; i < count; i++) bodies[i].WakeUp();
            return VehicleRecoveryResult.Success;
        }

        private static VehicleRecoveryResult ProbeWheels(
            VehicleDefinition vehicle, VehicleSimulationRoot simulation,
            Vector3 basePosition, Quaternion upright, float rayStartY,
            Transform chainRoot, RaycastHit[] buffer, out float minimumRootY)
        {
            minimumRootY = float.NegativeInfinity;
            foreach (WheelDefinition wheel in vehicle.Wheels)
            {
                if (wheel == null || wheel.RadiusMeters <= 0f ||
                    wheel.SuspensionTravelMeters <= 0f)
                    return VehicleRecoveryResult.MissingVehicleOrConfiguration;

                Vector3 offset = upright * wheel.MountLocalPosition;
                Vector3 point = basePosition + offset;
                var rayOrigin = new Vector3(point.x, rayStartY, point.z);
                int found = Physics.RaycastNonAlloc(rayOrigin, Vector3.down,
                    buffer, 130f, simulation.GroundLayers.value,
                    QueryTriggerInteraction.Ignore);
                if (found >= buffer.Length)
                    return VehicleRecoveryResult.GroundNotFound;

                float closest = float.PositiveInfinity;
                RaycastHit chosen = default(RaycastHit);
                for (int h = 0; h < found; h++)
                {
                    Collider collider = buffer[h].collider;
                    if (collider == null ||
                        collider.transform.IsChildOf(chainRoot))
                        continue;
                    if (buffer[h].distance < closest)
                    {
                        chosen = buffer[h];
                        closest = buffer[h].distance;
                    }
                }
                if (closest == float.PositiveInfinity)
                    return VehicleRecoveryResult.GroundNotFound;
                if (chosen.normal.y < MinRoadNormalY)
                    return VehicleRecoveryResult.GroundTooSteep;

                float rootY = chosen.point.y - offset.y +
                    wheel.RadiusMeters + wheel.SuspensionTravelMeters +
                    WheelClearanceMeters;
                minimumRootY = Mathf.Max(minimumRootY, rootY);
            }
            return VehicleRecoveryResult.Success;
        }

        private static bool IsVolumeBlocked(
            VehicleSimulationRoot simulation, Vector3 position, Quaternion rotation,
            VehicleRoot[] allVehicles, int connectedCount, Collider[] buffer)
        {
            // M1 demo volumes are direct child chassis cubes. Imported
            // compound meshes will need a more general clearance provider.
            BoxCollider cube = simulation.GetComponentInChildren<BoxCollider>();
            if (cube == null || cube.transform.parent != simulation.transform)
                return false;

            Vector3 scale = cube.transform.localScale;
            Vector3 absScale = new Vector3(
                Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Vector3 half = Vector3.Scale(cube.size * 0.48f, absScale);
            Vector3 localCenter = cube.transform.localPosition +
                cube.transform.localRotation * Vector3.Scale(cube.center, scale);
            Vector3 center = position + rotation * localCenter;
            Quaternion orientation = rotation * cube.transform.localRotation;
            int n = Physics.OverlapBoxNonAlloc(center, half, buffer, orientation,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (n >= buffer.Length) return true;

            for (int h = 0; h < n; h++)
            {
                Collider hit = buffer[h];
                if (hit == null) continue;
                bool ownConnectedGroup = false;
                for (int v = 0; v < connectedCount; v++)
                    if (allVehicles[v] != null &&
                        hit.transform.IsChildOf(allVehicles[v].transform))
                    {
                        ownConnectedGroup = true;
                        break;
                    }
                if (!ownConnectedGroup) return true;
            }
            return false;
        }
    }
}
