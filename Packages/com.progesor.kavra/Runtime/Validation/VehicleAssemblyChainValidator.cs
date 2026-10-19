using System.Collections.Generic;

namespace Progesor.Kavra
{
    /// <summary>Pure source-asset checks; does not mutate serialized data.</summary>
    public static class VehicleAssemblyChainValidator
    {
        public static List<VehicleValidationIssue> Validate(
            VehicleAssemblyChainDefinition chain)
        {
            var issues = new List<VehicleValidationIssue>();
            if (chain == null || chain.Links == null ||
                chain.Links.Count == 0 || chain.Links.Count > 16)
            {
                issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                    "Chain requires 1..16 ordered VehicleAssemblyDefinition links."));
                return issues;
            }

            var seen = new HashSet<VehicleDefinition>();
            VehicleDefinition expectedTractor = null;
            for (int i = 0; i < chain.Links.Count; i++)
            {
                VehicleAssemblyDefinition link = chain.Links[i];
                if (link == null)
                {
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Chain link " + i + " is null."));
                    continue;
                }

                foreach (VehicleValidationIssue result in VehicleAssemblyValidator.Validate(link))
                    if (result.Severity == ValidationSeverity.Error)
                        issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                            "Link " + i + ": " + result.Message));

                if (i == 0)
                {
                    if (link.Tractor != null) seen.Add(link.Tractor);
                }
                else if (link.Tractor != expectedTractor)
                {
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Link " + i + " must use the preceding link's Trailer as Tractor."));
                }

                if (link.Trailer != null && !seen.Add(link.Trailer))
                    issues.Add(new VehicleValidationIssue(ValidationSeverity.Error,
                        "Repeated VehicleDefinition in chain (cycle or same-body reuse) at link " +
                        i + ". Use separate trailer definition assets."));

                expectedTractor = link.Trailer;
            }
            return issues;
        }

        public static bool HasErrors(VehicleAssemblyChainDefinition chain)
        {
            foreach (VehicleValidationIssue issue in Validate(chain))
                if (issue.Severity == ValidationSeverity.Error) return true;
            return false;
        }
    }
}
