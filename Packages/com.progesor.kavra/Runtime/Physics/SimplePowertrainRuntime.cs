using System;
using System.Collections.Generic;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// First reusable powertrain: torque curve -> gear -> final drive ->
    /// weighted driven axle groups -> powered wheel torque commands.
    /// EqualTorque is a command split, not a traction-limited open diff;
    /// SpeedBiasedApproximation is NOT an actual LSD/joint speed constraint.
    /// </summary>
    public sealed class SimplePowertrainRuntime
    {
        private const float SpeedBiasDeadbandRadPerSecond = 3f;

        private readonly SimplePowertrainDefinition config;
        private readonly int[] wheelToAxle;
        private readonly int[] wheelCountPerAxle;
        private readonly float[] axleTorqueWeight;
        private readonly AxleDifferentialMode[] axleMode;
        private readonly float[] axleMaxBiasRatio;
        private readonly float[] axleSpeedBiasGain;
        private readonly float[] axleMinWheelSpeed;
        private readonly float[] axleNormalizedWeightSum;
        private readonly float[] wheelDistributionWeights;
        private readonly int drivenAxleCount;
        private readonly int drivenWheelCount;
        private readonly float totalAxleWeight;

        public float EngineRpm { get; private set; }
        public float EngineTorqueNm { get; private set; }
        public float TotalWheelTorqueNm { get; private set; }
        public int CurrentGear { get; private set; }
        public int DrivenWheelCount => drivenWheelCount;
        public int DrivenAxleCount => drivenAxleCount;

        /// <summary>
        /// Compatibility constructor: without axle configuration all driven
        /// axles have weight 1 and each driven wheel gets equal torque within
        /// its axle, matching the original v0.1.0 behavior.
        /// </summary>
        public SimplePowertrainRuntime(SimplePowertrainDefinition definition,
            IReadOnlyList<WheelDefinition> wheels)
            : this(definition, wheels, null)
        {
        }

        public SimplePowertrainRuntime(SimplePowertrainDefinition definition,
            IReadOnlyList<WheelDefinition> wheels,
            IReadOnlyList<AxleDefinition> axles)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (wheels == null) throw new ArgumentNullException(nameof(wheels));
            config = definition;

            wheelToAxle = new int[wheels.Count];
            wheelDistributionWeights = new float[wheels.Count];

            // Wheel.CanDrive remains the authoritative drive eligibility.
            // AxleDefinition.IsDriven is descriptive metadata in this first
            // powertrain and does not silently override existing wheel data.
            var nameToGroup = new Dictionary<string, int>(StringComparer.Ordinal);
            var axleConfigByName = new Dictionary<string, AxleDefinition>(StringComparer.Ordinal);
            if (axles != null)
            {
                for (int i = 0; i < axles.Count; i++)
                {
                    AxleDefinition axle = axles[i];
                    if (axle != null && !string.IsNullOrEmpty(axle.Id) &&
                        !axleConfigByName.ContainsKey(axle.Id))
                        axleConfigByName.Add(axle.Id, axle);
                }
            }

            var counts = new List<int>();
            var groupNames = new List<string>();
            int poweredCount = 0;
            for (int i = 0; i < wheels.Count; i++)
            {
                wheelToAxle[i] = -1;
                WheelDefinition wheel = wheels[i];
                if (wheel == null || !wheel.CanDrive || string.IsNullOrEmpty(wheel.AxleId))
                    continue;

                int group;
                if (!nameToGroup.TryGetValue(wheel.AxleId, out group))
                {
                    group = counts.Count;
                    nameToGroup.Add(wheel.AxleId, group);
                    groupNames.Add(wheel.AxleId);
                    counts.Add(0);
                }
                wheelToAxle[i] = group;
                counts[group]++;
                poweredCount++;
            }

            drivenAxleCount = counts.Count;
            drivenWheelCount = poweredCount;
            wheelCountPerAxle = counts.ToArray();
            axleTorqueWeight = new float[drivenAxleCount];
            axleMode = new AxleDifferentialMode[drivenAxleCount];
            axleMaxBiasRatio = new float[drivenAxleCount];
            axleSpeedBiasGain = new float[drivenAxleCount];
            axleMinWheelSpeed = new float[drivenAxleCount];
            axleNormalizedWeightSum = new float[drivenAxleCount];

            float weightSum = 0f;
            for (int a = 0; a < drivenAxleCount; a++)
            {
                AxleDefinition axle;
                bool configured = axleConfigByName.TryGetValue(groupNames[a], out axle);
                // Zero means "legacy missing serialized field" and therefore
                // defaults to 1. Axles are disabled via Wheel.CanDrive instead.
                float weight = configured && axle.DriveTorqueWeight > 0f
                    ? axle.DriveTorqueWeight : 1f;
                axleTorqueWeight[a] = weight;
                axleMode[a] = configured
                    ? axle.DifferentialMode : AxleDifferentialMode.EqualTorque;
                axleMaxBiasRatio[a] = configured
                    ? Mathf.Max(1f, axle.MaximumSpeedBiasRatio) : 1f;
                axleSpeedBiasGain[a] = configured
                    ? Mathf.Max(0f, axle.SpeedBiasGainPerRadPerSec) : 0f;
                weightSum += weight;
            }
            totalAxleWeight = Mathf.Max(0f, weightSum);
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
        /// One chassis FixedUpdate. Arrays owned by the caller. Negative gear
        /// selects reverse. Invalid gears and neutral output zero torque.
        /// No per-step allocations, and no Rigidbody references.
        /// </summary>
        public void Step(float accelerator01, int requestedGear, float dt,
            float[] wheelAngularVelocityRadPerSecond, float[] outputTorquesNm)
        {
            if (wheelAngularVelocityRadPerSecond == null ||
                wheelAngularVelocityRadPerSecond.Length < wheelToAxle.Length ||
                outputTorquesNm == null || outputTorquesNm.Length < wheelToAxle.Length)
                throw new ArgumentException("Wheel arrays must cover all chassis wheels.");

            Array.Clear(outputTorquesNm, 0, wheelToAxle.Length);
            EngineTorqueNm = 0f;
            TotalWheelTorqueNm = 0f;
            CurrentGear = requestedGear;
            accelerator01 = Mathf.Clamp01(accelerator01);

            float gear = config.GearRatio(requestedGear);
            float reduction = Mathf.Abs(gear) * Mathf.Max(0f, config.FinalDriveRatio);
            float idle = Mathf.Max(100f, config.IdleRpm);
            float redline = Mathf.Max(idle + 1f, config.MaxRpm);
            float averageOmega = 0f;

            if (drivenWheelCount > 0)
            {
                for (int i = 0; i < wheelToAxle.Length; i++)
                    if (wheelToAxle[i] >= 0)
                        averageOmega += Mathf.Abs(wheelAngularVelocityRadPerSecond[i]);
                averageOmega /= drivenWheelCount;
            }

            float coupledRpm = averageOmega * reduction * (30f / Mathf.PI);
            float target = Mathf.Max(idle, coupledRpm);
            if (reduction > 0f)
                target = Mathf.Max(target, idle + accelerator01 * (redline - idle) *
                    Mathf.Clamp01(config.LaunchFreeRevFraction));
            target = Mathf.Clamp(target, idle, redline);
            EngineRpm = Mathf.MoveTowards(EngineRpm, target,
                Mathf.Max(0f, config.EngineRpmResponsePerSecond) * Mathf.Max(0f, dt));

            if (gear == 0f || reduction <= 0f || drivenAxleCount == 0 ||
                totalAxleWeight <= 0f || accelerator01 <= 0f ||
                config.TorqueNmByRpm == null || config.TorqueNmByRpm.length == 0 ||
                coupledRpm >= redline || EngineRpm >= redline)
                return;

            EngineTorqueNm = Mathf.Max(0f, config.TorqueNmByRpm.Evaluate(EngineRpm)) *
                             accelerator01;
            float signedOutput = EngineTorqueNm * gear *
                Mathf.Max(0f, config.FinalDriveRatio) *
                Mathf.Clamp01(config.Efficiency);

            // Stage 1: calculate slowest wheel magnitude per driven axle.
            // That is an input to a mild speed-based LSD-like torque
            // COMMAND bias, never a mechanical wheel-speed constraint.
            for (int a = 0; a < drivenAxleCount; a++)
            {
                axleMinWheelSpeed[a] = float.PositiveInfinity;
                axleNormalizedWeightSum[a] = 0f;
            }
            for (int i = 0; i < wheelToAxle.Length; i++)
            {
                int axle = wheelToAxle[i];
                if (axle >= 0)
                    axleMinWheelSpeed[axle] = Mathf.Min(axleMinWheelSpeed[axle],
                        Mathf.Abs(wheelAngularVelocityRadPerSecond[i]));
            }

            // Stage 2: each axle divides its share between driven wheels.
            for (int i = 0; i < wheelToAxle.Length; i++)
            {
                int axle = wheelToAxle[i];
                if (axle < 0) continue;

                float relativeWeight = 1f;
                if (axleMode[axle] == AxleDifferentialMode.SpeedBiasedApproximation)
                {
                    float excessOmega = Mathf.Max(0f,
                        Mathf.Abs(wheelAngularVelocityRadPerSecond[i]) -
                        axleMinWheelSpeed[axle] - SpeedBiasDeadbandRadPerSecond);
                    float rawWeight = 1f /
                        (1f + excessOmega * axleSpeedBiasGain[axle]);
                    relativeWeight = Mathf.Max(
                        1f / axleMaxBiasRatio[axle], rawWeight);
                }
                wheelDistributionWeights[i] = relativeWeight;
                axleNormalizedWeightSum[axle] += relativeWeight;
            }

            float limitWheelOmega = redline / (reduction * (30f / Mathf.PI));
            for (int i = 0; i < wheelToAxle.Length; i++)
            {
                int axle = wheelToAxle[i];
                if (axle < 0 || wheelCountPerAxle[axle] == 0 ||
                    axleNormalizedWeightSum[axle] <= 0f ||
                    Mathf.Abs(wheelAngularVelocityRadPerSecond[i]) >= limitWheelOmega)
                    continue;

                float axleShare = signedOutput * (axleTorqueWeight[axle] / totalAxleWeight);
                float wheelShare = wheelDistributionWeights[i] /
                    axleNormalizedWeightSum[axle];
                outputTorquesNm[i] = axleShare * wheelShare;
                TotalWheelTorqueNm += outputTorquesNm[i];
            }
        }
    }
}
