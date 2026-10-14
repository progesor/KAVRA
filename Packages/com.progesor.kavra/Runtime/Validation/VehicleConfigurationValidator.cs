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
        public static List<VehicleValidationIssue> Validate(VehicleDefinition definition)
        {
            var issues = new List<VehicleValidationIssue>();
            if (definition == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "VehicleDefinition is null."));
                return issues;
            }

            if (definition.ChassisMassKg <= 0f)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Chassis mass must be greater than zero."));

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
                    if (string.IsNullOrWhiteSpace(wheel.AxleId) || !axleIds.Contains(wheel.AxleId))
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Wheel " + wheel.Id + " references a missing axle."));
                    else usedAxles.Add(wheel.AxleId);
                    if (wheel.RadiusMeters <= 0f || wheel.WidthMeters <= 0f)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Wheel " + wheel.Id + " has invalid dimensions."));
                    if (wheel.SuspensionTravelMeters < 0f)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error, "Wheel " + wheel.Id + " has negative suspension travel."));
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
                if (p.IdleRpm < 100f || p.MaxRpm <= p.IdleRpm)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain RPM settings are invalid (MaxRpm must exceed IdleRpm)."));
                if (p.FinalDriveRatio <= 0f || p.ReverseGearRatio <= 0f ||
                    p.Efficiency <= 0f || p.Efficiency > 1f)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain final-drive/reverse/efficiency settings are invalid."));
                if (p.ForwardGearRatios == null || p.ForwardGearRatios.Length == 0)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain needs at least one forward gear."));
                else
                    foreach (float ratio in p.ForwardGearRatios)
                        if (ratio <= 0f)
                        {
                            issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                                "Powertrain forward gear ratios must be positive."));
                            break;
                        }
                if (p.TorqueNmByRpm == null || p.TorqueNmByRpm.length == 0)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Powertrain requires an RPM-to-torque curve."));
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
