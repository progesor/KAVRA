using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Per-wheel physics telemetry, updated every FixedUpdate.</summary>
    [Serializable]
    public struct WheelTelemetry
    {
        public string WheelId;
        public bool Grounded;
        public float CompressionMeters;
        public float CompressionVelocityMps;
        public float SuspensionForceN;
        public float NormalLoadN;
        public float LongitudinalForceN;
        public float LateralForceN;
        public float LongitudinalSlipMps;
        public float LateralSlipMps;
        public float AngularVelocityRadPerSec;
        public float WheelRpm;
        public Vector3 ContactPoint;
        public Vector3 ContactNormal;
    }

    /// <summary>
    /// M0 single-Rigidbody test simulator. Raycast spring/damper and velocity-slip
    /// tire forces are approximations, NOT a validated road-vehicle simulator.
    /// No WheelCollider, engine, gearbox or differential is required.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleRoot), typeof(Rigidbody))]
    public sealed class VehicleSimulationRoot : MonoBehaviour
    {
        [Header("Contact")]
        public LayerMask GroundLayers = -1;
        public bool DrawDebugContacts;

        [Header("Suspension")]
        [Range(0.1f, 0.9f)] public float TargetStaticCompressionFraction = 0.5f;
        [Range(0f, 2f)] public float CompressionDampingRatio = 0.55f;
        [Range(0f, 2f)] public float ReboundDampingRatio = 0.45f;

        [Header("Tire: provisional velocity-slip model")]
        [Min(0f)] public float LongitudinalStiffnessNsPerMeter = 6500f;
        [Min(0f)] public float LateralStiffnessNsPerMeter = 8000f;
        [Range(0f, 3f)] public float LongitudinalFriction = 1.05f;
        [Range(0f, 3f)] public float LateralFriction = 1.05f;
        [Range(0f, 0.1f)] public float RollingResistanceCoefficient = 0.014f;
        [Min(0.1f)] public float WheelMassKg = 25f;
        [Range(1, 8)] public int TireSubsteps = 4;

        [Header("Test controls — NOT a powertrain")]
        [Range(-1f, 1f)] public float Throttle;
        [Range(-1f, 1f)] public float Steering;
        [Range(0f, 1f)] public float ServiceBrake;
        [Range(0f, 1f)] public float ParkingBrake;
        [Min(0f)] public float DriveTorquePerDrivenWheelNm = 1100f;
        [Min(0f)] public float ServiceBrakeTorquePerWheelNm = 1800f;
        [Min(0f)] public float ParkingBrakeTorquePerWheelNm = 2100f;
        [Range(0f, 65f)] public float MaximumSteerAngleDeg = 28f;

        [Header("Read-only output while playing")]
        [SerializeField] private WheelTelemetry[] wheelTelemetry = new WheelTelemetry[0];

        private sealed class WheelState
        {
            public WheelDefinition Definition;
            public VehicleWheelAnchor Anchor;
            public Transform Visual;
            public Quaternion BaseVisualRotation;
            public float OmegaRadPerSec;
            public float SpinAngleDeg;
            public float PreviousCompression;
            public bool WasGrounded;
        }

        private readonly RaycastHit[] raycastHits = new RaycastHit[32];
        private WheelState[] wheels;
        private Rigidbody body;
        private bool ready;

        public IReadOnlyList<WheelTelemetry> Telemetry => wheelTelemetry;

        public void SetInputs(float throttle, float steering, float serviceBrake, float parkingBrake)
        {
            Throttle = Mathf.Clamp(throttle, -1f, 1f);
            Steering = Mathf.Clamp(steering, -1f, 1f);
            ServiceBrake = Mathf.Clamp01(serviceBrake);
            ParkingBrake = Mathf.Clamp01(parkingBrake);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            VehicleRoot vehicle = GetComponent<VehicleRoot>();
            VehicleDefinition definition = vehicle != null ? vehicle.Definition : null;
            List<VehicleValidationIssue> issues = VehicleConfigurationValidator.Validate(definition);
            foreach (VehicleValidationIssue issue in issues)
            {
                if (issue.Severity != ValidationSeverity.Error) continue;
                Debug.LogError("KAVRA: " + issue.Message, this);
                enabled = false;
                return;
            }

            if (body.isKinematic)
            {
                Debug.LogError("KAVRA: VehicleSimulationRoot needs a non-kinematic Rigidbody. " +
                               "Use the dynamic M0 Wizard action.", this);
                enabled = false;
                return;
            }

            VehicleWheelAnchor[] anchors = GetComponentsInChildren<VehicleWheelAnchor>(true);
            var byId = new Dictionary<string, VehicleWheelAnchor>(StringComparer.Ordinal);
            foreach (VehicleWheelAnchor anchor in anchors)
            {
                if (anchor == null || string.IsNullOrEmpty(anchor.WheelId)
                    || byId.ContainsKey(anchor.WheelId))
                {
                    Debug.LogError("KAVRA: duplicate or empty wheel anchor ID.", this);
                    enabled = false;
                    return;
                }
                byId.Add(anchor.WheelId, anchor);
            }

            wheels = new WheelState[definition.Wheels.Count];
            wheelTelemetry = new WheelTelemetry[wheels.Length];
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelDefinition spec = definition.Wheels[i];
                VehicleWheelAnchor anchor;
                if (!byId.TryGetValue(spec.Id, out anchor) || spec.SuspensionTravelMeters <= 0f)
                {
                    Debug.LogError("KAVRA: missing wheel anchor or non-positive suspension travel: " + spec.Id, this);
                    enabled = false;
                    return;
                }

                Transform visual = anchor.VisualTransform != null
                    ? anchor.VisualTransform
                    : anchor.transform.Find("__WHEEL_PREVIEW");
                wheels[i] = new WheelState
                {
                    Definition = spec,
                    Anchor = anchor,
                    Visual = visual,
                    BaseVisualRotation = visual != null ? visual.localRotation : Quaternion.identity
                };
                wheelTelemetry[i].WheelId = spec.Id;
            }

            body.mass = definition.ChassisMassKg;
            body.centerOfMass = definition.CenterOfMassLocal;
            ready = true;
        }

        private void FixedUpdate()
        {
            if (!ready) return;

            float dt = Time.fixedDeltaTime;
            if (dt <= 0f) return;
            float sprungMassPerWheel = body.mass / wheels.Length;
            int substeps = Mathf.Max(1, TireSubsteps);

            for (int i = 0; i < wheels.Length; i++)
            {
                WheelState w = wheels[i];
                WheelDefinition spec = w.Definition;
                Transform mount = w.Anchor.transform;
                Vector3 suspensionUp = mount.up;
                float radius = spec.RadiusMeters;
                float travel = spec.SuspensionTravelMeters;
                float rayLength = radius + travel + 0.02f;

                RaycastHit hit;
                bool grounded = WheelContactProbe.TryRaycast(mount.position, -suspensionUp,
                    rayLength, GroundLayers.value, transform, body, raycastHits, out hit);

                float targetCompression = Mathf.Max(0.01f,
                    travel * TargetStaticCompressionFraction);
                float springRate = sprungMassPerWheel * Physics.gravity.magnitude / targetCompression;
                float criticalDamping = 2f * Mathf.Sqrt(springRate * sprungMassPerWheel);

                SuspensionSample suspension = SuspensionSolver.Evaluate(
                    grounded, radius, travel, grounded ? hit.distance : 0f,
                    springRate, criticalDamping * CompressionDampingRatio,
                    criticalDamping * ReboundDampingRatio,
                    w.PreviousCompression, w.WasGrounded, dt);

                Vector3 forward = Vector3.zero;
                Vector3 right = Vector3.zero;
                float longitudinalSpeed = 0f;
                float lateralSpeed = 0f;
                float normalLoad = 0f;
                float steerAngle = spec.CanSteer ? Steering * MaximumSteerAngleDeg : 0f;

                if (grounded)
                {
                    normalLoad = suspension.TotalForceN *
                        Mathf.Max(0f, Vector3.Dot(suspensionUp, hit.normal));

                    Vector3 steeredForward = Quaternion.AngleAxis(steerAngle, suspensionUp) * mount.forward;
                    forward = Vector3.ProjectOnPlane(steeredForward, hit.normal);
                    if (forward.sqrMagnitude < 0.0001f)
                        forward = Vector3.ProjectOnPlane(mount.right, hit.normal);
                    forward.Normalize();
                    right = Vector3.Cross(hit.normal, forward).normalized;

                    Vector3 velocity = body.GetPointVelocity(hit.point);
                    Rigidbody groundBody = hit.collider.attachedRigidbody;
                    if (groundBody != null)
                        velocity -= groundBody.GetPointVelocity(hit.point);
                    longitudinalSpeed = Vector3.Dot(velocity, forward);
                    lateralSpeed = Vector3.Dot(velocity, right);
                }

                float driveTorque = spec.CanDrive ? Throttle * DriveTorquePerDrivenWheelNm : 0f;
                float brakeTorque = (spec.HasServiceBrake ? ServiceBrake * ServiceBrakeTorquePerWheelNm : 0f)
                                  + (spec.HasParkingBrake ? ParkingBrake * ParkingBrakeTorquePerWheelNm : 0f);

                float inertia = 0.5f * WheelMassKg * radius * radius;
                float fxSum = 0f;
                float fySum = 0f;
                WheelDynamicsResult tire = default(WheelDynamicsResult);
                for (int step = 0; step < substeps; step++)
                {
                    tire = WheelDynamicsSolver.Step(new WheelDynamicsInput
                    {
                        AngularVelocityRadPerSec = w.OmegaRadPerSec,
                        LongitudinalSpeedMps = longitudinalSpeed,
                        LateralSpeedMps = lateralSpeed,
                        NormalLoadN = normalLoad,
                        WheelRadiusMeters = radius,
                        InertiaKgM2 = inertia,
                        DriveTorqueNm = driveTorque,
                        BrakeTorqueNm = brakeTorque,
                        LongitudinalStiffnessNsPerMeter = LongitudinalStiffnessNsPerMeter,
                        LateralStiffnessNsPerMeter = LateralStiffnessNsPerMeter,
                        LongitudinalFriction = LongitudinalFriction,
                        LateralFriction = LateralFriction,
                        RollingResistanceCoefficient = RollingResistanceCoefficient,
                        DeltaTime = dt / substeps
                    });
                    w.OmegaRadPerSec = tire.AngularVelocityRadPerSec;
                    fxSum += tire.LongitudinalForceN;
                    fySum += tire.LateralForceN;
                }

                float fx = fxSum / substeps;
                float fy = fySum / substeps;
                if (grounded)
                {
                    Vector3 contactForce = suspensionUp * suspension.TotalForceN +
                                           forward * fx + right * fy;
                    body.AddForceAtPosition(contactForce, hit.point, ForceMode.Force);

                    // Raycast sensors do not create native contacts. Explicit
                    // reaction keeps a dynamic ground platform from gaining force.
                    Rigidbody groundBody = hit.collider.attachedRigidbody;
                    if (groundBody != null && !groundBody.isKinematic)
                        groundBody.AddForceAtPosition(-contactForce, hit.point, ForceMode.Force);

                    if (DrawDebugContacts)
                    {
                        Debug.DrawLine(mount.position, hit.point, Color.green, dt);
                        Debug.DrawRay(hit.point, contactForce / Mathf.Max(1f, body.mass * 9.81f),
                            Color.yellow, dt);
                    }
                }
                else if (DrawDebugContacts)
                {
                    Debug.DrawRay(mount.position, -suspensionUp * rayLength, Color.red, dt);
                }

                float wheelDrop = grounded
                    ? Mathf.Clamp(hit.distance - radius, 0f, travel)
                    : travel;
                w.SpinAngleDeg = Mathf.Repeat(
                    w.SpinAngleDeg + w.OmegaRadPerSec * dt * Mathf.Rad2Deg, 360f);
                if (w.Visual != null)
                {
                    w.Visual.localPosition = Vector3.down * wheelDrop;
                    w.Visual.localRotation =
                        Quaternion.AngleAxis(steerAngle, Vector3.up) *
                        Quaternion.AngleAxis(w.SpinAngleDeg, Vector3.right) *
                        w.BaseVisualRotation;
                }

                w.PreviousCompression = suspension.CompressionMeters;
                w.WasGrounded = grounded;
                wheelTelemetry[i] = new WheelTelemetry
                {
                    WheelId = spec.Id,
                    Grounded = grounded,
                    CompressionMeters = suspension.CompressionMeters,
                    CompressionVelocityMps = suspension.CompressionVelocityMps,
                    SuspensionForceN = suspension.TotalForceN,
                    NormalLoadN = normalLoad,
                    LongitudinalForceN = fx,
                    LateralForceN = fy,
                    LongitudinalSlipMps = tire.LongitudinalSlipSpeedMps,
                    LateralSlipMps = tire.LateralSlipSpeedMps,
                    AngularVelocityRadPerSec = w.OmegaRadPerSec,
                    WheelRpm = w.OmegaRadPerSec * (60f / (2f * Mathf.PI)),
                    ContactPoint = grounded ? hit.point : Vector3.zero,
                    ContactNormal = grounded ? hit.normal : Vector3.zero
                };
            }
        }
    }
}
