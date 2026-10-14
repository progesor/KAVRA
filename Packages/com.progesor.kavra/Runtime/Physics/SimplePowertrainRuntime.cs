using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// A genuinely wheel-speed-dependent but intentionally simple power path:
    /// engine torque curve -> selected gear -> final drive -> equal power
    /// among driven AXLES -> equally among driven wheels on each axle.
    ///
    /// This is an initial drivetrain module, NOT a torque-constrained clutch,
    /// differential, locked axle, converter or full combustion simulation.
    /// All allocations happen at construction. No Rigidbody ownership.
    /// </summary>
    public sealed class SimplePowertrainRuntime
    {
        private readonly SimplePowertrainDefinition config;
        private readonly int[] wheelToAxle;
        private readonly int[] wheelCountPerAxle;
        private readonly int drivenAxleCount;
        private readonly int drivenWheelCount;

        public float EngineRpm { get; private set; }
        public float EngineTorqueNm { get; private set; }
        public float TotalWheelTorqueNm { get; private set; }
        public int CurrentGear { get; private set; }

        public int DrivenWheelCount => drivenWheelCount;
        public int DrivenAxleCount => drivenAxleCount;

        public SimplePowertrainRuntime(SimplePowertrainDefinition definition,
            IReadOnlyList<WheelDefinition> wheels)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (wheels == null) throw new ArgumentNullException(nameof(wheels));
            config = definition;
            wheelToAxle = new int[wheels.Count];
            var axleNames = new Dictionary<string, int>(StringComparer.Ordinal);
            var counts = new List<int>();

            int wheelTotal = 0;
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelDefinition wheel = wheels[i];
                wheelToAxle[i] = -1;
                if (wheel == null || !wheel.CanDrive || string.IsNullOrEmpty(wheel.AxleId))
                    continue;
                int idx;
                if (!axleNames.TryGetValue(wheel.AxleId, out idx))
                {
                    idx = counts.Count;
                    axleNames.Add(wheel.AxleId, idx);
                    counts.Add(0);
                }
                wheelToAxle[i] = idx;
                counts[idx]++;
                wheelTotal++;
            }

            wheelCountPerAxle = counts.ToArray();
            drivenAxleCount = counts.Count;
            drivenWheelCount = wheelTotal;
            Reset();
        }

        public void Reset()
        {
            EngineRpm = Mathf.Max(0f, config.IdleRpm);
            EngineTorqueNm = 0f;
            TotalWheelTorqueNm = 0f;
            CurrentGear = 0;
        }

        /// <summary>
        /// Evaluate one chassis physics step. Arrays are caller-owned and
        /// reused. Negative RPM feedback is allowed for reversing wheels.
        /// Requested gear: -1 reverse, 0 neutral, 1..N forward.
        /// </summary>
        public void Step(float accelerator01, int requestedGear, float dt,
            float[] wheelAngularVelocityRadPerSecond, float[] outputTorquesNm)
        {
            if (wheelAngularVelocityRadPerSecond == null ||
                wheelAngularVelocityRadPerSecond.Length < wheelToAxle.Length ||
                outputTorquesNm == null || outputTorquesNm.Length < wheelToAxle.Length)
                throw new ArgumentException("Wheel arrays must cover all chassis wheels.");

            Array.Clear(outputTorquesNm, 0, wheelToAxle.Length);
            accelerator01 = Mathf.Clamp01(accelerator01);
            CurrentGear = requestedGear;
            EngineTorqueNm = 0f;
            TotalWheelTorqueNm = 0f;

            float gearRatio = config.GearRatio(requestedGear);
            float reduction = Mathf.Abs(gearRatio) * Mathf.Max(0f, config.FinalDriveRatio);
            float idle = Mathf.Max(100f, config.IdleRpm);
            float maxRpm = Mathf.Max(idle + 1f, config.MaxRpm);

            float avgWheelOmega = 0f;
            if (drivenWheelCount > 0)
            {
                for (int i = 0; i < wheelToAxle.Length; i++)
                    if (wheelToAxle[i] >= 0)
                        avgWheelOmega += Mathf.Abs(wheelAngularVelocityRadPerSecond[i]);
                avgWheelOmega /= drivenWheelCount;
            }

            float wheelCoupledRpm = avgWheelOmega * reduction * (30f / Mathf.PI);
            float targetRpm = Mathf.Max(idle, wheelCoupledRpm);
            if (reduction > 0f)
                targetRpm = Mathf.Max(targetRpm,
                    idle + accelerator01 * (maxRpm - idle) *
                    Mathf.Clamp01(config.LaunchFreeRevFraction));
            targetRpm = Mathf.Clamp(targetRpm, idle, maxRpm);

            // Smooth power-source speed, without allocating or modifying the
            // ScriptableObject. This simplified response is NOT a real clutch.
            EngineRpm = Mathf.MoveTowards(EngineRpm, targetRpm,
                Mathf.Max(0f, config.EngineRpmResponsePerSecond) * Mathf.Max(0f, dt));

            if (gearRatio == 0f || reduction <= 0f || drivenAxleCount == 0 ||
                accelerator01 <= 0f || config.TorqueNmByRpm == null ||
                config.TorqueNmByRpm.length == 0 ||
                wheelCoupledRpm >= maxRpm || EngineRpm >= maxRpm)
                return;

            EngineTorqueNm = Mathf.Max(0f,
                config.TorqueNmByRpm.Evaluate(EngineRpm)) * accelerator01;

            float signedTotal = EngineTorqueNm * gearRatio *
                Mathf.Max(0f, config.FinalDriveRatio) *
                Mathf.Clamp01(config.Efficiency);
            float perAxle = signedTotal / drivenAxleCount;

            float maxWheelOmega = maxRpm / (reduction * (30f / Mathf.PI));
            for (int i = 0; i < wheelToAxle.Length; i++)
            {
                int axle = wheelToAxle[i];
                if (axle < 0 || wheelCountPerAxle[axle] == 0 ||
                    Mathf.Abs(wheelAngularVelocityRadPerSecond[i]) >= maxWheelOmega)
                    continue;
                outputTorquesNm[i] = perAxle / wheelCountPerAxle[axle];
                TotalWheelTorqueNm += outputTorquesNm[i];
            }
        }
    }
}
