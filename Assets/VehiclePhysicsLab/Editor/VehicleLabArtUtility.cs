using Progesor.Kavra;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Editor-only PhysicsLab readability pass. Generates real, reusable native
/// .asset/.mat files under Assets/VehiclePhysicsLab/Generated, never modifies
/// the KAVRA UPM package or any pre-existing user-created material.
/// Works with URP, HDRP or the built-in rendering pipeline.
/// </summary>
public static class VehicleLabArtUtility
{
    private const string Folder = "Assets/VehiclePhysicsLab/Generated";
    private const string CheckerPath = Folder + "/KAVRA_Checker_128.asset";
    private const float CheckerSquareMeters = 3f;

    public static void DecorateChain(VehicleAssemblyChainRoot chain, GameObject road)
    {
        if (chain == null || chain.Vehicles == null ||
            chain.Definition == null || chain.Couplings == null)
            return;

        StyleRoad(road);
        StyleVehicles(chain.Vehicles);
        var guide = chain.GetComponent<VehicleLabHitchGuide>();
        if (guide == null)
            guide = chain.gameObject.AddComponent<VehicleLabHitchGuide>();

        var fronts = new Transform[chain.LinkCount];
        var rears = new Transform[chain.LinkCount];
        for (int i = 0; i < chain.LinkCount; i++)
        {
            fronts[i] = chain.Vehicles[i].transform.Find("HitchRear_To_" + (i + 1));
            rears[i] = chain.Vehicles[i + 1].transform.Find("HitchFront_From_" + i);
        }
        PaintHitchMarkers(fronts, rears);
        guide.Setup(chain, null, fronts, rears, GetGuideMaterial());
        AssetDatabase.SaveAssets();
    }

    public static void DecorateTwoBody(VehicleAssemblyRoot assembly, GameObject road)
    {
        if (assembly == null || assembly.Tractor == null || assembly.Trailer == null)
            return;

        StyleRoad(road);
        StyleVehicle(assembly.Tractor.gameObject, 0);
        StyleVehicle(assembly.Trailer.gameObject, 1);
        var guide = assembly.GetComponent<VehicleLabHitchGuide>();
        if (guide == null)
            guide = assembly.gameObject.AddComponent<VehicleLabHitchGuide>();

        var fronts = new[] { assembly.Tractor.transform.Find("TractorHitchMarker") };
        var rears = new[] { assembly.Trailer.transform.Find("TrailerCouplingMarker") };
        PaintHitchMarkers(fronts, rears);
        guide.Setup(null, assembly, fronts, rears, GetGuideMaterial());
        AssetDatabase.SaveAssets();
    }

    private static void PaintHitchMarkers(
        Transform[] tractorMarkers, Transform[] trailerMarkers)
    {
        Material tractorColor = GetColorMaterial("HitchTow_ElectricCyan",
            new Color(0.05f, 0.90f, 1f));
        Material trailerColor = GetColorMaterial("HitchTrailer_WarmYellow",
            new Color(1f, 0.83f, 0.12f));
        for (int i = 0; i < tractorMarkers.Length; i++)
        {
            if (tractorMarkers[i] != null)
            {
                Renderer renderer = tractorMarkers[i].GetComponent<Renderer>();
                if (renderer != null && tractorColor != null)
                    renderer.sharedMaterial = tractorColor;
                tractorMarkers[i].localScale = Vector3.one * 0.26f;
            }
            if (trailerMarkers[i] != null)
            {
                Renderer renderer = trailerMarkers[i].GetComponent<Renderer>();
                if (renderer != null && trailerColor != null)
                    renderer.sharedMaterial = trailerColor;
                trailerMarkers[i].localScale = Vector3.one * 0.26f;
            }
        }
    }

    private static void StyleVehicles(VehicleRoot[] vehicles)
    {
        for (int i = 0; i < vehicles.Length; i++)
            if (vehicles[i] != null)
                StyleVehicle(vehicles[i].gameObject, i);
    }

