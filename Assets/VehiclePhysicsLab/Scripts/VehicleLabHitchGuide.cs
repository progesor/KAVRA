using Progesor.Kavra;
using UnityEngine;

/// <summary>
/// LAB-ONLY selected-hitch overlay: colored anchor spheres and a line
/// connecting actual hitch world points. Physics code is NOT modified by
/// this component. No automatic attachment, braking or movement.
/// Works for either 2-body M1.3 or ordered N-body M1.5 assemblies.
/// </summary>
[DisallowMultipleComponent]
public sealed class VehicleLabHitchGuide : MonoBehaviour
{
    public VehicleAssemblyChainRoot Chain;
    public VehicleAssemblyRoot Assembly;
    public Transform[] TractorMarkers;
    public Transform[] TrailerMarkers;
    public LineRenderer[] Lines;
    public LineRenderer[] CaptureRings;

    private VehicleHitchAssessment[] lastAssessments;
    private float nextAssessmentTime;
    private MaterialPropertyBlock colorBlock;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColor = Shader.PropertyToID("_Color");

    public int LinkCount => TractorMarkers == null || TrailerMarkers == null
        ? 0 : Mathf.Min(TractorMarkers.Length, TrailerMarkers.Length);
    public int SelectedLinkIndex => Chain != null
        ? Chain.SelectedLinkIndex : 0;

    public void Setup(VehicleAssemblyChainRoot chain, VehicleAssemblyRoot assembly,
        Transform[] tractorMarkers, Transform[] trailerMarkers, Material guideMaterial)
    {
        Chain = chain;
        Assembly = assembly;
        TractorMarkers = tractorMarkers;
        TrailerMarkers = trailerMarkers;

        // Setup() also runs from Editor scene builders, where Awake() is
        // NOT guaranteed to have run. Initialize the material block BEFORE
        // the first StyleMarker -> TintRenderer call below.
        EnsureArrays();

        Lines = new LineRenderer[LinkCount];
        CaptureRings = new LineRenderer[LinkCount];
        for (int i = 0; i < LinkCount; i++)
        {
            GameObject lineObject = new GameObject("KAVRA_HitchGuideLine_" + i);
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.numCapVertices = 4;
            line.sharedMaterial = guideMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Lines[i] = line;

            GameObject ringObject = new GameObject("KAVRA_HitchCaptureRing_" + i);
            ringObject.transform.SetParent(transform, false);
            LineRenderer ring = ringObject.AddComponent<LineRenderer>();
            ring.positionCount = 40;
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.startWidth = 0.035f;
            ring.endWidth = 0.035f;
            ring.sharedMaterial = guideMaterial;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            CaptureRings[i] = ring;

            StyleMarker(TractorMarkers[i], new Color(0.05f, 0.86f, 1f), 0.24f);
            StyleMarker(TrailerMarkers[i], new Color(1f, 0.82f, 0.18f), 0.24f);
        }

        EnsureArrays();
        RefreshGuides();
    }

    private void Awake()
    {
        EnsureArrays();
    }

    private void EnsureArrays()
    {
        if (lastAssessments == null || lastAssessments.Length != LinkCount)
            lastAssessments = new VehicleHitchAssessment[LinkCount];
        if (colorBlock == null)
            colorBlock = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        RefreshGuides();
    }

    private void RefreshGuides()
    {
        if (LinkCount == 0 || Lines == null) return;
        EnsureArrays();

        int selected = SelectedLinkIndex;
        bool refreshSafety = Time.unscaledTime >= nextAssessmentTime;
        if (refreshSafety) nextAssessmentTime = Time.unscaledTime + 0.10f;

        for (int i = 0; i < LinkCount; i++)
        {
            Transform markerA = TractorMarkers[i];
            Transform markerB = TrailerMarkers[i];
            if (markerA == null || markerB == null) continue;

            float gap = Vector3.Distance(markerA.position, markerB.position);
            VehicleCoupling hitch = GetCoupling(i);
            bool attached = hitch != null && hitch.IsAttached;

            if (refreshSafety && !attached)
            {
                Rigidbody tractorBody = GetTractorBody(i);
                VehicleAssemblyDefinition definition = GetDefinition(i);
                if (hitch != null && tractorBody != null && definition != null)
                    hitch.AssessAttach(tractorBody, definition, out lastAssessments[i]);
            }

            VehicleHitchAssessment assessment = lastAssessments[i];
            bool canCapture = !attached &&
                              assessment.Result == VehicleCouplingResult.Connected;
            VehicleAssemblyDefinition config = GetDefinition(i);
            bool closeEnough = config != null &&
                               gap <= config.MaximumAttachErrorMeters * 2.0f;

            Color tint = attached
                ? new Color(0.04f, 0.86f, 1f)
                : canCapture
                    ? new Color(0.18f, 0.95f, 0.40f)
                    : closeEnough
                        ? new Color(1f, 0.76f, 0.18f)
                        : new Color(1f, 0.24f, 0.22f);
            bool active = i == selected;

            // Each end remains visible even if connected and nearly
            // coincident. The selected pair is significantly larger.
            StyleMarker(markerA, tint, active ? 0.36f : 0.22f);
            StyleMarker(markerB, tint, active ? 0.30f : 0.20f);

            if (CaptureRings != null && i < CaptureRings.Length &&
                CaptureRings[i] != null)
            {
                LineRenderer capture = CaptureRings[i];
                capture.enabled = active && !attached && config != null;
                if (capture.enabled)
                {
                    float radius = config.MaximumAttachErrorMeters;
                    Vector3 center = markerA.position;
                    center.y += 0.03f;
                    for (int point = 0; point < capture.positionCount; point++)
                    {
                        float angle = (point * 2f * Mathf.PI) / capture.positionCount;
                        capture.SetPosition(point, center +
                            new Vector3(Mathf.Cos(angle) * radius, 0f,
                                        Mathf.Sin(angle) * radius));
                    }
                    capture.startColor = tint;
                    capture.endColor = tint;
                    TintRenderer(capture, tint);
                }
            }

            if (i >= Lines.Length || Lines[i] == null) continue;
            LineRenderer line = Lines[i];
            // Hide only very long broken-link lines that would otherwise
            // dominate the scene; world markers and HUD still give cues.
            line.enabled = active ? gap <= 12f : gap <= 2.5f;
            if (!line.enabled) continue;
            line.SetPosition(0, markerA.position);
            line.SetPosition(1, markerB.position);
            line.startColor = tint;
            line.endColor = tint;
            TintRenderer(line, tint);
            line.startWidth = active ? 0.11f : 0.045f;
            line.endWidth = active ? 0.11f : 0.045f;
        }
    }

