using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Suspension mount (not the final wheel center). The visible wheel is
    /// displaced downward by 0..SuspensionTravelMeters while playing.
    /// </summary>
    public sealed class VehicleWheelAnchor : MonoBehaviour
    {
        public string WheelId;
        public string AxleId;
        [Min(0.01f)] public float RadiusMeters = 0.4f;
        [Min(0.01f)] public float WidthMeters = 0.25f;
        [Min(0f)] public float SuspensionTravelMeters = 0.2f;
        public Transform VisualTransform;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 freeWheelCenter = transform.position - transform.up * SuspensionTravelMeters;
            Gizmos.DrawWireSphere(freeWheelCenter, RadiusMeters);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, freeWheelCenter);
        }
    }
}
