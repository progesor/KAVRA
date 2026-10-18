using System;
using System.Collections.Generic;

namespace Progesor.Kavra
{
    public enum ValidationSeverity { Info, Warning, Error }

    public sealed class VehicleValidationIssue
    {
        public readonly ValidationSeverity Severity;
        public readonly string Message;

        public VehicleValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>No side effects; safe to call from runtime and Editor.</summary>
    public static class VehicleConfigurationValidator
    {
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(UnityEngine.Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        public static List<VehicleValidationIssue> Validate(VehicleDefinition definition)
        {
            var issues = new List<VehicleValidationIssue>();
            if (definition == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "VehicleDefinition is null."));
                return issues;
            }

            if (!IsFinite(definition.ChassisMassKg) || definition.ChassisMassKg <= 0f)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Chassis mass must be finite and greater than zero."));
            if (!IsFinite(definition.CenterOfMassLocal))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Center of mass must have finite local coordinates."));

            if (definition.Axles == null || definition.Axles.Count == 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "At least one axle definition is required."));
            if (definition.Wheels == null || definition.Wheels.Count == 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "At least one wheel is required."));

            var axleIds = new HashSet<string>(StringComparer.Ordinal);
            if (definition.Axles != null)
            {
                foreach (var axle in definition.Axles)
                {
                    if (axle == null || string.IsNullOrWhiteSpace(axle.Id))
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "An axle has a missing ID."));
                        continue;
                    }
                    if (!axleIds.Add(axle.Id))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Duplicate axle ID: " + axle.Id));
                    if (axle.Id != axle.Id.Trim())
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Axle ID contains leading/trailing whitespace: " + axle.Id));
                    if (axle.Linkage != AxleLinkage.Independent &&
                        axle.Linkage != AxleLinkage.Rigid &&
                        axle.Linkage != AxleLinkage.Bogie &&
                        axle.Linkage != AxleLinkage.Virtual)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Axle " + axle.Id + " has an unknown linkage mode."));
                    if (axle.DifferentialMode != AxleDifferentialMode.EqualTorque &&
                        axle.DifferentialMode != AxleDifferentialMode.SpeedBiasedApproximation)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Axle " + axle.Id + " has an unknown torque routing mode."));
                    if (axle.DriveTorqueWeight < 0f ||
                        float.IsNaN(axle.DriveTorqueWeight) ||
                        float.IsInfinity(axle.DriveTorqueWeight))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Axle " + axle.Id + " has an invalid drive torque weight."));
                    if (axle.DifferentialMode == AxleDifferentialMode.SpeedBiasedApproximation &&
                        (axle.MaximumSpeedBiasRatio < 1f ||
                         float.IsNaN(axle.MaximumSpeedBiasRatio) ||
                         float.IsInfinity(axle.MaximumSpeedBiasRatio) ||
                         axle.SpeedBiasGainPerRadPerSec < 0f ||
                         float.IsNaN(axle.SpeedBiasGainPerRadPerSec) ||
                         float.IsInfinity(axle.SpeedBiasGainPerRadPerSec)))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Axle " + axle.Id + " has invalid speed-bias settings."));
                }
            }

            var wheelIds = new HashSet<string>(StringComparer.Ordinal);
            var usedAxles = new HashSet<string>(StringComparer.Ordinal);
            if (definition.Wheels != null)
            {
                foreach (var wheel in definition.Wheels)
                {
                    if (wheel == null)
                    {
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "A wheel entry is null."));
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(wheel.Id) || !wheelIds.Add(wheel.Id))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Missing or duplicate wheel ID: " + wheel.Id));
                    if (!string.IsNullOrWhiteSpace(wheel.Id) && wheel.Id != wheel.Id.Trim())
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + wheel.Id + " contains leading/trailing whitespace."));
                    if (wheel.Side != WheelSide.Left && wheel.Side != WheelSide.Center &&
                        wheel.Side != WheelSide.Right)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + wheel.Id + " has an unknown side."));
                    if (string.IsNullOrWhiteSpace(wheel.AxleId) || !axleIds.Contains(wheel.AxleId))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Wheel " + wheel.Id + " references a missing axle."));
                    else usedAxles.Add(wheel.AxleId);
                    if (!IsFinite(wheel.MountLocalPosition))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + wheel.Id + " has non-finite local mount coordinates."));
                    if (!IsFinite(wheel.RadiusMeters) || wheel.RadiusMeters <= 0f ||
                        !IsFinite(wheel.WidthMeters) || wheel.WidthMeters <= 0f)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + wheel.Id + " has invalid radius or width."));
                    if (!IsFinite(wheel.SuspensionTravelMeters) ||
                        wheel.SuspensionTravelMeters < 0f)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Wheel " + wheel.Id + " has invalid suspension travel."));
                }

                if (definition.Wheels.Count < 3)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Warning,
                        "Fewer than 3 wheels: possible for future specialized vehicles, not in the initial M0 presets."));
            }

            if (definition.Axles != null)
                foreach (var axle in definition.Axles)
                    if (axle != null && !string.IsNullOrWhiteSpace(axle.Id) && !usedAxles.Contains(axle.Id))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Warning, "Unused axle: " + axle.Id));

            // Optional powertrain is validated only when attached. Old M0
            // vehicle assets remain valid without a powertrain.
            if (definition.Powertrain != null)
            {
                var p = definition.Powertrain;
                if (!IsFinite(p.IdleRpm) || !IsFinite(p.MaxRpm) ||
                    p.IdleRpm < 100f || p.MaxRpm <= p.IdleRpm)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain RPM settings must be finite (MaxRpm above IdleRpm)."));
                if (!IsFinite(p.EngineRpmResponsePerSecond) ||
                    p.EngineRpmResponsePerSecond <= 0f ||
                    !IsFinite(p.LaunchFreeRevFraction) ||
                    p.LaunchFreeRevFraction < 0f || p.LaunchFreeRevFraction > 1f)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain engine response / free-rev fraction is invalid."));
                if (!IsFinite(p.FinalDriveRatio) || p.FinalDriveRatio <= 0f ||
                    !IsFinite(p.ReverseGearRatio) || p.ReverseGearRatio <= 0f ||
                    !IsFinite(p.Efficiency) || p.Efficiency <= 0f ||
                    p.Efficiency > 1f)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain final-drive/reverse/efficiency settings are invalid."));
                if (!IsFinite(p.ShiftDurationSeconds) || p.ShiftDurationSeconds < 0f ||
                    !IsFinite(p.MinTimeBetweenShiftsSeconds) ||
                    p.MinTimeBetweenShiftsSeconds <= 0f ||
                    !IsFinite(p.AutomaticUpshiftRpm) || p.AutomaticUpshiftRpm <= 0f ||
                    !IsFinite(p.AutomaticDownshiftRpm) || p.AutomaticDownshiftRpm <= 0f)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain shift timing and RPM thresholds must be finite and non-negative."));
                if (p.ForwardGearRatios == null || p.ForwardGearRatios.Length == 0)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain needs at least one forward gear."));
                else
                    foreach (float ratio in p.ForwardGearRatios)
                        if (!IsFinite(ratio) || ratio <= 0f)
                        {
                            issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                                "Powertrain forward gear ratios must be finite and positive."));
                            break;
                        }
                if (p.TorqueNmByRpm == null || p.TorqueNmByRpm.length == 0)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain requires an RPM-to-torque curve."));
                else
                    foreach (UnityEngine.Keyframe key in p.TorqueNmByRpm.keys)
                        if (!IsFinite(key.time) || !IsFinite(key.value) || key.time < 0f)
                        {
                            issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                                "Powertrain torque curve contains an invalid RPM/torque key."));
                            break;
                        }
                bool driven = false;
                if (definition.Wheels != null)
                    foreach (var wheel in definition.Wheels)
                        if (wheel != null && wheel.CanDrive) { driven = true; break; }
                if (!driven)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain cannot run: vehicle has no driven wheels."));
            }

            if (issues.Count == 0)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Info, "Configuration structure is valid (physics not yet verified)."));
            return issues;
        }
    }
}