    public string SelectedStatus()
    {
        int i = SelectedLinkIndex;
        if (i < 0 || i >= LinkCount) return "No hitch selected";
        VehicleCoupling coupling = GetCoupling(i);
        if (coupling == null) return "No hitch component";
        if (coupling.IsAttached) return "CONNECTED — H disconnect at low speed";
        VehicleHitchAssessment data =
            lastAssessments != null && i < lastAssessments.Length
                ? lastAssessments[i] : default(VehicleHitchAssessment);

        switch (data.Result)
        {
            case VehicleCouplingResult.Connected:
                return "READY — press H to connect";
            case VehicleCouplingResult.AttachmentPointsSeparated:
                return "TOO FAR — align colored markers";
            case VehicleCouplingResult.AttachmentYawMisaligned:
                return "BAD ANGLE — straighten trailer";
            case VehicleCouplingResult.RelativeSpeedTooHigh:
            case VehicleCouplingResult.VehicleSpeedTooHigh:
                return "TOO FAST — reduce speed";
            case VehicleCouplingResult.JointRemovalPending:
                return "WAIT — joint removing";
            default:
                return "NOT READY — " + data.Result;
        }
    }

    public float SelectedHitchGapMeters()
    {
        int i = SelectedLinkIndex;
        if (i < 0 || i >= LinkCount || TractorMarkers[i] == null ||
            TrailerMarkers[i] == null)
            return 0f;
        return Vector3.Distance(TractorMarkers[i].position,
            TrailerMarkers[i].position);
    }

    public VehicleHitchAssessment SelectedAssessment()
    {
        int i = SelectedLinkIndex;
        return lastAssessments != null && i >= 0 && i < lastAssessments.Length
            ? lastAssessments[i] : default(VehicleHitchAssessment);
    }

    private VehicleAssemblyDefinition GetDefinition(int i)
    {
        if (Chain != null && Chain.Definition != null &&
            Chain.Definition.Links != null &&
            i >= 0 && i < Chain.Definition.Links.Count)
            return Chain.Definition.Links[i];
        return Assembly != null ? Assembly.Definition : null;
    }

    private VehicleCoupling GetCoupling(int i)
    {
        if (Chain != null)
            return Chain.Couplings != null && i >= 0 && i < Chain.Couplings.Length
                ? Chain.Couplings[i] : null;
        return Assembly != null ? Assembly.Coupling : null;
    }

    private Rigidbody GetTractorBody(int i)
    {
        if (Chain != null)
            return Chain.Vehicles != null && i >= 0 && i < Chain.Vehicles.Length &&
                   Chain.Vehicles[i] != null
                ? Chain.Vehicles[i].GetComponent<Rigidbody>() : null;
        return Assembly != null && Assembly.Tractor != null
            ? Assembly.Tractor.GetComponent<Rigidbody>() : null;
    }

    private void StyleMarker(Transform marker, Color tint, float size)
    {
        if (marker == null) return;
        marker.localScale = Vector3.one * size;
        TintRenderer(marker.GetComponent<Renderer>(), tint);
    }

    private void TintRenderer(Renderer renderer, Color tint)
    {
        if (renderer == null) return;
        // Defense in depth for Editor-created components and domain reloads.
        if (colorBlock == null)
            colorBlock = new MaterialPropertyBlock();
        colorBlock.Clear();
        colorBlock.SetColor(BaseColor, tint);
        colorBlock.SetColor(LegacyColor, tint);
        renderer.SetPropertyBlock(colorBlock);
    }
}
