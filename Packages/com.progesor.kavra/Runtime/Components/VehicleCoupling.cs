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
        InvalidConfiguration,
        AttachmentYawMisaligned,
        RelativeSpeedTooHigh,
        JointRemovalPending,
        VehicleSpeedTooHigh
    }

    public enum VehicleHitchState
    {
        Detached = 0,
        Attached = 1,
        Broken = 2
    }

    /// <summary>
    /// Actual physics hitch on the TRAILER Rigidbody. The user explicitly
    /// requests attachment/detachment; solver breakForce/breakTorque can
    /// disconnect it automatically when overstressed. Never auto-attaches.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleCoupling : MonoBehaviour
    {
        [SerializeField, HideInInspector] private ConfigurableJoint activeJoint;
        [SerializeField, HideInInspector] private VehicleHitchState recordedState;

        public ConfigurableJoint Joint => activeJoint;
        public bool IsAttached => activeJoint != null &&
                                  activeJoint.connectedBody != null;
        public Rigidbody ConnectedBody => IsAttached ? activeJoint.connectedBody : null;

        public VehicleHitchState State => IsAttached
            ? VehicleHitchState.Attached
            : recordedState == VehicleHitchState.Broken
                ? VehicleHitchState.Broken : VehicleHitchState.Detached;

        /// <summary>Recorded by Unity's OnJointBreak callback, NOT computed from velocity.</summary>
        public float LastBreakForceNewtons { get; private set; }
        public int PhysicalBreakCount { get; private set; }

        private void Awake()
        {
            // Rebuild a serialized reference if an assembly prefab was loaded.
            if (activeJoint == null)
                activeJoint = GetComponent<ConfigurableJoint>();
            if (IsAttached)
                recordedState = VehicleHitchState.Attached;
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
            if (IsAttached)
                return VehicleCouplingResult.AlreadyAttached;

            // In Play Mode Destroy(joint) is deferred until end of frame.
            // The old component must be physically gone before reattaching;
            // otherwise Unity would create two constraints accidentally.
            if (GetComponent<ConfigurableJoint>() != null)
                return VehicleCouplingResult.JointRemovalPending;

            float maxBodySpeedSq = definition.MaximumManualCouplingBodySpeedMps *
                                   definition.MaximumManualCouplingBodySpeedMps;
            if (tractorBody.linearVelocity.sqrMagnitude > maxBodySpeedSq ||
                trailerBody.linearVelocity.sqrMagnitude > maxBodySpeedSq)
                return VehicleCouplingResult.VehicleSpeedTooHigh;

            Vector3 tractorPoint = VehicleAssemblyMath.HitchWorldPosition(
                tractorBody.position, tractorBody.rotation, definition.TractorHitchLocal);
            Vector3 trailerPoint = VehicleAssemblyMath.HitchWorldPosition(
                trailerBody.position, trailerBody.rotation, definition.TrailerHitchLocal);

            if (Vector3.Distance(tractorPoint, trailerPoint) >
                definition.MaximumAttachErrorMeters)
                return VehicleCouplingResult.AttachmentPointsSeparated;

            Vector3 tractorHeading = Vector3.ProjectOnPlane(
                tractorBody.rotation * Vector3.forward, Vector3.up);
            Vector3 trailerHeading = Vector3.ProjectOnPlane(
                trailerBody.rotation * Vector3.forward, Vector3.up);
            if (tractorHeading.sqrMagnitude < 0.00001f ||
                trailerHeading.sqrMagnitude < 0.00001f ||
                Vector3.Angle(tractorHeading, trailerHeading) >
                    definition.MaximumAttachYawDegrees)
                return VehicleCouplingResult.AttachmentYawMisaligned;

            // Use hitch-point velocity rather than chassis center velocity:
            // an aggressively rotating trailer nose can still be dangerous.
            Vector3 relativeHitchVelocity =
                tractorBody.GetPointVelocity(tractorPoint) -
                trailerBody.GetPointVelocity(trailerPoint);
            if (relativeHitchVelocity.sqrMagnitude >
                definition.MaximumAttachRelativeSpeedMps *
                definition.MaximumAttachRelativeSpeedMps)
                return VehicleCouplingResult.RelativeSpeedTooHigh;

            // Attach is already explicitly requested and speed/position/yaw
            // were preflighted. Small positional snap prevents the PhysX
            // joint solver from delivering a violent initial correction.
            // This NEVER occurs automatically, and never exceeds tolerance.
            if (definition.AlignHitchesOnManualAttach)
            {
                Vector3 correction = tractorPoint - trailerPoint;
                if (correction.sqrMagnitude > 0.000001f)
                {
                    trailerBody.position += correction;
                    Physics.SyncTransforms();
                }
            }

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

            // Real Unity physics break thresholds. Zero means deliberately
            // unbreakable. The thresholds are sample values, not calibrated
            // fifth-wheel hardware. Joint angular torque breaking requires
            // an axis to be locked/limited (yaw is free).
            created.breakForce = definition.BreakForceNewtons > 0f
                ? definition.BreakForceNewtons : Mathf.Infinity;
            created.breakTorque = definition.BreakTorqueNewtonMeters > 0f
                ? definition.BreakTorqueNewtonMeters : Mathf.Infinity;

            created.projectionMode = JointProjectionMode.None;
            created.enableCollision = false;
            activeJoint = created;
            recordedState = VehicleHitchState.Attached;
            return VehicleCouplingResult.Connected;
        }

        /// <summary>
        /// Manual disconnect. Destroy is deferred to end of frame in Play
        /// Mode; trying to reattach that same frame returns JointRemovalPending.
        /// Never null connectedBody: that would WORLD-anchor a live joint.
        /// </summary>
        public bool Detach()
        {
            if (!IsAttached) return false;
            ConfigurableJoint old = activeJoint;
            activeJoint = null;
            recordedState = VehicleHitchState.Detached;
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);
            return true;
        }

        // Unity sends this message only when the actual physics joint
        // exceeds its breakForce/breakTorque. It removes the joint itself.
        // This is NOT called by the driver or any rollover/angle check.
        private void OnJointBreak(float breakForce)
        {
            if (recordedState != VehicleHitchState.Attached)
                return; // Ignore late events from an explicitly removed joint.

            activeJoint = null;
            recordedState = VehicleHitchState.Broken;
            LastBreakForceNewtons = breakForce;
            PhysicalBreakCount++;
            Debug.LogWarning("KAVRA: trailer hitch BROKE under solver load (reported " +
                             breakForce.ToString("F0") + " N). Driver must reconnect manually.", this);
        }
    }
}
