using UnityEngine;

/// <summary>
/// Host-only chase camera with optional subtle speed-dependent FOV.
/// Disabled by default, so previously committed M0 scenes remain identical.
/// </summary>
public sealed class VehicleLabChaseCamera : MonoBehaviour
{
    public Transform Target;
    public Vector3 LocalOffset = new Vector3(0f, 5f, -10f);
    [Min(0f)] public float FollowSharpness = 5f;

    [Header("PhysicsLab speed reference (opt-in)")]
    public bool SpeedReactiveFov;
    [Range(0f, 14f)] public float MaximumAdditionalFovDegrees = 6f;
    [Min(1f)] public float SpeedForMaximumFovKmh = 90f;
    [Min(0.01f)] public float FovSmoothness = 3f;

    private Camera cameraComponent;
    private Rigidbody targetRigidbody;
    private float baseFov;

    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        baseFov = cameraComponent != null ? cameraComponent.fieldOfView : 60f;
        targetRigidbody = Target != null ? Target.GetComponent<Rigidbody>() : null;
    }

    private void LateUpdate()
    {
        if (Target == null) return;
        Quaternion yaw = Quaternion.Euler(0f, Target.eulerAngles.y, 0f);
        Vector3 destination = Target.position + yaw * LocalOffset;
        float alpha = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, destination, alpha);
        transform.LookAt(Target.position + Vector3.up * 0.6f, Vector3.up);

        if (!SpeedReactiveFov || cameraComponent == null) return;
        if (targetRigidbody == null)
            targetRigidbody = Target.GetComponent<Rigidbody>();
        float kmh = targetRigidbody != null
            ? targetRigidbody.linearVelocity.magnitude * 3.6f : 0f;
        float desired = baseFov + MaximumAdditionalFovDegrees *
            Mathf.Clamp01(kmh / Mathf.Max(1f, SpeedForMaximumFovKmh));
        float smooth = 1f - Mathf.Exp(-FovSmoothness * Time.deltaTime);
        cameraComponent.fieldOfView = Mathf.Lerp(
            cameraComponent.fieldOfView, desired, smooth);
    }
}
