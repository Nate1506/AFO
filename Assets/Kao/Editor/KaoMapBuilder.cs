using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// KaoMapBuilder - builds the flood-disaster isometric map for the Kao scene.
/// Call via menu: Tools > Kao Map > Build Map
/// </summary>
public static class KaoMapBuilder
{
    const string TEMPLE_PREFAB   = "Assets/Kao/Temple/prefab/Templa.prefab";
    const string HOUSE1_PREFAB   = "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_1_URP.prefab";
    const string HOUSE2_PREFAB   = "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_2_URP.prefab";
    const string HOUSE3_PREFAB   = "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_3_URP.prefab";
    const string HOUSE4_PREFAB   = "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_4_URP.prefab";
    const string HOUSE5_PREFAB   = "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_5_URP.prefab";
    const string PIER_PREFAB     = "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_02a_PRE.prefab";
    const string PIER_END_PREFAB = "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_End_01a_PRE.prefab";
    const string TREE1_PREFAB    = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/EvergreenTallThick.prefab";
    const string TREE2_PREFAB    = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/Evergreen_Med.prefab";
    const string TREE3_PREFAB    = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/Evergreen_MedSmall.prefab";

    const float MAP_SIZE    = 60f;
    const float WATER_LEVEL = 0.3f;
    const float HILL_HEIGHT = 8f;

