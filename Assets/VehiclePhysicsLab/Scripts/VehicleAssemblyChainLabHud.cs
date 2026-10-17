using Progesor.Kavra;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
    /// Source-only sample visualization for an arbitrary ordered N-body chain.
    /// K applies a REAL impulse to the selected downstream Rigidbody. Joint
    /// break events remain Unity physics events, not manual script calls.
    /// </summary>
public sealed class VehicleAssemblyChainLabHud : MonoBehaviour
{
    public VehicleAssemblyChainRoot Chain;

    [Range(0.1f, 10f)]
    public float StressImpulseMetersPerSecond = 4f;

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.kKey.wasPressedThisFrame ||
            Chain == null || Chain.Vehicles == null || Chain.LinkCount == 0)
            return;
        int link = Chain.SelectedLinkIndex;
        if (!Chain.IsLinkAttached(link) || link + 1 >= Chain.Vehicles.Length)
            return;

        Rigidbody victim = Chain.Vehicles[link + 1].GetComponent<Rigidbody>();
        if (victim == null) return;
        Vector3 impulse = victim.transform.right *
            (victim.mass * StressImpulseMetersPerSecond);
        Vector3 point = victim.worldCenterOfMass +
                        victim.transform.forward * 1.4f;
        victim.AddForceAtPosition(impulse, point, ForceMode.Impulse);
        Debug.Log("KAVRA M1.5: physical load applied to link " + link +
                  ". PhysX decides if the hitch actually breaks.", this);
#endif
    }

    private void OnGUI()
    {
        if (Chain == null || Chain.Vehicles == null ||
            Chain.Couplings == null) return;
        int connected = Chain.ConnectedLeadVehicleCount();
        string message = "KAVRA M1.5 — MULTI-TRAILER CHAIN" +
                         "\nBodies: " + Chain.Vehicles.Length +
                         " | Lead-connected: " + connected +
                         " | Links: " + Chain.LinkCount +
                         "\nSelected link: " + Chain.SelectedLinkIndex;

        for (int i = 0; i < Chain.LinkCount; i++)
        {
            VehicleCoupling coupling = Chain.Couplings[i];
            if (coupling == null) continue;
            message += "\n  " + i + ": " + coupling.State +
                       " (breaks " + coupling.PhysicalBreakCount + ")";
        }

        message += "\nJ/L select link | H attach/detach | K physics stress" +
                   "\nR recovers lead-connected group ONLY (manual)";

        float h = Mathf.Min(440f, 130f + 19f * Chain.LinkCount);
        GUI.Box(new Rect(Mathf.Max(10f, Screen.width - 520f), 12f, 510f, h),
            message);
    }
}
