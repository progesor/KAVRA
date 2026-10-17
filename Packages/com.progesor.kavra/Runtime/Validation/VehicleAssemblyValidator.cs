using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Validation only; does not change existing vehicle assets.</summary>
    public static class VehicleAssemblyValidator
    {
        private static bool IsNonnegativeFinite(float v)
        {
            return v >= 0f && !float.IsNaN(v) && !float.IsInfinity(v);
        }

        private static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsInfinity(v.x) ||
                     float.IsNaN(v.y) || float.IsInfinity(v.y) ||
                     float.IsNaN(v.z) || float.IsInfinity(v.z));
        }

        public static List<VehicleValidationIssue> Validate(VehicleAssemblyDefinition assembly)
        {
            var issues = new List<VehicleValidationIssue>();
            if (assembly == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "VehicleAssemblyDefinition is null."));
                return issues;
            }

            if (assembly.Tractor == null || assembly.Trailer == null)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "An assembly needs two VehicleDefinition assets."));
                return issues;
            }
            if (assembly.Tractor == assembly.Trailer)
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Tractor and trailer must have independent VehicleDefinition assets."));

            AppendVehicleErrors(assembly.Tractor, "Tractor", issues);
            AppendVehicleErrors(assembly.Trailer, "Trailer", issues);

            if (!IsFinite(assembly.TractorHitchLocal) ||
                !IsFinite(assembly.TrailerHitchLocal))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Hitch positions must contain finite local coordinates."));
            if (assembly.MaximumPitchDegrees < 0f ||
                assembly.MaximumPitchDegrees > 60f ||
                assembly.MaximumRollDegrees < 0f ||
                assembly.MaximumRollDegrees > 45f ||
                float.IsNaN(assembly.MaximumPitchDegrees) ||
                float.IsNaN(assembly.MaximumRollDegrees))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Pitch / roll limits are outside the supported M1.3 range."));
            if (assembly.MaximumAttachErrorMeters <= 0f ||
                float.IsNaN(assembly.MaximumAttachErrorMeters) ||
                float.IsInfinity(assembly.MaximumAttachErrorMeters))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Attach tolerance must be a finite positive number."));

            if (assembly.MaximumAttachYawDegrees < 0f ||
                assembly.MaximumAttachYawDegrees > 180f ||
                float.IsNaN(assembly.MaximumAttachYawDegrees) ||
                float.IsInfinity(assembly.MaximumAttachYawDegrees))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Attach yaw tolerance must be within 0..180 degrees."));

            if (assembly.MaximumAttachRelativeSpeedMps <= 0f ||
                float.IsNaN(assembly.MaximumAttachRelativeSpeedMps) ||
                float.IsInfinity(assembly.MaximumAttachRelativeSpeedMps))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Maximum relative attach speed must be finite and positive."));

            if (assembly.MaximumManualCouplingBodySpeedMps <= 0f ||
                float.IsNaN(assembly.MaximumManualCouplingBodySpeedMps) ||
                float.IsInfinity(assembly.MaximumManualCouplingBodySpeedMps))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Manual hitch engagement/disengagement body speed must be finite and positive."));

            if (!IsNonnegativeFinite(assembly.BreakForceNewtons) ||
                !IsNonnegativeFinite(assembly.BreakTorqueNewtonMeters))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Hitch break force and torque must be finite and non-negative (0=unbreakable)."));

            if (assembly.TrailerServiceBrakeMultiplier < 0f ||
                float.IsNaN(assembly.TrailerServiceBrakeMultiplier) ||
                float.IsInfinity(assembly.TrailerServiceBrakeMultiplier))
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Trailer brake multiplier must be a finite non-negative number."));
            return issues;
        }

        private static void AppendVehicleErrors(VehicleDefinition vehicle, string name,
            List<VehicleValidationIssue> issues)
        {
            foreach (VehicleValidationIssue issue in
                     VehicleConfigurationValidator.Validate(vehicle))
            {
                if (issue.Severity == ValidationSeverity.Error)
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        name + ": " + issue.Message));
            }
        }

        public static bool HasErrors(VehicleAssemblyDefinition definition)
        {
            foreach (VehicleValidationIssue issue in Validate(definition))
                if (issue.Severity == ValidationSeverity.Error) return true;
            return false;
        }
    }
}
