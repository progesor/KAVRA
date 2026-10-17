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

    /// <summary>
    /// Snapshot shared by manual coupling preflight and PhysicsLab visuals.
    /// CanAttach is represented by Result == Connected, even before the
    /// actual ConfigurableJoint is created by an explicit H request.
    /// </summary>
    public struct VehicleHitchAssessment
    {
        public VehicleCouplingResult Result;
        public float HitchGapMeters;
        public float YawErrorDegrees;
        public float RelativeHitchSpeedMps;
        public float FastestBodySpeedMps;
        public bool ReadyToAttach => Result == VehicleCouplingResult.Connected;
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

        public VehicleCouplingResult AssessAttach(Rigidbody tractorBody,
            VehicleAssemblyDefinition definition,
            out VehicleHitchAssessment assessment)
        {
            assessment = default(VehicleHitchAssessment);
            Rigidbody trailerBody = GetComponent<Rigidbody>();
            if (tractorBody == null || trailerBody == null)
                return assessment.Result = VehicleCouplingResult.MissingBody;
            if (tractorBody == trailerBody)
                return assessment.Result = VehicleCouplingResult.SameBody;
            if (tractorBody.isKinematic || trailerBody.isKinematic)
                return assessment.Result = VehicleCouplingResult.BodyIsKinematic;
            if (definition == null || VehicleAssemblyValidator.HasErrors(definition))
                return assessment.Result = VehicleCouplingResult.InvalidConfiguration;
            if (IsAttached)
                return assessment.Result = VehicleCouplingResult.AlreadyAttached;

            // Destroy(joint) is delayed in Play Mode. Do not put a second
            // ConfigurableJoint on the same trailer before the first is gone.
            if (GetComponent<ConfigurableJoint>() != null)
                return assessment.Result = VehicleCouplingResult.JointRemovalPending;

            assessment.FastestBodySpeedMps = Mathf.Max(
                tractorBody.linearVelocity.magnitude,
                trailerBody.linearVelocity.magnitude);

            Vector3 tractorPoint = VehicleAssemblyMath.HitchWorldPosition(
                tractorBody.position, tractorBody.rotation, definition.TractorHitchLocal);
            Vector3 trailerPoint = VehicleAssemblyMath.HitchWorldPosition(
                trailerBody.position, trailerBody.rotation, definition.TrailerHitchLocal);
            assessment.HitchGapMeters = Vector3.Distance(tractorPoint, trailerPoint);

            Vector3 tractorHeading = Vector3.ProjectOnPlane(
                tractorBody.rotation * Vector3.forward, Vector3.up);
            Vector3 trailerHeading = Vector3.ProjectOnPlane(
                trailerBody.rotation * Vector3.forward, Vector3.up);
            if (tractorHeading.sqrMagnitude > 0.00001f &&
                trailerHeading.sqrMagnitude > 0.00001f)
                assessment.YawErrorDegrees = Vector3.Angle(
                    tractorHeading, trailerHeading);
            else
                assessment.YawErrorDegrees = 180f;

            Vector3 relativeHitchVelocity =
                tractorBody.GetPointVelocity(tractorPoint) -
                trailerBody.GetPointVelocity(trailerPoint);
            assessment.RelativeHitchSpeedMps = relativeHitchVelocity.magnitude;

            if (assessment.FastestBodySpeedMps >
                definition.MaximumManualCouplingBodySpeedMps)
                return assessment.Result = VehicleCouplingResult.VehicleSpeedTooHigh;
            if (assessment.HitchGapMeters > definition.MaximumAttachErrorMeters)
                return assessment.Result = VehicleCouplingResult.AttachmentPointsSeparated;
            if (assessment.YawErrorDegrees > definition.MaximumAttachYawDegrees)
                return assessment.Result = VehicleCouplingResult.AttachmentYawMisaligned;
            if (assessment.RelativeHitchSpeedMps >
                definition.MaximumAttachRelativeSpeedMps)
                return assessment.Result = VehicleCouplingResult.RelativeSpeedTooHigh;
            return assessment.Result = VehicleCouplingResult.Connected;
        }

        public VehicleCouplingResult TryAttach(Rigidbody tractorBody,
            VehicleAssemblyDefinition definition)
        {
            VehicleHitchAssessment assessment;
            VehicleCouplingResult safety = AssessAttach(
                tractorBody, definition, out assessment);
            if (safety != VehicleCouplingResult.Connected)
                return safety;

            Rigidbody trailerBody = GetComponent<Rigidbody>();
            Vector3 tractorPoint = VehicleAssemblyMath.HitchWorldPosition(
                tractorBody.position, tractorBody.rotation, definition.TractorHitchLocal);
            Vector3 trailerPoint = VehicleAssemblyMath.HitchWorldPosition(
                trailerBody.position, trailerBody.rotation, definition.TrailerHitchLocal);

            // Only after an H request and full preflight can the trailer
            // move by the small configured capture gap. No proximity auto-
            // attachment and no continuous magic steering correction.
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
