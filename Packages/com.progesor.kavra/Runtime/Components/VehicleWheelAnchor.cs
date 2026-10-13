using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>A mount/reference, NOT a functioning WheelCollider.</summary>
    public sealed class VehicleWheelAnchor : MonoBehaviour
    {
        public string WheelId;
        public string AxleId;
        [Min(0.01f)] public float RadiusMeters = 0.4f;
        [Min(0.01f)] public float WidthMeters = 0.25f;
        [Min(0f)] public float SuspensionTravelMeters = 0.2f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, RadiusMeters);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * SuspensionTravelMeters);
        }
    }
}
