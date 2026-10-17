using System;
using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>Manual, or optional threshold-based automatic forward shifts.</summary>
    public enum GearboxControlMode
    {
        Manual = 0,
        Automatic = 1
    }

    /// <summary>
    /// Per-vehicle, allocation-free gearbox state controller. Owns gear
    /// transitions, but does not attempt clutch/driveline shaft constraints.
    /// Negative = reverse, zero = neutral, positive = forward gear number.
    /// </summary>
    public sealed class VehicleGearboxController
    {
        private readonly int forwardGears;
        private readonly float shiftDuration;
        private readonly float minShiftInterval;
        private readonly float upshiftRpm;
        private readonly float downshiftRpm;

        private float shiftProgress;
        private float timeSinceShift;
        public int CurrentGear { get; private set; }
        public int TargetGear { get; private set; }
        public bool IsShifting { get; private set; }
        public float ShiftProgress01 => !IsShifting || shiftDuration <= 0f
            ? 1f : Mathf.Clamp01(shiftProgress / shiftDuration);
        /// <summary>
        /// 0 during shift (torque interrupted), 1 after gear is engaged.
        /// A clutch/differential coupling model would replace this later.
        /// </summary>
        public float TorqueTransfer01 => IsShifting ? 0f : 1f;

        public VehicleGearboxController(SimplePowertrainDefinition config, int startGear = 1)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            forwardGears = Mathf.Max(0, config.ForwardGearCount);
            shiftDuration = Mathf.Max(0f, config.ShiftDurationSeconds);
            minShiftInterval = Mathf.Max(0.01f, config.MinTimeBetweenShiftsSeconds);
            // Older serialized SimplePowertrain assets may not contain
            // these newer automatic-threshold fields. Zero falls back to
            // sensible idle/redline-derived values instead of auto-upshifting
            // at idle RPM. Explicit valid values remain user-controlled.
            float fallbackUpshift = Mathf.Lerp(config.IdleRpm, config.MaxRpm, 0.78f);
            float requestedUpshift = config.AutomaticUpshiftRpm > config.IdleRpm
                ? config.AutomaticUpshiftRpm : Mathf.Max(config.IdleRpm + 300f, fallbackUpshift);
            // Truck examples have a much lower redline than the passenger-
            // car default 4600 RPM threshold. Bound to their actual range
            // so Automatic can change gears instead of never shifting.
            upshiftRpm = Mathf.Clamp(requestedUpshift,
                config.IdleRpm + 100f,
                Mathf.Max(config.IdleRpm + 200f, config.MaxRpm - 100f));
            float fallbackDownshift = Mathf.Lerp(config.IdleRpm, config.MaxRpm, 0.2f);
            float rawDownshift = config.AutomaticDownshiftRpm > config.IdleRpm
                ? config.AutomaticDownshiftRpm : fallbackDownshift;
            downshiftRpm = Mathf.Clamp(rawDownshift,
                Mathf.Max(100f, config.IdleRpm), Mathf.Max(config.IdleRpm, upshiftRpm - 100f));
            Reset(startGear);
        }

        public void Reset(int gear)
        {
            CurrentGear = IsValidGear(gear) ? gear : 0;
            TargetGear = CurrentGear;
            IsShifting = false;
            shiftProgress = 0f;
            timeSinceShift = minShiftInterval;
        }

        public bool IsValidGear(int gear)
        {
            return gear >= -1 && gear <= forwardGears;
        }

        /// <summary>
        /// Host API. Forbid forward/reverse direction changes while the body
        /// still travels rapidly in the opposite longitudinal direction.
        /// Entering neutral remains allowed regardless of vehicle speed.
        /// Does not trigger automatically or infer from a rollover.
        /// </summary>
        public bool TryRequestGear(int newGear, float longitudinalSpeedMps)
        {
            if (!IsValidGear(newGear)) return false;
            if (IsShifting) return newGear == TargetGear;
            if (newGear == CurrentGear) return true;
            const float DirectionChangeMaxSpeedMps = 0.75f;
            if (newGear < 0 && longitudinalSpeedMps > DirectionChangeMaxSpeedMps)
                return false;
            if (newGear > 0 && longitudinalSpeedMps < -DirectionChangeMaxSpeedMps)
                return false;

            TargetGear = newGear;
            shiftProgress = 0f;
            timeSinceShift = 0f;
            if (shiftDuration <= 0f)
            {
                CurrentGear = TargetGear;
                IsShifting = false;
            }
            else
            {
                IsShifting = true;
            }
            return true;
        }

        /// <summary>
        /// Advances the current shift, then may schedule an automatic up/down
        /// forward shift. Uses previous physics-step EngineRpm feedback and
        /// a time cooldown to avoid oscillating between two gears.
        /// </summary>
        public void Step(float dt, float lastEngineRpm, float throttle01,
            float longitudinalSpeedMps, GearboxControlMode controlMode)
        {
            dt = Mathf.Max(0f, dt);
            if (IsShifting)
            {
                shiftProgress += dt;
                if (shiftProgress >= shiftDuration)
                {
                    IsShifting = false;
                    CurrentGear = TargetGear;
                    shiftProgress = shiftDuration;
                    timeSinceShift = 0f;
                }
                return;
            }

            timeSinceShift += dt;
            if (controlMode != GearboxControlMode.Automatic ||
                CurrentGear <= 0 || throttle01 < 0.05f ||
                timeSinceShift < minShiftInterval)
                return;

            if (CurrentGear < forwardGears && lastEngineRpm >= upshiftRpm)
                TryRequestGear(CurrentGear + 1, longitudinalSpeedMps);
            else if (CurrentGear > 1 && lastEngineRpm <= downshiftRpm)
                TryRequestGear(CurrentGear - 1, longitudinalSpeedMps);
        }
    }
}
