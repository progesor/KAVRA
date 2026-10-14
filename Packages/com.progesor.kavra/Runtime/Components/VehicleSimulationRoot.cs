using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Per-wheel snapshots of the provisional M0 solver.</summary>
    [Serializable]
    public struct WheelTelemetry
    {
        public string WheelId;
        public bool Grounded;
        public bool UsedSphereContact;
        public bool UsedRayFallback;
        public string SurfaceName;
        public float EffectiveLongitudinalFriction;
        public float EffectiveLateralFriction;
        public float EffectiveRollingResistance;
        public float StaticLoadFraction;
        public float CompressionMeters;
        public float CompressionVelocityMps;
        public float BottomOutMeters;
        public float BumpStopForceN;
        public float AntiRollForceN;
        public float SuspensionForceN;
        public float NormalLoadN;
        public float LongitudinalForceN;
        public float LateralForceN;
        public bool BrakeImpulseLimited;
        public float LongitudinalSlipMps;
        public float LateralSlipMps;
        public float SlipRatio;
        public float SlipAngleDegrees;
        public float SteerAngleDegrees;
        public string TireProfileName;
        public float EffectiveLongitudinalStiffness;
        public float EffectiveLateralStiffness;
        public float AngularVelocityRadPerSec;
        public float WheelRpm;
        public Vector3 ContactPoint;
        public Vector3 ContactNormal;
    }

    /// <summary>
    /// M0 experimental single-Rigidbody vehicle. Approximate loads, tire
    /// forces and contact only; NOT yet calibrated simulation physics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleRoot), typeof(Rigidbody))]
    public sealed class VehicleSimulationRoot : MonoBehaviour
    {
        [Header("Contact")]
        public WheelContactMode ContactMode = WheelContactMode.Raycast;
        public LayerMask GroundLayers = -1;
        public bool DrawDebugContacts;
        public bool ShowCenterOfMassGizmo = true;
        [Tooltip("M0 A/B benchmark: measures only contact cast call duration.")]
        public bool MeasureContactQueryCost;
        public float ContactQueryMicrosecondsLastStep { get; private set; }

        [Header("Mass properties")]
        [Tooltip("Test-lab inertia estimated from wheel footprint and body height, not from the low cube collider.")]
        public bool EstimateChassisInertia = true;
        [Range(2f, 5f)] public float BodyHeightToWheelRadiusMultiplier = 3.6f;

        [Header("Suspension")]
        [Range(0.1f, 0.9f)] public float TargetStaticCompressionFraction = 0.5f;
        [Range(0f, 2f)] public float CompressionDampingRatio = 0.55f;
        [Range(0f, 2f)] public float ReboundDampingRatio = 0.45f;
        [Range(0.5f, 1f)] public float BumpStopStartFraction = 0.85f;
        [Range(0f, 12f)] public float BumpStopRateMultiplier = 4f;
        [Range(0f, 2f)] public float AntiRollRateFraction = 0.35f;

        [Header("Tire: provisional velocity-slip model")]
        [Min(0f)] public float LongitudinalStiffnessNsPerMeter = 6500f;
        [Min(0f)] public float LateralStiffnessNsPerMeter = 8000f;
        [Range(0f, 3f)] public float LongitudinalFriction = 1.05f;
        [Range(0f, 3f)] public float LateralFriction = 1.05f;
        [Range(0f, 0.1f)] public float RollingResistanceCoefficient = 0.014f;
        [Min(0.1f)] public float WheelMassKg = 25f;
        [Range(1, 8)] public int TireSubsteps = 4;
        [Range(0.1f, 5f)] public float SlipMetricReferenceSpeedMps = 1f;

        [Header("Test controls — NOT a powertrain")]
        [Range(-1f, 1f)] public float Throttle;
        [Range(-1f, 1f)] public float Steering;
        [Range(0f, 1f)] public float ServiceBrake;
        [Range(0f, 1f)] public float ParkingBrake;
        [Min(0f)] public float DriveTorquePerDrivenWheelNm = 1100f;
        [Min(0f)] public float ServiceBrakeTorquePerWheelNm = 1800f;
        [Min(0f)] public float ParkingBrakeTorquePerWheelNm = 2100f;
        [Range(0f, 65f)] public float MaximumSteerAngleDeg = 28f;

        [Header("Low-speed brake stability — M0.5 experimental")]
        [Tooltip("Limit near-stationary locked-wheel braking impulses. No automatic steering, yaw, or rollover assistance.")]
        public bool LimitNearStopBrakingImpulse = true;
        [Range(0f, 2f)] public float NearStopBrakeSpeedThresholdMps = 0.5f;

        [Tooltip("Parallel retains M0 steering; ApproximateAckermann accounts for wheel locations.")]
        public SteeringGeometryMode SteeringMode = SteeringGeometryMode.Parallel;

        [Header("Read-only snapshots while playing")]
        [SerializeField] private WheelTelemetry[] wheelTelemetry = new WheelTelemetry[0];

        private sealed class WheelState
        {
            public WheelDefinition Definition;
            public VehicleWheelAnchor Anchor;
            public Transform Visual;
            public Quaternion BaseVisualRotation;
            public float StaticLoadFraction;
            public float SpringRateNPerMeter;
            public float SprungMassKg;
            public float OmegaRadPerSec;
            public float SpinAngleDeg;
            public float PreviousRawCompressionMeters;
            public bool WasGrounded;

            // Contact and spring phase for THIS physics step.
            public bool Grounded;
            public WheelContactSample Contact;
            public SurfaceProfile Surface;
            public SurfaceTireParameters TireParameters;
            public TireTuningValues TunedTire;
            public SuspensionSample Suspension;
            public Vector3 SuspensionUp;
            public float VerticalSupportN;
            public float AntiRollForceN;
        }

        private struct AntiRollPair
        {
            public int Left;
            public int Right;
        }

        private readonly RaycastHit[] raycastHits = new RaycastHit[32];
        private WheelState[] wheels;
        private AntiRollPair[] antiRollPairs = new AntiRollPair[0];
        private SteeringGeometryParameters steeringGeometry;
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

        /// <summary>
        /// Reset wheel angular motion and spring history after an EXPLICIT
        /// user teleport. Does not move or stabilize the Rigidbody and is never
        /// called automatically from FixedUpdate or collision detection.
        /// </summary>
        public void ResetTransientMotionState()
        {
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelState wheel = wheels[i];
                wheel.OmegaRadPerSec = 0f;
                wheel.SpinAngleDeg = 0f;
                wheel.PreviousRawCompressionMeters = 0f;
                wheel.WasGrounded = false;
                wheel.Grounded = false;
                wheel.Contact = default(WheelContactSample);
                wheel.Suspension = default(SuspensionSample);
                wheel.VerticalSupportN = 0f;
                wheel.AntiRollForceN = 0f;
                if (wheelTelemetry != null && i < wheelTelemetry.Length)
                    wheelTelemetry[i] = new WheelTelemetry { WheelId = wheel.Definition.Id };
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            VehicleRoot root = GetComponent<VehicleRoot>();
            VehicleDefinition definition = root != null ? root.Definition : null;
            foreach (VehicleValidationIssue issue in VehicleConfigurationValidator.Validate(definition))
            {
                if (issue.Severity != ValidationSeverity.Error) continue;
                Debug.LogError("KAVRA: " + issue.Message, this);
                enabled = false;
                return;
            }

            if (body == null || body.isKinematic)
            {
                Debug.LogError("KAVRA: dynamic simulation requires a non-kinematic Rigidbody.", this);
                enabled = false;
                return;
            }

            VehicleWheelAnchor[] anchors = GetComponentsInChildren<VehicleWheelAnchor>(true);
            var byId = new Dictionary<string, VehicleWheelAnchor>(StringComparer.Ordinal);
            foreach (VehicleWheelAnchor anchor in anchors)
            {
                if (anchor == null || string.IsNullOrEmpty(anchor.WheelId) ||
                    byId.ContainsKey(anchor.WheelId))
                {
                    Debug.LogError("KAVRA: missing or duplicate wheel anchor ID.", this);
                    enabled = false;
                    return;
                }
                byId.Add(anchor.WheelId, anchor);
            }

            steeringGeometry = SteeringGeometrySolver.Analyze(definition.Wheels);
            int wheelCount = definition.Wheels.Count;
            float[] loadFractions = new float[wheelCount];
            bool balanced = VehicleStaticLoadEstimator.Estimate(
                definition.Wheels, definition.CenterOfMassLocal, loadFractions);
            if (!balanced)
                Debug.LogWarning("KAVRA: vehicle CoM projection is outside or unsupported by " +
                                 "its wheel footprint. Spring weights are approximate; " +
                                 "inspect CenterOfMassLocal and wheel positions.", this);

            wheels = new WheelState[wheelCount];
            wheelTelemetry = new WheelTelemetry[wheelCount];

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            float biggestRadius = 0f;
            for (int i = 0; i < wheelCount; i++)
            {
                WheelDefinition spec = definition.Wheels[i];
                VehicleWheelAnchor anchor;
                if (!byId.TryGetValue(spec.Id, out anchor) ||
                    spec.SuspensionTravelMeters <= 0f)
                {
                    Debug.LogError("KAVRA: missing anchor or invalid suspension travel for " +
                                   spec.Id, this);
                    enabled = false;
                    return;
                }

                Transform visual = anchor.VisualTransform != null
                    ? anchor.VisualTransform
                    : anchor.transform.Find("__WHEEL_PREVIEW");
                float sprungMass = definition.ChassisMassKg * loadFractions[i];
                float sag = Mathf.Max(0.01f,
                    spec.SuspensionTravelMeters * TargetStaticCompressionFraction);
                wheels[i] = new WheelState
                {
                    Definition = spec,
                    Anchor = anchor,
                    Visual = visual,
                    BaseVisualRotation = visual != null ? visual.localRotation : Quaternion.identity,
                    StaticLoadFraction = loadFractions[i],
                    SprungMassKg = sprungMass,
                    SpringRateNPerMeter = sprungMass * Physics.gravity.magnitude / sag
                };
                wheelTelemetry[i].WheelId = spec.Id;

                minX = Mathf.Min(minX, spec.MountLocalPosition.x - spec.WidthMeters * 0.5f);
                maxX = Mathf.Max(maxX, spec.MountLocalPosition.x + spec.WidthMeters * 0.5f);
                minZ = Mathf.Min(minZ, spec.MountLocalPosition.z - spec.RadiusMeters);
                maxZ = Mathf.Max(maxZ, spec.MountLocalPosition.z + spec.RadiusMeters);
                biggestRadius = Mathf.Max(biggestRadius, spec.RadiusMeters);
            }

            body.mass = definition.ChassisMassKg;
            // This value remains data-driven and is NOT silently lowered by
            // "arcade" handling. It is in local chassis coordinates.
            body.centerOfMass = definition.CenterOfMassLocal;

            if (EstimateChassisInertia)
            {
                float width = Mathf.Max(0.6f, maxX - minX);
                float length = Mathf.Max(1f, maxZ - minZ);
                float height = Mathf.Max(0.6f,
                    biggestRadius * BodyHeightToWheelRadiusMultiplier);
                float m = body.mass / 12f;
                // Approximate upright box inertia about its CoM. Inertia of
                // the Editor's very flat temporary cube is otherwise too low.
                body.automaticInertiaTensor = false;
                body.inertiaTensorRotation = Quaternion.identity;
                body.inertiaTensor = new Vector3(
                    m * (height * height + length * length),
                    m * (width * width + length * length),
                    m * (width * width + height * height));
            }

            // Pair wheels on the same axle by side. Multiwheel axles can later
            // use explicit anti-roll groups; this pairs each left only once.
            var pairs = new List<AntiRollPair>();
            bool[] rightUsed = new bool[wheelCount];
            for (int i = 0; i < wheelCount; i++)
            {
                WheelDefinition left = wheels[i].Definition;
                if (left.Side != WheelSide.Left) continue;
                int chosen = -1;
                float nearest = float.PositiveInfinity;
                for (int j = 0; j < wheelCount; j++)
                {
                    WheelDefinition right = wheels[j].Definition;
                    if (rightUsed[j] || right.Side != WheelSide.Right ||
                        !string.Equals(left.AxleId, right.AxleId, StringComparison.Ordinal))
                        continue;

                    float separation = Mathf.Abs(left.MountLocalPosition.z -
                                                  right.MountLocalPosition.z) +
                                       Mathf.Abs(Mathf.Abs(left.MountLocalPosition.x) -
                                                  Mathf.Abs(right.MountLocalPosition.x));
                    if (separation < nearest)
                    {
                        nearest = separation;
                        chosen = j;
                    }
                }
                if (chosen >= 0)
                {
                    rightUsed[chosen] = true;
                    pairs.Add(new AntiRollPair { Left = i, Right = chosen });
                }
            }
            antiRollPairs = pairs.ToArray();
            ready = true;
        }

        private void FixedUpdate()
        {
            if (!ready || wheels == null) return;
            float dt = Time.fixedDeltaTime;
            if (dt <= 0f) return;

            long contactQueryTicks = 0L;

            // Phase 1: all contacts, suspension lengths and provisional loads.
            // Splitting phases prevents per-wheel loop ordering from affecting
            // the paired anti-roll calculation.
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelState wheel = wheels[i];
                WheelDefinition spec = wheel.Definition;
                wheel.SuspensionUp = wheel.Anchor.transform.up;
                long queryStart = MeasureContactQueryCost
                    ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                wheel.Grounded = WheelContactProbe.TrySample(
                    wheel.Anchor.transform.position, wheel.SuspensionUp,
                    spec.RadiusMeters, spec.SuspensionTravelMeters, ContactMode,
                    GroundLayers.value, transform, body, raycastHits, out wheel.Contact);
                if (MeasureContactQueryCost)
                    contactQueryTicks += System.Diagnostics.Stopwatch.GetTimestamp() - queryStart;

                float criticalDamping = 2f * Mathf.Sqrt(
                    Mathf.Max(0f, wheel.SpringRateNPerMeter * wheel.SprungMassKg));
                wheel.Suspension = SuspensionSolver.Evaluate(
                    wheel.Grounded, spec.RadiusMeters, spec.SuspensionTravelMeters,
                    wheel.Grounded ? wheel.Contact.RayEquivalentDistanceMeters : 0f,
                    wheel.SpringRateNPerMeter,
                    criticalDamping * CompressionDampingRatio,
                    criticalDamping * ReboundDampingRatio,
                    wheel.PreviousRawCompressionMeters, wheel.WasGrounded, dt,
                    BumpStopStartFraction, BumpStopRateMultiplier);

                wheel.VerticalSupportN = wheel.Suspension.TotalForceN;
                wheel.AntiRollForceN = 0f;

                // The hit Collider chooses one optional surface profile per
                // wheel. Absence of a profile preserves pre-0.0.4 behavior.
                wheel.Surface = wheel.Grounded
                    ? SurfaceResponse.Resolve(wheel.Contact.Hit.collider)
                    : null;
                wheel.TireParameters = SurfaceResponse.Evaluate(
                    wheel.Surface, LongitudinalFriction, LateralFriction,
                    RollingResistanceCoefficient);
                wheel.TunedTire = TireTuningResolver.Resolve(
                    spec.TireProfile, wheel.TireParameters,
                    LongitudinalStiffnessNsPerMeter, LateralStiffnessNsPerMeter,
                    WheelMassKg, spec.RadiusMeters);
            }

            ContactQueryMicrosecondsLastStep = MeasureContactQueryCost
                ? (float)(contactQueryTicks * 1000000.0 /
                          System.Diagnostics.Stopwatch.Frequency)
                : 0f;

            // Phase 2: equivalent left/right anti-roll bar forces. Their sum
            // is zero and they never require a wheel to pull on the ground.
            for (int p = 0; p < antiRollPairs.Length; p++)
            {
                WheelState left = wheels[antiRollPairs[p].Left];
                WheelState right = wheels[antiRollPairs[p].Right];
                if (!left.Grounded || !right.Grounded) continue;

                float rate = (left.SpringRateNPerMeter +
                              right.SpringRateNPerMeter) * 0.5f *
                             Mathf.Max(0f, AntiRollRateFraction);
                float transfer = AntiRollSolver.ComputeTransferN(
                    left.Suspension.CompressionMeters,
                    right.Suspension.CompressionMeters,
                    left.VerticalSupportN, right.VerticalSupportN, rate);
                left.VerticalSupportN += transfer;
                right.VerticalSupportN -= transfer;
                left.AntiRollForceN += transfer;
                right.AntiRollForceN -= transfer;
            }

            // Phase 3: tire rotor forces, Rigidbody forces and visuals.
            int substeps = Mathf.Max(1, TireSubsteps);
            for (int i = 0; i < wheels.Length; i++)
            {
                WheelState wheel = wheels[i];
                WheelDefinition spec = wheel.Definition;
                Transform mount = wheel.Anchor.transform;
                bool grounded = wheel.Grounded;
                RaycastHit hit = wheel.Contact.Hit;

                Vector3 forward = Vector3.zero;
                Vector3 right = Vector3.zero;
                float longitudinalSpeed = 0f;
                float lateralSpeed = 0f;
                float normalLoad = 0f;
                float steerAngle = SteeringGeometrySolver.CalculateAngleDegrees(
                    steeringGeometry, spec, Steering, MaximumSteerAngleDeg,
                    SteeringMode);

                if (grounded)
                {
                    normalLoad = wheel.VerticalSupportN *
                        Mathf.Max(0f, Vector3.Dot(wheel.SuspensionUp, hit.normal));

                    Vector3 steeredForward = Quaternion.AngleAxis(
                        steerAngle, wheel.SuspensionUp) * mount.forward;
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

                float driveTorque = spec.CanDrive ?
                    Throttle * DriveTorquePerDrivenWheelNm : 0f;
                float brakeTorque =
                    (spec.HasServiceBrake ? ServiceBrake * ServiceBrakeTorquePerWheelNm : 0f) +
                    (spec.HasParkingBrake ? ParkingBrake * ParkingBrakeTorquePerWheelNm : 0f);
                float inertia = wheel.TunedTire.InertiaKgM2;

                float sumFx = 0f, sumFy = 0f;
                bool anyBrakeLimit = false;
                WheelDynamicsResult tire = default(WheelDynamicsResult);
                for (int step = 0; step < substeps; step++)
                {
                    tire = WheelDynamicsSolver.Step(new WheelDynamicsInput
                    {
                        AngularVelocityRadPerSec = wheel.OmegaRadPerSec,
                        LongitudinalSpeedMps = longitudinalSpeed,
                        LateralSpeedMps = lateralSpeed,
                        NormalLoadN = normalLoad,
                        WheelRadiusMeters = spec.RadiusMeters,
                        InertiaKgM2 = inertia,
                        DriveTorqueNm = driveTorque,
                        BrakeTorqueNm = brakeTorque,
                        LongitudinalStiffnessNsPerMeter =
                            wheel.TunedTire.LongitudinalStiffnessNsPerMeter,
                        LateralStiffnessNsPerMeter =
                            wheel.TunedTire.LateralStiffnessNsPerMeter,
                        LongitudinalFriction = wheel.TunedTire.LongitudinalFriction,
                        LateralFriction = wheel.TunedTire.LateralFriction,
                        RollingResistanceCoefficient =
                            wheel.TunedTire.RollingResistanceCoefficient,
                        BrakeEffectiveMassKg = LimitNearStopBrakingImpulse
                            ? wheel.SprungMassKg : 0f,
                        BodyFixedDeltaTime = dt,
                        LowSpeedBrakeThresholdMps = NearStopBrakeSpeedThresholdMps,
                        DeltaTime = dt / substeps
                    });
                    wheel.OmegaRadPerSec = tire.AngularVelocityRadPerSec;
                    sumFx += tire.LongitudinalForceN;
                    sumFy += tire.LateralForceN;
                    anyBrakeLimit |= tire.NearStopBrakeImpulseLimited;
                }

                float fx = sumFx / substeps;
                float fy = sumFy / substeps;
                TireSlipMetrics slip = TireSlipDiagnostics.Calculate(
                    longitudinalSpeed, lateralSpeed, wheel.OmegaRadPerSec,
                    spec.RadiusMeters, SlipMetricReferenceSpeedMps);
                if (grounded)
                {
                    Vector3 force = wheel.SuspensionUp * wheel.VerticalSupportN +
                                    forward * fx + right * fy;
                    body.AddForceAtPosition(force, hit.point, ForceMode.Force);

                    Rigidbody groundBody = hit.collider.attachedRigidbody;
                    if (groundBody != null && !groundBody.isKinematic)
                        groundBody.AddForceAtPosition(-force, hit.point, ForceMode.Force);

                    if (DrawDebugContacts)
                    {
                        Debug.DrawLine(mount.position, hit.point, Color.green, dt);
                        Debug.DrawRay(hit.point, force /
                            Mathf.Max(1f, body.mass * Physics.gravity.magnitude),
                            Color.yellow, dt);
                    }
                }
                else if (DrawDebugContacts)
                {
                    Debug.DrawRay(mount.position, -wheel.SuspensionUp *
                        (spec.RadiusMeters + spec.SuspensionTravelMeters), Color.red, dt);
                }

                float visualDrop = grounded
                    ? wheel.Contact.RayEquivalentDistanceMeters - spec.RadiusMeters
                    : spec.SuspensionTravelMeters;
                // When the suspension is fully compressed, allow a small VISUAL
                // correction above the mount. The physical bottom-out remains
                // visible as telemetry and is resisted by the bump stop.
                visualDrop = Mathf.Clamp(visualDrop,
                    -Mathf.Min(0.25f, spec.RadiusMeters * 0.7f),
                    spec.SuspensionTravelMeters);
                wheel.SpinAngleDeg = Mathf.Repeat(
                    wheel.SpinAngleDeg + wheel.OmegaRadPerSec * dt * Mathf.Rad2Deg, 360f);
                if (wheel.Visual != null)
                {
                    wheel.Visual.localPosition = Vector3.down * visualDrop;
                    wheel.Visual.localRotation =
                        Quaternion.AngleAxis(steerAngle, Vector3.up) *
                        Quaternion.AngleAxis(wheel.SpinAngleDeg, Vector3.right) *
                        wheel.BaseVisualRotation;
                }

                wheel.PreviousRawCompressionMeters = wheel.Suspension.RawCompressionMeters;
                wheel.WasGrounded = grounded;
                wheelTelemetry[i] = new WheelTelemetry
                {
                    WheelId = spec.Id,
                    Grounded = grounded,
                    UsedSphereContact = grounded && wheel.Contact.IsSphereContact,
                    UsedRayFallback = grounded && wheel.Contact.UsedRayFallback,
                    SurfaceName = !grounded ? "Air" :
                        wheel.Surface != null ? wheel.Surface.name : "Default",
                    EffectiveLongitudinalFriction =
                        wheel.TunedTire.LongitudinalFriction,
                    EffectiveLateralFriction = wheel.TunedTire.LateralFriction,
                    EffectiveRollingResistance =
                        wheel.TunedTire.RollingResistanceCoefficient,
                    EffectiveLongitudinalStiffness =
                        wheel.TunedTire.LongitudinalStiffnessNsPerMeter,
                    EffectiveLateralStiffness = wheel.TunedTire.LateralStiffnessNsPerMeter,
                    TireProfileName = spec.TireProfile != null ? spec.TireProfile.name : "Default",
                    StaticLoadFraction = wheel.StaticLoadFraction,
                    CompressionMeters = wheel.Suspension.CompressionMeters,
                    CompressionVelocityMps = wheel.Suspension.CompressionVelocityMps,
                    BottomOutMeters = wheel.Suspension.BottomOutMeters,
                    BumpStopForceN = wheel.Suspension.BumpStopForceN,
                    AntiRollForceN = wheel.AntiRollForceN,
                    SuspensionForceN = wheel.VerticalSupportN,
                    NormalLoadN = normalLoad,
                    LongitudinalForceN = fx,
                    LateralForceN = fy,
                    BrakeImpulseLimited = anyBrakeLimit,
                    LongitudinalSlipMps = tire.LongitudinalSlipSpeedMps,
                    LateralSlipMps = tire.LateralSlipSpeedMps,
                    SlipRatio = slip.LongitudinalSlipRatio,
                    SlipAngleDegrees = slip.SlipAngleDegrees,
                    SteerAngleDegrees = steerAngle,
                    AngularVelocityRadPerSec = wheel.OmegaRadPerSec,
                    WheelRpm = wheel.OmegaRadPerSec * (60f / (2f * Mathf.PI)),
                    ContactPoint = grounded ? hit.point : Vector3.zero,
                    ContactNormal = grounded ? hit.normal : Vector3.zero
                };
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!ShowCenterOfMassGizmo) return;
            Rigidbody rigid = Application.isPlaying ? body : GetComponent<Rigidbody>();
            VehicleRoot root = GetComponent<VehicleRoot>();
            Vector3 localCoM = rigid != null && Application.isPlaying
                ? rigid.centerOfMass
                : root != null && root.Definition != null
                    ? root.Definition.CenterOfMassLocal
                    : Vector3.zero;
            Vector3 worldCoM = transform.TransformPoint(localCoM);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(worldCoM, 0.15f);
            Gizmos.DrawLine(worldCoM - transform.right * 0.25f,
                            worldCoM + transform.right * 0.25f);
            Gizmos.DrawLine(worldCoM - transform.up * 0.25f,
                            worldCoM + transform.up * 0.25f);
            Gizmos.DrawLine(worldCoM - transform.forward * 0.25f,
                            worldCoM + transform.forward * 0.25f);
        }
    }
}