    private static void StyleVehicle(GameObject vehicle, int index)
    {
        // The chassis collider is physically unchanged: color is purely
        // Renderer.sharedMaterial, no new colliders, movement or rigidbodies.
        Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
        Material painted = index == 0
            ? GetColorMaterial("Tractor_MatteBlue", new Color(0.15f, 0.33f, 0.64f))
            : index == 1
                ? GetColorMaterial("TrailerA_BurntOrange", new Color(0.86f, 0.38f, 0.10f))
                : GetColorMaterial("TrailerB_SignalRed", new Color(0.78f, 0.16f, 0.18f));
        Material tire = GetColorMaterial("Tire_Graphite", new Color(0.10f, 0.12f, 0.15f));
        Material hardware = GetColorMaterial("Suspension_Titanium", new Color(0.62f, 0.66f, 0.70f));

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            string objectName = renderer.gameObject.name;
            if (objectName == "__CHASSIS_TEST_COLLIDER")
                renderer.sharedMaterial = painted;
            else if (objectName == "__WHEEL_PREVIEW")
                renderer.sharedMaterial = tire;
            else if (objectName == "__SHOCK_PREVIEW")
                renderer.sharedMaterial = hardware;
        }
    }

    public static void StyleRoad(GameObject road)
    {
        if (road == null) return;
        Renderer renderer = road.GetComponent<Renderer>();
        if (renderer == null) return;

        EnsureFolder();
        Texture2D texture = CheckerTexture();
        if (texture == null) return;

        float width = Mathf.Abs(road.transform.localScale.x);
        float length = Mathf.Abs(road.transform.localScale.z);
        string matPath = Folder + "/KAVRA_CheckerRoad_" +
            Mathf.RoundToInt(width) + "x" + Mathf.RoundToInt(length) + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(matPath) != null)
            {
                Debug.LogError("KAVRA: road material path occupied; skipping visual style: " + matPath);
                return;
            }

            material = new Material(FindSurfaceShader());
            material.name = "KAVRA Lab Checker Asphalt";
            Color white = Color.white;
            SetColor(material, white);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap",
                    new Vector2(width / (8f * CheckerSquareMeters),
                                length / (8f * CheckerSquareMeters)));
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex",
                    new Vector2(width / (8f * CheckerSquareMeters),
                                length / (8f * CheckerSquareMeters)));
            }
            AssetDatabase.CreateAsset(material, matPath);
        }
        renderer.sharedMaterial = material;

        // Regular stationary roadside landmarks make displacement and
        // sideways slip easier to see, without adding physics collisions.
        AddDistanceReferenceMarks(road);
    }

    private static Texture2D CheckerTexture()
    {
        Texture2D old = AssetDatabase.LoadAssetAtPath<Texture2D>(CheckerPath);
        if (old != null) return old;
        if (AssetDatabase.LoadMainAssetAtPath(CheckerPath) != null)
        {
            Debug.LogError("KAVRA: incompatible native texture asset at " + CheckerPath);
            return null;
        }
        const int size = 128;
        const int squares = 8;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "KAVRA physics reference checker (8x8)";
        Color dark = new Color(0.21f, 0.25f, 0.31f);
        Color light = new Color(0.48f, 0.52f, 0.59f);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = ((x / (size / squares) +
                                        y / (size / squares)) % 2 == 0)
                    ? dark : light;
        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 4;
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, CheckerPath);
        return texture;
    }

    private static void AddDistanceReferenceMarks(GameObject road)
    {
        // Geometry is under a generated child, so rerunning style on the
        // same scene cannot duplicate landmarks.
        if (road.transform.Find("KAVRA_Speed_Reference_Marks") != null)
            return;
        Transform container = new GameObject("KAVRA_Speed_Reference_Marks").transform;
        container.SetParent(road.transform, false);
        container.localPosition = Vector3.zero;
        container.localRotation = Quaternion.identity;
        container.localScale = new Vector3(
            1f / Mathf.Max(0.01f, Mathf.Abs(road.transform.localScale.x)),
            1f / Mathf.Max(0.01f, Mathf.Abs(road.transform.localScale.y)),
            1f / Mathf.Max(0.01f, Mathf.Abs(road.transform.localScale.z)));

        float width = Mathf.Abs(road.transform.localScale.x);
        float length = Mathf.Abs(road.transform.localScale.z);
        Material marker = GetColorMaterial("LabLaneReference_Cream",
            new Color(0.94f, 0.87f, 0.66f));

        // The container cancels the parent's cube scale; all child
        // positions below are in METERS relative to road center.
        for (float z = -length * 0.5f + 10f;
             z < length * 0.5f - 10f; z += 15f)
        {
            AddNonCollidingStripe(container, "RightRoad_15m", marker,
                new Vector3(width * 0.44f, 0.515f, z),
                new Vector3(0.30f, 0.02f, 2.2f));
            AddNonCollidingStripe(container, "LeftRoad_15m", marker,
                new Vector3(-width * 0.44f, 0.515f, z),
                new Vector3(0.30f, 0.02f, 2.2f));
            // Center marks are repeated every 15m; diagonal travel still
            // has stable fixed-world parallax reference points.
            AddNonCollidingStripe(container, "CenterRoad_15m", marker,
                new Vector3(0f, 0.515f, z),
                new Vector3(0.20f, 0.02f, 1.6f));
        }
    }

    private static void AddNonCollidingStripe(Transform parent, string name,
        Material material, Vector3 position, Vector3 size)
    {
        GameObject objectToMark = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objectToMark.name = name;
        objectToMark.transform.SetParent(parent, false);
        objectToMark.transform.localPosition = position;
        objectToMark.transform.localScale = size;
        Collider collider = objectToMark.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        Renderer renderer = objectToMark.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    public static Material GetGuideMaterial()
    {
        // Vertex tint from LineRenderer.startColor requires a white
        // unlit material. Sprites/Default works in most Unity setups.
        EnsureFolder();
        const string path = Folder + "/KAVRA_Guide_White.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) return null;

        string pipeline = GraphicsSettings.currentRenderPipeline != null
            ? GraphicsSettings.currentRenderPipeline.GetType().Name : "";
        Shader shader = null;
        if (pipeline.Contains("UniversalRenderPipeline"))
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        else if (pipeline.Contains("HDRenderPipeline"))
            shader = Shader.Find("HDRP/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = FindSurfaceShader();
        var material = new Material(shader);
        material.name = "KAVRA Hitch Guide White";
        SetColor(material, Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material GetColorMaterial(string key, Color color)
    {
        EnsureFolder();
        string path = Folder + "/KAVRA_" + key + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            Debug.LogError("KAVRA: material path occupied: " + path);
            return null;
        }

        var material = new Material(FindSurfaceShader());
        material.name = "KAVRA " + key;
        SetColor(material, color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Shader FindSurfaceShader()
    {
        string pipeline = GraphicsSettings.currentRenderPipeline != null
            ? GraphicsSettings.currentRenderPipeline.GetType().Name : "";
        Shader shader = null;
        if (pipeline.Contains("HDRenderPipeline"))
            shader = Shader.Find("HDRP/Lit");
        else if (pipeline.Contains("UniversalRenderPipeline"))
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
            throw new System.InvalidOperationException(
                "KAVRA PhysicsLab: unable to find a supported Unity shader.");
        return shader;
    }

    private static void SetColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/VehiclePhysicsLab", "Generated");
    }
}
