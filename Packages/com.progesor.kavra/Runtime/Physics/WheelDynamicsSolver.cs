using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Inputs to the provisional M0 brush-like velocity-slip model.
    /// This is NOT Pacejka or a calibrated production tire.
    /// </summary>
    public struct WheelDynamicsInput
    {
        public float AngularVelocityRadPerSec;
        public float LongitudinalSpeedMps;
        public float LateralSpeedMps;
        public float NormalLoadN;
        public float WheelRadiusMeters;
        public float InertiaKgM2;
        public float DriveTorqueNm;
        public float BrakeTorqueNm;
        public float LongitudinalStiffnessNsPerMeter;
        public float LateralStiffnessNsPerMeter;
        public float LongitudinalFriction;
        public float LateralFriction;
        public float RollingResistanceCoefficient;
        public float DeltaTime;

        // Optional near-stop brake impulse limiter. The chassis is stepped
        // ONCE per FixedUpdate even if this rotor uses several substeps.
        // Both fields default to zero for backward-compatible direct callers.
        public float BrakeEffectiveMassKg;
        public float BodyFixedDeltaTime;
        public float LowSpeedBrakeThresholdMps;
    }

    public struct WheelDynamicsResult
    {
        public float AngularVelocityRadPerSec;
        public float LongitudinalForceN;
        public float LateralForceN;
        public float LongitudinalSlipSpeedMps;
        public float LateralSlipSpeedMps;
        public bool NearStopBrakeImpulseLimited;
    }

    public static class WheelDynamicsSolver
    {
        /// <summary>
        /// Semi-implicit wheel/tire coupling with a combined-slip friction ellipse.
        /// Low-speed slip is expressed in m/s (rather than a singular slip ratio).
        /// Airborne tires have zero road force but retain wheel angular dynamics.
        /// </summary>
        public static WheelDynamicsResult Step(WheelDynamicsInput p)
        {
            float dt = Mathf.Max(0f, p.DeltaTime);
            float inertia = Mathf.Max(0.01f, p.InertiaKgM2);
            float radius = Mathf.Max(0.01f, p.WheelRadiusMeters);
            float normalLoad = Mathf.Max(0f, p.NormalLoadN);
            float kx = Mathf.Max(0f, p.LongitudinalStiffnessNsPerMeter);
            float ky = Mathf.Max(0f, p.LateralStiffnessNsPerMeter);

            float omega = p.AngularVelocityRadPerSec;
            if (dt <= 0f)
            {
                return new WheelDynamicsResult
                {
                    AngularVelocityRadPerSec = omega,
                    LongitudinalSlipSpeedMps = omega * radius - p.LongitudinalSpeedMps,
                    LateralSlipSpeedMps = p.LateralSpeedMps
                };
            }

            // A brake always resists the current/incipient direction of rotation.
            // At a true standstill it cannot introduce negative wheel speed.
            float direction = 0f;
            if (Mathf.Abs(omega) > 0.01f)
                direction = Mathf.Sign(omega);
            else if (Mathf.Abs(p.LongitudinalSpeedMps) > 0.05f)
                direction = Mathf.Sign(p.LongitudinalSpeedMps);
            else if (Mathf.Abs(p.DriveTorqueNm) > 0.01f)
                direction = Mathf.Sign(p.DriveTorqueNm);

            float brakeTorque = Mathf.Max(0f, p.BrakeTorqueNm);
            float rollingTorque = normalLoad * Mathf.Max(0f, p.RollingResistanceCoefficient)
                                  * radius * direction;
            float netTorque = p.DriveTorqueNm - brakeTorque * direction - rollingTorque;

            // Implicit linear longitudinal slip prevents the usual explicit-Euler
            // wheel oscillation at high stiffness. The friction cap is applied below.
            float denominator = inertia + dt * kx * radius * radius;
            float omegaImplicit = (inertia * omega + dt * netTorque
                                   + dt * kx * radius * p.LongitudinalSpeedMps)
                                  / denominator;
            float fx = kx * (omegaImplicit * radius - p.LongitudinalSpeedMps);
            float fy = -ky * p.LateralSpeedMps;

            float maxFx = Mathf.Max(0f, p.LongitudinalFriction) * normalLoad;
            float maxFy = Mathf.Max(0f, p.LateralFriction) * normalLoad;
            fx = maxFx > 0f ? Mathf.Clamp(fx, -maxFx, maxFx) : 0f;
            fy = maxFy > 0f ? Mathf.Clamp(fy, -maxFy, maxFy) : 0f;

            float x = maxFx > 0f ? fx / maxFx : 0f;
            float y = maxFy > 0f ? fy / maxFy : 0f;
            float ellipse = x * x + y * y;
            if (ellipse > 1f)
            {
                float scale = 1f / Mathf.Sqrt(ellipse);
                fx *= scale;
                fy *= scale;
            }

            // The v0.0.5 model applied ~mu*Fz even while a locked wheel was
            // crawling at 0.08m/s. Four wheels could reverse a 1450kg chassis
            // in a SINGLE Rigidbody step, leading to +14kN/-14kN chatter.
            // Bound ONLY a near-stationary, brake-dominated longitudinal
            // opposing force by the impulse needed to stop that wheel's
            // nominal share of chassis velocity within one BODY time step.
            //
            // Rotor substeps DO NOT substep Rigidbody. Do not use rotor dt
            // here or it will still over-brake the chassis. We intentionally
            // do not change sliding friction at normal driving speeds.
            bool nearStopLimited = false;
            if (brakeTorque > Mathf.Abs(p.DriveTorqueNm) &&
                p.BrakeEffectiveMassKg > 0f && p.BodyFixedDeltaTime > 0f &&
                p.LowSpeedBrakeThresholdMps > 0f &&
                Mathf.Abs(p.LongitudinalSpeedMps) < p.LowSpeedBrakeThresholdMps &&
                Mathf.Abs(omega * radius) < p.LowSpeedBrakeThresholdMps &&
                fx * p.LongitudinalSpeedMps < 0f)
            {
                float forceToStop = Mathf.Max(0f, p.BrakeEffectiveMassKg) *
                    Mathf.Abs(p.LongitudinalSpeedMps) / p.BodyFixedDeltaTime;
                float priorFx = fx;
                fx = Mathf.Clamp(fx, -forceToStop, forceToStop);
                nearStopLimited = Mathf.Abs(priorFx - fx) > 0.0001f;
            }

            // When saturated, reaction torque uses the applied road force.
            // Near-stop limits therefore affect the rotor consistently too.
            float nextOmega = omega + dt * (netTorque - fx * radius) / inertia;

            // Model static brake lock without letting a stationary brake spin
            // the wheel in reverse; the residual brake torque balances it.
            if (brakeTorque > Mathf.Abs(p.DriveTorqueNm)
                && direction != 0f && nextOmega * direction < 0f)
                nextOmega = 0f;

            return new WheelDynamicsResult
            {
                AngularVelocityRadPerSec = nextOmega,
                LongitudinalForceN = fx,
                LateralForceN = fy,
                LongitudinalSlipSpeedMps = nextOmega * radius - p.LongitudinalSpeedMps,
                LateralSlipSpeedMps = p.LateralSpeedMps,
                NearStopBrakeImpulseLimited = nearStopLimited
            };
        }
    }
}
