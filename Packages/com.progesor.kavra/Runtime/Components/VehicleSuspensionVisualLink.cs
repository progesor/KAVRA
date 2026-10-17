using UnityEngine;

namespace Progesor.Kavra
{
    /// <summary>
    /// Optional kinematic visual connecting two scene pivots. A cylinder
    /// aligned with local +Y is scaled along length. It NEVER changes the
    /// physical suspension forces/rigidbody or creates joints.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VehicleSuspensionVisualLink : MonoBehaviour
    {
        public Transform UpperAttachment;
        public Transform LowerAttachment;
        public Transform LinkMesh;

        [Min(0.001f)]
        [Tooltip("Actual cylinder length represented by LinkMesh at its base scale.")]
        public float ReferenceLengthMeters = 1f;

        // Persist the original scale in the prefab. Sample geometry is
        // fitted once in the Editor; a non-serialized cached scale would
        // incorrectly shrink the rod again when reloaded in Play Mode.
        [SerializeField, HideInInspector] private Vector3 initialScale = Vector3.one;
        [SerializeField, HideInInspector] private bool captured;

        public bool CaptureInitialScale()
        {
            if (LinkMesh == null || UpperAttachment == null || LowerAttachment == null)
                return false;
            initialScale = LinkMesh.localScale;
            captured = true;
            return true;
        }

        private void LateUpdate()
        {
            UpdateLink();
        }

        public bool UpdateLink()
        {
            if (!captured && !CaptureInitialScale()) return false;
            if (LinkMesh.parent == null) return false;

            Transform parent = LinkMesh.parent;
            Vector3 a = parent.InverseTransformPoint(UpperAttachment.position);
            Vector3 b = parent.InverseTransformPoint(LowerAttachment.position);
            Vector3 delta = b - a;
            float length = delta.magnitude;

            LinkMesh.localPosition = (a + b) * 0.5f;
            if (length > 0.00001f)
                LinkMesh.localRotation = Quaternion.FromToRotation(Vector3.up, delta);
            float factor = length / Mathf.Max(0.001f, ReferenceLengthMeters);
            LinkMesh.localScale = new Vector3(initialScale.x,
                initialScale.y * factor, initialScale.z);
            return true;
        }
    }
}
