using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// M1 starting point: a single power source, fixed-ratio gearbox and
    /// final drive. Torque curve is Nm as a function of engine RPM.
    /// Real clutch slip, converters, differentials and hybrids come later.
    /// </summary>
    [CreateAssetMenu(fileName = "KAVRA_SimplePowertrain",
        menuName = "KAVRA/Simple Powertrain")]
    public sealed class SimplePowertrainDefinition : ScriptableObject
    {
        [Header("Engine (simplified)")]
        [Min(100f)] public float IdleRpm = 850f;
        [Min(300f)] public float MaxRpm = 6000f;
        [Min(0.1f)] public float EngineRpmResponsePerSecond = 2200f;
        [Range(0f, 1f)] public float LaunchFreeRevFraction = 0.3f;
        public AnimationCurve TorqueNmByRpm = new AnimationCurve(
            new Keyframe(800f, 150f),
            new Keyframe(1600f, 205f),
            new Keyframe(3700f, 240f),
            new Keyframe(5900f, 160f));

        [Header("Gearbox and final drive")]
        [Tooltip("Forward gears, 1-based: 1 selects ForwardGearRatios[0].")]
        public float[] ForwardGearRatios = { 3.5f, 2.2f, 1.45f, 1f, 0.82f };
        [Min(0.01f)] public float ReverseGearRatio = 3.3f;
        [Min(0.01f)] public float FinalDriveRatio = 3.9f;
        [Range(0f, 1f)] public float Efficiency = 0.87f;

        public int ForwardGearCount => ForwardGearRatios != null
            ? ForwardGearRatios.Length : 0;

        public float GearRatio(int gear)
        {
            if (gear == -1) return -Mathf.Max(0f, ReverseGearRatio);
            if (gear < 1 || ForwardGearRatios == null ||
                gear > ForwardGearRatios.Length) return 0f;
            return Mathf.Max(0f, ForwardGearRatios[gear - 1]);
        }
    }
}
