using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Visual-only wheel rig, independent from wheel contact and powertrain.
    /// Child hierarchy convention:
    /// WheelAnchor -> TravelPivot -> SteeringPivot -> SpinPivot -> wheel mesh.
    /// Imported model meshes can replace the placeholder without touching
    /// Rigidbody/VehicleDefinition physics. No wheel collider or joint here.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleWheelAnchor))]
    public sealed class VehicleWheelVisualRig : MonoBehaviour
    {
        public Transform TravelPivot;
        public Transform SteeringPivot;
        public Transform SpinPivot;

        [Tooltip("Local rotation axis of SteeringPivot.")]
        public Vector3 SteeringAxisLocal = Vector3.up;
        [Tooltip("Local rotation axis of SpinPivot (not the mesh's imported +Y).")]
        public Vector3 SpinAxisLocal = Vector3.right;

        private Vector3 baseTravelPosition;
        private Quaternion baseSteeringRotation;
        private Quaternion baseSpinRotation;
        private bool captured;

        /// <summary>Call once after hierarchy/pivot setup, before first pose.</summary>
        public bool CaptureInitialPose()
        {
            if (TravelPivot == null || SteeringPivot == null || SpinPivot == null ||
                SteeringAxisLocal.sqrMagnitude <= 0.000001f ||
                SpinAxisLocal.sqrMagnitude <= 0.000001f)
                return false;

            baseTravelPosition = TravelPivot.localPosition;
            baseSteeringRotation = SteeringPivot.localRotation;
            baseSpinRotation = SpinPivot.localRotation;
            captured = true;
            return true;
        }

        public bool ApplyPose(float wheelDropMeters, float steerDegrees, float spinDegrees)
        {
            if (!captured && !CaptureInitialPose()) return false;

            // Local mounting -Y is positive extension from anchor mount.
            TravelPivot.localPosition = baseTravelPosition +
                                        Vector3.down * wheelDropMeters;
            SteeringPivot.localRotation = baseSteeringRotation *
                Quaternion.AngleAxis(steerDegrees, SteeringAxisLocal.normalized);
            SpinPivot.localRotation = baseSpinRotation *
                Quaternion.AngleAxis(spinDegrees, SpinAxisLocal.normalized);
            return true;
        }
    }
}
