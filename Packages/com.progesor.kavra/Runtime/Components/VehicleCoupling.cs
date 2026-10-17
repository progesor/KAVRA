using UnityEngine;

namespace Progesor.Kavra
{
    public enum VehicleCouplingResult
    {
        Connected,
        MissingBody,
        SameBody,
        BodyIsKinematic,
        AlreadyAttached,
        AttachmentPointsSeparated,
        InvalidConfiguration
    }

    /// <summary>
    /// Real two-body joint installed on the towed vehicle Rigidbody.
    /// Linear motion locked, yaw free, pitch/roll limited. No continuous
    /// auto-alignment, wheel-force hacks or artificial anti-rollover torque.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleCoupling : MonoBehaviour
    {
        [SerializeField, HideInInspector] private ConfigurableJoint activeJoint;
        public ConfigurableJoint Joint => activeJoint;
        public bool IsAttached => activeJoint != null && activeJoint.enabled &&
                                  activeJoint.connectedBody != null;
        public Rigidbody ConnectedBody => IsAttached ? activeJoint.connectedBody : null;

        private void Awake()
        {
            if (activeJoint == null)
                activeJoint = GetComponent<ConfigurableJoint>();
        }

        public VehicleCouplingResult TryAttach(Rigidbody tractorBody,
            VehicleAssemblyDefinition definition)
        {
            Rigidbody trailerBody = GetComponent<Rigidbody>();
            if (tractorBody == null || trailerBody == null)
                return VehicleCouplingResult.MissingBody;
            if (tractorBody == trailerBody)
                return VehicleCouplingResult.SameBody;
            if (tractorBody.isKinematic || trailerBody.isKinematic)
                return VehicleCouplingResult.BodyIsKinematic;
            if (definition == null || VehicleAssemblyValidator.HasErrors(definition))
                return VehicleCouplingResult.InvalidConfiguration;
            if (IsAttached || GetComponent<ConfigurableJoint>() != null)
                return VehicleCouplingResult.AlreadyAttached;

            float error = VehicleAssemblyMath.CalculateHitchErrorMeters(
                tractorBody.position, tractorBody.rotation, definition.TractorHitchLocal,
                trailerBody.position, trailerBody.rotation, definition.TrailerHitchLocal);
            if (error > definition.MaximumAttachErrorMeters)
                return VehicleCouplingResult.AttachmentPointsSeparated;

            ConfigurableJoint created = gameObject.AddComponent<ConfigurableJoint>();
            created.autoConfigureConnectedAnchor = false;
            created.anchor = definition.TrailerHitchLocal;
            created.connectedBody = tractorBody;
            created.connectedAnchor = definition.TractorHitchLocal;
            created.axis = Vector3.right;
            created.secondaryAxis = Vector3.up;

            created.xMotion = ConfigurableJointMotion.Locked;
            created.yMotion = ConfigurableJointMotion.Locked;
            created.zMotion = ConfigurableJointMotion.Locked;
            created.angularXMotion = ConfigurableJointMotion.Limited; // pitch
            created.angularYMotion = ConfigurableJointMotion.Free;    // yaw
            created.angularZMotion = ConfigurableJointMotion.Limited; // roll
            created.lowAngularXLimit = new SoftJointLimit
                { limit = -Mathf.Clamp(definition.MaximumPitchDegrees, 0f, 60f) };
            created.highAngularXLimit = new SoftJointLimit
                { limit = Mathf.Clamp(definition.MaximumPitchDegrees, 0f, 60f) };
            created.angularZLimit = new SoftJointLimit
                { limit = Mathf.Clamp(definition.MaximumRollDegrees, 0f, 45f) };

            // Projection can violate momentum and collision constraints.
            // Leave it OFF by default. Tune only after real joint profiling.
            created.projectionMode = JointProjectionMode.None;
            created.enableCollision = false;
            activeJoint = created;
            return VehicleCouplingResult.Connected;
        }

        public bool Detach()
        {
            if (!IsAttached) return false;
            ConfigurableJoint old = activeJoint;
            activeJoint = null;
            old.connectedBody = null;
            old.enabled = false;
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);
            return true;
        }
    }
}
