using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Vehicle turning geometry, not automatic yaw or roll correction.</summary>
    public enum SteeringGeometryMode
    {
        Parallel = 0,
        ApproximateAckermann = 1
    }

    /// <summary>
    /// Geometric single-steering-group approximation based on the centroid of
    /// steerable/non-steerable axles. Supports 3W/4W/multi-axle and rear-steer.
    /// Does not yet model independently selectable crab/counter-steer groups.
    /// </summary>
    public struct SteeringGeometryParameters
    {
        public bool HasGeometry;
        public float PivotZ;
        public float SteeredCenterZ;
        public float NominalWheelbaseMeters;
    }

    public static class SteeringGeometrySolver
    {
        /// <summary>Analyze wheel groups once when a vehicle initializes.</summary>
        public static SteeringGeometryParameters Analyze(IReadOnlyList<WheelDefinition> wheels)
        {
            var result = new SteeringGeometryParameters();
            if (wheels == null) return result;
            float steeredZ = 0f, unsteeredZ = 0f;
            int steered = 0, unsteered = 0;
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelDefinition wheel = wheels[i];
                if (wheel == null) continue;
                if (wheel.CanSteer) { steeredZ += wheel.MountLocalPosition.z; steered++; }
                else { unsteeredZ += wheel.MountLocalPosition.z; unsteered++; }
            }

            if (steered == 0 || unsteered == 0) return result;
            result.PivotZ = unsteeredZ / unsteered;
            result.SteeredCenterZ = steeredZ / steered;
            result.NominalWheelbaseMeters = Mathf.Abs(result.SteeredCenterZ - result.PivotZ);
            result.HasGeometry = result.NominalWheelbaseMeters > 0.05f;
            return result;
        }

        /// <summary>Convenience overload for tests; Analyze once for runtime.</summary>
        public static float CalculateAngleDegrees(
            IReadOnlyList<WheelDefinition> wheels, WheelDefinition current,
            float normalizedSteering, float maximumCenterSteerDegrees,
            SteeringGeometryMode mode)
        {
            return CalculateAngleDegrees(Analyze(wheels), current, normalizedSteering,
                maximumCenterSteerDegrees, mode);
        }

        public static float CalculateAngleDegrees(
            SteeringGeometryParameters geometry, WheelDefinition current,
            float normalizedSteering, float maximumCenterSteerDegrees,
            SteeringGeometryMode mode)
        {
            if (current == null || !current.CanSteer) return 0f;
            float input = Mathf.Clamp(normalizedSteering, -1f, 1f);
            float centerAngle = Mathf.Clamp(maximumCenterSteerDegrees, 0f, 75f) * input;
            if (mode == SteeringGeometryMode.Parallel || !geometry.HasGeometry ||
                Mathf.Abs(centerAngle) < 0.0001f)
                return centerAngle;

            float turnSign = Mathf.Sign(input);
            float radius = geometry.NominalWheelbaseMeters /
                Mathf.Tan(Mathf.Abs(centerAngle) * Mathf.Deg2Rad);
            float longitudinal = current.MountLocalPosition.z - geometry.PivotZ;
            float lateralRadius = radius - turnSign * current.MountLocalPosition.x;
            float angle = Mathf.Atan2(turnSign * longitudinal, lateralRadius) *
                          Mathf.Rad2Deg;
            return Mathf.Clamp(angle, -85f, 85f);
        }
    }
}