    [MenuItem("Tools/Kao Map/Build Map")]
    public static void BuildMap()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.path.Contains("Kao"))
            scene = EditorSceneManager.OpenScene("Assets/Kao/Kao.unity", OpenSceneMode.Single);

        ClearMapObjects();

        EnsureGeneratedFolder();

        BuildTerrain();
        BuildWater();

        var root = new GameObject("MAP_ROOT");
        Undo.RegisterCreatedObjectUndo(root, "Build Kao Map");

        PlaceTemple(root);
        PlaceVillage(root);
        PlaceBridge(root);
        PlaceTrees(root);
        PlaceCommandPost(root);
        PlaceEvacuationPath(root);

        SetupAtmosphere();
        SetupCamera();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[KaoMapBuilder] Map built! Scene saved.");
    }

    static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Kao/Editor"))
            AssetDatabase.CreateFolder("Assets/Kao", "Editor");
        if (!AssetDatabase.IsValidFolder("Assets/Kao/Editor/Generated"))
            AssetDatabase.CreateFolder("Assets/Kao/Editor", "Generated");
    }

    static void ClearMapObjects()
    {
        foreach (var n in new[] { "MAP_ROOT", "KaoTerrain", "FloodWater" })
        {
            var go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
        // Remove old generated folder assets so we can recreate
        AssetDatabase.DeleteAsset("Assets/Kao/Editor/Generated");
        EnsureGeneratedFolder();
    }

    // ── TERRAIN ──────────────────────────────────────────────────────────────
    static void BuildTerrain()
    {
        int res = 129;
        var data = new TerrainData();
        data.heightmapResolution = res;
        data.size = new Vector3(MAP_SIZE, HILL_HEIGHT + 3f, MAP_SIZE);

        float[,] h = new float[res, res];
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = x / (float)(res - 1);
                float nz = z / (float)(res - 1);

                float val = Mathf.PerlinNoise(nx * 3f + 1f, nz * 3f + 1f) * 0.06f;

                // Temple hill top-right
                float d = Vector2.Distance(new Vector2(nx, nz), new Vector2(0.83f, 0.83f));
                float hill = Mathf.Clamp01(1f - d * 2.6f);
                val += hill * hill * hill * 0.85f;

                // Flood channel (diagonal trench)
                float ch = Mathf.Abs(nz - 0.38f - nx * 0.18f);
                val -= Mathf.Clamp01(1f - ch * 7f) * 0.05f;

                // Left cliff edge
                val += Mathf.Clamp01(nx * 4f - 3.2f) * 0.1f;

                h[z, x] = Mathf.Clamp01(val);
            }
        }
        data.SetHeights(0, 0, h);

        AssetDatabase.CreateAsset(data, "Assets/Kao/Editor/Generated/KaoTerrainData.asset");

        var tGO = Terrain.CreateTerrainGameObject(data);
        tGO.name = "KaoTerrain";
        tGO.transform.position = new Vector3(-MAP_SIZE * 0.5f, 0f, -MAP_SIZE * 0.5f);
        var t = tGO.GetComponent<Terrain>();
        t.heightmapPixelError = 4;
        t.basemapDistance = 300;
        t.drawTreesAndFoliage = false;
        Undo.RegisterCreatedObjectUndo(tGO, "Build Terrain");
    }

    // ── WATER ─────────────────────────────────────────────────────────────────
    static void BuildWater()
    {
        var wGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        wGO.name = "FloodWater";
        wGO.transform.position = new Vector3(0, WATER_LEVEL, 0);
        wGO.transform.localScale = new Vector3(MAP_SIZE * 0.1f, 1f, MAP_SIZE * 0.1f);
        Object.DestroyImmediate(wGO.GetComponent<MeshCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = "FloodWater_Mat";
        mat.SetColor("_BaseColor", new Color(0.12f, 0.33f, 0.52f, 0.76f));
        mat.SetFloat("_Metallic", 0.15f);
        mat.SetFloat("_Smoothness", 0.88f);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;

        AssetDatabase.CreateAsset(mat, "Assets/Kao/Editor/Generated/FloodWater_Mat.mat");
        wGO.GetComponent<MeshRenderer>().sharedMaterial = mat;
        wGO.AddComponent<FloodWaterWave>();
        Undo.RegisterCreatedObjectUndo(wGO, "Create Flood Water");
    }

    // ── TEMPLE ────────────────────────────────────────────────────────────────
    static void PlaceTemple(GameObject root)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TEMPLE_PREFAB);
        if (prefab == null) { Debug.LogWarning("[KaoMap] Temple prefab not found"); return; }

        var go = PrefabUtility.InstantiatePrefab(prefab, root.transform) as GameObject;
        go.name = "Temple_SafeZone";
        go.transform.position = new Vector3(22f, HILL_HEIGHT + 0.8f, 20f);
        go.transform.rotation = Quaternion.Euler(0, -30f, 0);
        go.transform.localScale = Vector3.one * 1.8f;

        // Golden glow
        var glow = new GameObject("Temple_GoldenLight");
        glow.transform.SetParent(go.transform, false);
        glow.transform.localPosition = new Vector3(0, 4f, 0);
        var lc = glow.AddComponent<Light>();
        lc.type  = LightType.Point;
        lc.color = new Color(1f, 0.88f, 0.45f);
        lc.intensity = 5f;
        lc.range = 22f;
        Undo.RegisterCreatedObjectUndo(go, "Place Temple");
    }

    // ── VILLAGE ───────────────────────────────────────────────────────────────
    static void PlaceVillage(GameObject root)
    {
        var data = new (float x, float z, float ry, float s, string p)[]
        {
            ( 8f,  8f,  20f, 1.1f, HOUSE1_PREFAB),
            (12f,  5f, -10f, 1.2f, HOUSE2_PREFAB),
            (16f,  9f,  45f, 1.0f, HOUSE3_PREFAB),
            (10f, 13f,   0f, 1.0f, HOUSE1_PREFAB),
            (14f, 15f, -30f, 1.1f, HOUSE4_PREFAB),
            (18f, 12f,  15f, 0.9f, HOUSE5_PREFAB),
            (-5f,  6f,  10f, 1.0f, HOUSE2_PREFAB),
            (-8f, 10f,  60f, 0.9f, HOUSE3_PREFAB),
            (-3f, 14f, -15f, 1.1f, HOUSE1_PREFAB),
            ( 2f, 18f,  90f, 1.0f, HOUSE4_PREFAB),
            (-1f,  3f,   5f, 0.95f,HOUSE5_PREFAB),
            ( 5f,  2f,  -5f, 1.0f, HOUSE1_PREFAB),
        };

        var vRoot = new GameObject("Village");
        vRoot.transform.SetParent(root.transform);

        foreach (var h in data)
        {
            var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(h.p);
            if (pfb == null) continue;
            var go = PrefabUtility.InstantiatePrefab(pfb, vRoot.transform) as GameObject;
            go.transform.position = new Vector3(h.x, WATER_LEVEL - 0.15f, h.z);
            go.transform.rotation = Quaternion.Euler(0, h.ry, 0);
            go.transform.localScale = Vector3.one * h.s;
        }
        Undo.RegisterCreatedObjectUndo(vRoot, "Place Village");
    }

    // ── BRIDGE ────────────────────────────────────────────────────────────────
    static void PlaceBridge(GameObject root)
    {
        var bRoot = new GameObject("Bridge_Crossing");
        bRoot.transform.SetParent(root.transform);

        float bx = 2f, bz = 0f, secW = 2.5f;
        int secs = 5;

        var pier    = AssetDatabase.LoadAssetAtPath<GameObject>(PIER_PREFAB);
        var pierEnd = AssetDatabase.LoadAssetAtPath<GameObject>(PIER_END_PREFAB);
        if (pier == null) { Debug.LogWarning("[KaoMap] Pier prefab not found"); return; }

        for (int i = 0; i < secs; i++)
        {
            var pfb = (i == 0 || i == secs - 1) ? (pierEnd ?? pier) : pier;
            var go  = PrefabUtility.InstantiatePrefab(pfb, bRoot.transform) as GameObject;
            go.name = $"Bridge_Sec_{i}";
            go.transform.position = new Vector3(bx + i * secW - secs * secW * 0.5f, WATER_LEVEL + 0.35f, bz);
            go.transform.rotation = Quaternion.Euler(0, 90f, 0);
        }

        // Broken segment
        var brk = PrefabUtility.InstantiatePrefab(pier, bRoot.transform) as GameObject;
        brk.name = "Bridge_Broken";
        brk.transform.position = new Vector3(bx + 2 * secW - secs * secW * 0.5f, WATER_LEVEL - 0.4f, bz + 0.9f);
        brk.transform.rotation = Quaternion.Euler(-28f, 90f, 18f);

        // Danger marker (red glowing cube)
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "BrokenBridge_Marker";
        marker.transform.SetParent(bRoot.transform);
        marker.transform.position = new Vector3(bx, WATER_LEVEL + 2.8f, bz);
        marker.transform.localScale = Vector3.one * 0.45f;
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
        var mMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mMat.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f));
        mMat.EnableKeyword("_EMISSION");
        mMat.SetColor("_EmissionColor", Color.red * 2f);
        AssetDatabase.CreateAsset(mMat, "Assets/Kao/Editor/Generated/DangerMarker_Mat.mat");
        marker.GetComponent<MeshRenderer>().sharedMaterial = mMat;

        Undo.RegisterCreatedObjectUndo(bRoot, "Place Bridge");
    }

    // ── TREES ─────────────────────────────────────────────────────────────────
    static void PlaceTrees(GameObject root)
    {
        var tRoot = new GameObject("Trees");
        tRoot.transform.SetParent(root.transform);

        string[] paths = { TREE1_PREFAB, TREE2_PREFAB, TREE3_PREFAB };

        var tData = new (float x, float z, float s, int t)[]
        {
            (18f, 18f, 1.5f, 0), (20f, 15f, 1.3f, 1), (24f, 16f, 1.4f, 0),
            (16f, 22f, 1.2f, 2), (22f, 22f, 1.6f, 0), (25f, 20f, 1.1f, 1),
            (-2f, 16f, 1.2f, 1), ( 6f, 20f, 1.3f, 2), (20f,  4f, 1.0f, 1),
            (-10f, 5f, 1.1f, 0), (-12f,12f, 1.4f, 2), (14f, 20f, 1.2f, 0),
            (-4f, -4f, 1.0f, 2), ( 4f, -2f, 1.1f, 1), (10f,  2f, 1.0f, 2),
            (-18f,-5f, 1.3f, 0), (24f, -2f, 1.2f, 1), (-15f,18f, 1.5f, 0),
            ( 0f, 22f, 1.1f, 2), (-6f, -8f, 1.2f, 0),
        };

        foreach (var d in tData)
        {
            var pfb = AssetDatabase.LoadAssetAtPath<GameObject>(paths[d.t]);
            if (pfb == null) continue;
            var go = PrefabUtility.InstantiatePrefab(pfb, tRoot.transform) as GameObject;
            go.transform.position = new Vector3(d.x, 0.1f, d.z);
            go.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            go.transform.localScale = Vector3.one * d.s;
        }
        Undo.RegisterCreatedObjectUndo(tRoot, "Place Trees");
    }

    // ── COMMAND POST ──────────────────────────────────────────────────────────
    static void PlaceCommandPost(GameObject root)
    {
        var cp = new GameObject("CommandPost_Start");
        cp.transform.SetParent(root.transform);
        cp.transform.position = new Vector3(-20f, 0.5f, -15f);

        // Main building body
        var body = CreateBox("CP_Body", cp.transform, Vector3.zero, new Vector3(5f, 3f, 4f),
                             new Color(0.52f, 0.57f, 0.47f));

        // Roof
        CreateBox("CP_Roof", cp.transform, new Vector3(0f, 2f, 0f), new Vector3(5.6f, 0.4f, 4.6f),
                  new Color(0.28f, 0.28f, 0.28f));

        // Satellite pole
        CreateCyl("CP_SatPole", cp.transform, new Vector3(-1.5f, 3.5f, -0.5f),
                  new Vector3(0.1f, 2f, 0.1f), new Color(0.6f, 0.6f, 0.6f));

        // Dish
        var dish = CreateSphere("CP_SatDish", cp.transform, new Vector3(-1.5f, 5.4f, -0.5f),
                                new Vector3(1.2f, 0.18f, 1.2f), new Color(0.85f, 0.85f, 0.85f), metallic: 0.7f);
        dish.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

        // Blue beacon light
        var beacon = new GameObject("CP_Beacon");
        beacon.transform.SetParent(cp.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 5.5f, 0f);
        var bl = beacon.AddComponent<Light>();
        bl.type = LightType.Point; bl.color = new Color(0.2f, 0.6f, 1f);
        bl.intensity = 3.5f; bl.range = 18f;

        Undo.RegisterCreatedObjectUndo(cp, "Place Command Post");
    }

    // ── EVACUATION PATH ───────────────────────────────────────────────────────
    static void PlaceEvacuationPath(GameObject root)
    {
        var pRoot = new GameObject("EvacuationPath");
        pRoot.transform.SetParent(root.transform);

        var pathMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        pathMat.SetColor("_BaseColor", new Color(1f, 0.88f, 0.1f));
        pathMat.EnableKeyword("_EMISSION");
        pathMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0f) * 1.5f);
        AssetDatabase.CreateAsset(pathMat, "Assets/Kao/Editor/Generated/EvacPath_Mat.mat");

        var pts = new Vector3[]
        {
            new Vector3(-20f, WATER_LEVEL + 0.05f, -15f),
            new Vector3(-10f, WATER_LEVEL + 0.05f, -10f),
            new Vector3( -2f, WATER_LEVEL + 0.05f,  -3f),
            new Vector3(  2f, WATER_LEVEL + 0.05f,   2f),
            new Vector3(  8f, WATER_LEVEL + 0.05f,   8f),
            new Vector3( 14f, WATER_LEVEL + 0.05f,  14f),
            new Vector3( 18f, 1.5f,                 18f),
            new Vector3( 22f, HILL_HEIGHT * 0.55f,  20f),
        };

        float spacing = 1.5f;
        int idx = 0;
        for (int i = 0; i < pts.Length - 1; i++)
        {
            float dist = Vector3.Distance(pts[i], pts[i + 1]);
            int cnt = Mathf.CeilToInt(dist / spacing);
            for (int j = 0; j <= cnt; j++)
            {
                float t = j / (float)cnt;
                Vector3 pos = Vector3.Lerp(pts[i], pts[i + 1], t);
                var dot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                dot.name = $"PD_{idx++}";
                dot.transform.SetParent(pRoot.transform);
                dot.transform.position = pos;
                dot.transform.localScale = new Vector3(0.22f, 0.07f, 0.22f);
                dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Object.DestroyImmediate(dot.GetComponent<CapsuleCollider>());
                dot.GetComponent<MeshRenderer>().sharedMaterial = pathMat;
            }
        }
        Undo.RegisterCreatedObjectUndo(pRoot, "Place Evac Path");
    }

    // ── ATMOSPHERE ────────────────────────────────────────────────────────────
    static void SetupAtmosphere()
    {
        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.07f, 0.09f, 0.15f);
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.ExponentialSquared;
        RenderSettings.fogColor     = new Color(0.13f, 0.18f, 0.28f);
        RenderSettings.fogDensity   = 0.011f;

        var dlGO = GameObject.Find("Directional Light");
        if (dlGO == null) { dlGO = new GameObject("Directional Light"); dlGO.AddComponent<Light>(); }
        var dl = dlGO.GetComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(0.5f, 0.62f, 0.84f);
        dl.intensity = 0.55f;
        dl.shadows = LightShadows.Soft;
        dl.shadowStrength = 0.88f;
        dlGO.transform.rotation = Quaternion.Euler(35f, 200f, 0f);

        var lfGO = GameObject.Find("LightningFlash") ?? new GameObject("LightningFlash");
        Undo.RegisterCreatedObjectUndo(lfGO, "Lightning");
        var lf = lfGO.GetComponent<Light>() ?? lfGO.AddComponent<Light>();
        lf.type = LightType.Directional;
        lf.color = new Color(0.88f, 0.93f, 1f);
        lf.intensity = 0f;
        lf.shadows = LightShadows.None;
        if (lfGO.GetComponent<LightningFlashController>() == null)
            lfGO.AddComponent<LightningFlashController>();
    }

    // ── CAMERA ────────────────────────────────────────────────────────────────
    static void SetupCamera()
    {
        var camGO = Camera.main?.gameObject
                 ?? GameObject.FindWithTag("MainCamera")
                 ?? new GameObject("Main Camera");
        if (camGO.GetComponent<Camera>() == null) camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 42f, -32f);
        camGO.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        var c = camGO.GetComponent<Camera>();
        c.fieldOfView  = 45f;
        c.nearClipPlane = 0.3f;
        c.farClipPlane  = 500f;
        c.backgroundColor = new Color(0.04f, 0.07f, 0.13f);
        c.clearFlags = CameraClearFlags.SolidColor;
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    static GameObject CreateBox(string n, Transform parent, Vector3 lp, Vector3 ls, Color col, float metallic = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n; go.transform.SetParent(parent, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        SetMat(go, col, n, metallic);
        return go;
    }
    static GameObject CreateCyl(string n, Transform parent, Vector3 lp, Vector3 ls, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n; go.transform.SetParent(parent, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        SetMat(go, col, n, 0.4f);
        return go;
    }
    static GameObject CreateSphere(string n, Transform parent, Vector3 lp, Vector3 ls, Color col, float metallic = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n; go.transform.SetParent(parent, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SetMat(go, col, n, metallic);
        return go;
    }
    static void SetMat(GameObject go, Color col, string matName, float metallic)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", 0.4f);
        AssetDatabase.CreateAsset(mat, $"Assets/Kao/Editor/Generated/{matName}_Mat.mat");
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }
}
