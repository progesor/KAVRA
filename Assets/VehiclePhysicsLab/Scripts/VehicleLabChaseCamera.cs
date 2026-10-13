using UnityEngine;

/// <summary>Simple host-only camera. No Cinemachine dependency in KAVRA core.</summary>
public sealed class VehicleLabChaseCamera : MonoBehaviour
{
    public Transform Target;
    public Vector3 LocalOffset = new Vector3(0f, 5f, -10f);
    [Min(0f)] public float FollowSharpness = 5f;

    private void LateUpdate()
    {
        if (Target == null) return;
        Quaternion yaw = Quaternion.Euler(0f, Target.eulerAngles.y, 0f);
        Vector3 destination = Target.position + yaw * LocalOffset;
        float alpha = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, destination, alpha);
        transform.LookAt(Target.position + Vector3.up * 0.6f, Vector3.up);
    }
}
