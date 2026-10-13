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
    }

    public struct WheelDynamicsResult
    {
        public float AngularVelocityRadPerSec;
        public float LongitudinalForceN;
        public float LateralForceN;
        public float LongitudinalSlipSpeedMps;
        public float LateralSlipSpeedMps;
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

            // When saturated, the reaction torque must use the *actual* road
            // force. When unsaturated this equals the semi-implicit solution.
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
                LateralSlipSpeedMps = p.LateralSpeedMps
            };
        }
    }
}
