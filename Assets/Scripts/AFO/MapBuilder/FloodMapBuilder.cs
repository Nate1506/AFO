using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// AFO Flood Escape Map Builder
/// Menu: AFO > Build Flood Escape Map
/// สร้างแมพน้ำท่วมแบบ isometric 3D ตาม reference image
/// </summary>
public class FloodMapBuilder
{
#if UNITY_EDITOR
    [MenuItem("AFO/Build Flood Escape Map")]
    public static void BuildMap()
    {
        Debug.Log("[FloodMapBuilder] เริ่มสร้างแมพ...");

        // สร้าง root container
        var mapRoot = new GameObject("=== FLOOD_ESCAPE_MAP ===");
        Undo.RegisterCreatedObjectUndo(mapRoot, "Create Flood Map");

        CreateTerrainBase(mapRoot.transform);
        CreateFloodWater(mapRoot.transform);
        CreateStartZone(mapRoot.transform);
        CreateVillageZone(mapRoot.transform);
        CreateBrokenBridgeZone(mapRoot.transform);
        CreateTempleHillZone(mapRoot.transform);
        CreateEscapePath(mapRoot.transform);
        CreateEnvironment(mapRoot.transform);
        CreateAtmosphere(mapRoot.transform);
        SetupIsometricCamera(mapRoot.transform);

        Selection.activeGameObject = mapRoot;
        EditorUtility.DisplayDialog("AFO Map Builder", "✅ สร้างแมพ Flood Escape เสร็จแล้ว!\n\nกด Play เพื่อดูผลลัพธ์", "OK");
    }

    // ==================== TERRAIN BASE ====================
    static void CreateTerrainBase(Transform parent)
    {
        var zone = new GameObject("Terrain_Base");
        zone.transform.SetParent(parent);

        // เกาะหลัก
        var main = MakeCube("Island_Main", new Vector3(0,-0.5f,0), new Vector3(36,1,28), new Color(0.35f,0.28f,0.18f));
        main.transform.SetParent(zone.transform);

        // เนินสูง (ที่ตั้งวัด)
        var hill = MakeCube("Hill_Temple", new Vector3(12,1.5f,8), new Vector3(10,4,10), new Color(0.28f,0.38f,0.2f));
        hill.transform.SetParent(zone.transform);

        // ขั้นบันไดเนิน
        float[] sy = {0.2f, 0.8f, 1.4f};
        float[] sz = {4f, 6f, 7.2f};
        for (int i = 0; i < 3; i++)
        {
            var step = MakeCube($"Hill_Step_{i}", new Vector3(10,sy[i],sz[i]), new Vector3(9,0.4f,2.5f), new Color(0.32f,0.36f,0.22f));
            step.transform.SetParent(zone.transform);
        }

        // โซนเริ่มต้น (สูงกว่าน้ำ)
        var start = MakeCube("Ground_Start", new Vector3(-13,-0.1f,-5), new Vector3(8,0.4f,8), new Color(0.42f,0.34f,0.22f));
        start.transform.SetParent(zone.transform);

        // ขอบเกาะ
        foreach (var e in new[]{
            ("Left",  new Vector3(-18,-1f,0), new Vector3(1,3,28)),
            ("Right", new Vector3(18,-1f,0),  new Vector3(1,3,28)),
            ("Front", new Vector3(0,-1f,-14), new Vector3(36,3,1)),
            ("Back",  new Vector3(0,-1f,14),  new Vector3(36,3,1))
        })
        {
            var cliff = MakeCube($"Cliff_{e.Item1}", e.Item2, e.Item3, new Color(0.22f,0.18f,0.1f));
            cliff.transform.SetParent(zone.transform);
        }
    }

    // ==================== FLOOD WATER ====================
    static void CreateFloodWater(Transform parent)
    {
        var zone = new GameObject("Water_Flood");
        zone.transform.SetParent(parent);

        // แผ่นน้ำหลัก
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water_Main";
        water.transform.SetParent(zone.transform);
        water.transform.localPosition = new Vector3(0, 0.05f, 1);
        water.transform.localScale = new Vector3(3.5f, 1f, 2.8f);
        var wm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wm.name = "MAT_FloodWater";
        wm.SetColor("_BaseColor", new Color(0.08f, 0.22f, 0.45f, 0.88f));
        wm.SetFloat("_Metallic", 0.5f);
        wm.SetFloat("_Smoothness", 0.95f);
        water.GetComponent<Renderer>().sharedMaterial = wm;

        // ฟองน้ำ/คลื่น
        for (int i = 0; i < 6; i++)
        {
            var foam = GameObject.CreatePrimitive(PrimitiveType.Plane);
            foam.name = $"Water_Foam_{i}";
            foam.transform.SetParent(zone.transform);
            foam.transform.localPosition = new Vector3(
                UnityEngine.Random.Range(-12f, 10f), 0.08f,
                UnityEngine.Random.Range(-5f, 6f));
            foam.transform.localScale = Vector3.one * (0.3f + UnityEngine.Random.value * 0.35f);
            foam.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0f,360f), 0);
            var fm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            fm.SetColor("_BaseColor", new Color(0.65f, 0.82f, 0.95f, 0.55f));
            fm.SetFloat("_Smoothness", 0.2f);
            foam.GetComponent<Renderer>().sharedMaterial = fm;
        }

        zone.AddComponent<FloodWaterAnimator>();
    }

    // ==================== ZONE A: จุดเริ่มต้น ====================
    static void CreateStartZone(Transform parent)
    {
        var zone = new GameObject("Zone_A_StartPoint");
        zone.transform.SetParent(parent);
        zone.transform.localPosition = new Vector3(-13, 0, -5);

        // อาคารหน่วยงาน
        InstPrefab(zone.transform, "Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_5_URP.prefab",
            "CommandCenter", Vector3.zero, Vector3.zero, 1.2f);

        // เสาอากาศ
        var post = MakeCube("Dish_Post", new Vector3(2.5f,0.75f,0.5f), new Vector3(0.12f,1.5f,0.12f), Color.gray);
        post.transform.SetParent(zone.transform);
        var dish = MakeCube("Dish_Head", new Vector3(2.5f,1.7f,0.5f), new Vector3(0.9f,0.08f,0.9f), new Color(0.8f,0.8f,0.8f));
        dish.transform.SetParent(zone.transform);
        dish.transform.localEulerAngles = new Vector3(40,0,0);

        // ป้าย marker (เสา+หัวสีฟ้า)
        var markerPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerPost.name = "StartMarker";
        markerPost.transform.SetParent(zone.transform);
        markerPost.transform.localPosition = new Vector3(0, 2.2f, 0);
        markerPost.transform.localScale = new Vector3(0.1f, 1.3f, 0.1f);
        SetMatColor(markerPost, new Color(0.2f, 0.5f, 1f), 0.8f, 0.6f);

        // Sphere บนหัวเสา
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "StartMarker_Top";
        sphere.transform.SetParent(zone.transform);
        sphere.transform.localPosition = new Vector3(0, 3.6f, 0);
        sphere.transform.localScale = Vector3.one * 0.35f;
        SetMatColor(sphere, new Color(0.1f, 0.4f, 1f), 0.9f, 0.8f);

        // รถทหาร
        var truck = MakeCube("Truck_Body", new Vector3(-2.5f, 0.4f, 1.5f), new Vector3(2f,0.7f,1.2f), new Color(0.28f,0.38f,0.22f));
        truck.transform.SetParent(zone.transform);
        var cab = MakeCube("Truck_Cab", new Vector3(-1.7f, 1.0f, 1.5f), new Vector3(0.8f,0.55f,1f), new Color(0.3f,0.4f,0.24f));
        cab.transform.SetParent(zone.transform);

        // ไฟฉุกเฉิน
        MakeEmergencyLight(zone.transform, new Vector3(1.5f, 1.8f, -0.5f));
        MakeEmergencyLight(zone.transform, new Vector3(-0.5f, 1.8f, -0.5f));
    }

    // ==================== ZONE B: หมู่บ้านน้ำท่วม ====================
    static void CreateVillageZone(Transform parent)
    {
        var zone = new GameObject("Zone_B_Village");
        zone.transform.SetParent(parent);

        // บ้านหลายหลัง
        var houses = new[]{
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_1_URP.prefab", new Vector3(2,0.05f,3),    0f),
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_2_URP.prefab", new Vector3(7,0.05f,2),   45f),
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_3_URP.prefab", new Vector3(5,0.05f,-2), -30f),
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_7_URP.prefab", new Vector3(9,0.05f,-4),  15f),
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_4_URP.prefab", new Vector3(1,0.05f,-3), -60f),
            ("Assets/Kao/MyDreamGameStudio/Medieval Village Pack Vol.1/Prefabs/URP/House_8_URP.prefab", new Vector3(-4,0.05f,4),  20f),
        };
        for (int i = 0; i < houses.Length; i++)
            InstPrefab(zone.transform, houses[i].Item1, $"House_{i+1}",
                houses[i].Item2, new Vector3(0, houses[i].Item3, 0), 1f);

        // น้ำรอบบ้าน (จมน้ำ)
        float[] wx = {2f, 7f, 5f};
        float[] wz = {3f, 2f, -2f};
        for (int i = 0; i < 3; i++)
        {
            var wp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            wp.name = $"Water_House_{i}";
            wp.transform.SetParent(zone.transform);
            wp.transform.localPosition = new Vector3(wx[i], 0.03f, wz[i]);
            wp.transform.localScale = new Vector3(0.55f,1,0.55f);
            var wm = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wm.SetColor("_BaseColor", new Color(0.1f, 0.22f, 0.4f, 0.8f));
            wm.SetFloat("_Smoothness", 0.9f);
            wp.GetComponent<Renderer>().sharedMaterial = wm;
        }

        // Props
        InstPrefab(zone.transform, "Assets/Kao/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/bucket01.prefab",
            "Bucket", new Vector3(3.5f,0.1f,1f), Vector3.zero, 0.8f);
        InstPrefab(zone.transform, "Assets/Kao/NikolayFedorov/OldVillage/VilagePropsPack/Prefab/Wrata01Wood.prefab",
            "WoodGate", new Vector3(6,0f,0), Vector3.zero, 0.6f);

        // ชาวบ้าน
        MakeVillagersGroup(zone.transform, new Vector3(8, 0.1f, -1));
    }

    // ==================== ZONE C: สะพานพัง ====================
    static void CreateBrokenBridgeZone(Transform parent)
    {
        var zone = new GameObject("Zone_C_BrokenBridge");
        zone.transform.SetParent(parent);
        zone.transform.localPosition = new Vector3(-2, 0, 2);

        // สะพานส่วนซ้าย (ดี)
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_01b_PRE.prefab",
            "Bridge_L1", new Vector3(-2f,0,0), new Vector3(0,90,0), 1f);
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_01b_PRE.prefab",
            "Bridge_L2", new Vector3(-1f,0,0), new Vector3(0,90,0), 1f);

        // ส่วนพัง (rotated/dropped)
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_02a_PRE.prefab",
            "Bridge_Broken1", new Vector3(0,-0.5f,0.3f), new Vector3(15,90,-28), 1f);
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_02a_PRE.prefab",
            "Bridge_Broken2", new Vector3(0.8f,-0.9f,-0.2f), new Vector3(-12,90,22), 1f);

        // ส่วนขวา (ดีแต่ขาดช่วง)
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_01b_PRE.prefab",
            "Bridge_R1", new Vector3(2f,0,0), new Vector3(0,90,0), 1f);
        InstPrefab(zone.transform, "Assets/Kao/EmaceArt/Roadside Tales Bridges/Prefabs/EA03_Prop_Pier_End_01a_PRE.prefab",
            "Bridge_End", new Vector3(3f,0,0), new Vector3(0,90,0), 1f);

        // ป้ายเตือน
        MakeWarningSign(zone.transform, new Vector3(0, 2.5f, -0.5f));

        // เศษซากในน้ำ
        for (int i = 0; i < 5; i++)
        {
            float rx = UnityEngine.Random.Range(-1.2f, 1.2f);
            float rz = UnityEngine.Random.Range(-0.6f, 0.6f);
            var d = MakeCube($"Debris_{i}", new Vector3(rx, UnityEngine.Random.Range(-0.3f,0.1f), rz),
                new Vector3(UnityEngine.Random.Range(0.1f,0.4f), 0.08f, UnityEngine.Random.Range(0.4f,1.2f)),
                new Color(0.38f,0.23f,0.1f));
            d.transform.SetParent(zone.transform);
            d.transform.localEulerAngles = new Vector3(
                UnityEngine.Random.Range(-30f,30f),
                UnityEngine.Random.Range(0f,360f),
                UnityEngine.Random.Range(-20f,20f));
        }
    }

    // ==================== ZONE D: เนิน+วัด ====================
    static void CreateTempleHillZone(Transform parent)
    {
        var zone = new GameObject("Zone_D_TempleHill");
        zone.transform.SetParent(parent);
        zone.transform.localPosition = new Vector3(12, 3.5f, 8);

        // วัดไทย
        InstPrefab(zone.transform, "Assets/Kao/Temple/prefab/Templa.prefab",
            "Thai_Temple", Vector3.zero, new Vector3(0,-45,0), 0.8f);

        // ไฟสปอตไลต์รอบวัด
        MakeSpot(zone.transform, new Vector3(-1.5f,3,-1.5f), new Color(1f,0.9f,0.5f), 3.5f);
        MakeSpot(zone.transform, new Vector3(1.5f,3,-1.5f),  new Color(1f,0.9f,0.5f), 3.5f);
        MakeSpot(zone.transform, new Vector3(0,4.5f,1.5f),   new Color(1f,0.85f,0.4f), 5f);

        // เขตปลอดภัย (Safe Zone marker)
        var safe = MakeCube("SafeZone", new Vector3(0,0.05f,-3), new Vector3(4,0.05f,4), new Color(0.1f,0.9f,0.3f,0.4f));
        safe.transform.SetParent(zone.transform);

        // เสาทองจุดหมาย
        var pin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pin.name = "DestinationPin";
        pin.transform.SetParent(zone.transform);
        pin.transform.localPosition = new Vector3(0, 4.5f, -2);
        pin.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
        SetMatColor(pin, new Color(1f,0.85f,0.1f), 1f, 0.9f);

        // ลูกบอลบนเสา
        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = "Pin_Top";
        top.transform.SetParent(zone.transform);
        top.transform.localPosition = new Vector3(0, 6.5f, -2);
        top.transform.localScale = Vector3.one * 0.4f;
        SetMatColor(top, new Color(1f,0.7f,0.05f), 1f, 0.95f);

        // บันได
        MakeStairs(zone.transform, new Vector3(-1, -1.8f, -4));
    }

    // ==================== ESCAPE PATH ====================
    static void CreateEscapePath(Transform parent)
    {
        var zone = new GameObject("EscapePath");
        zone.transform.SetParent(parent);

        var waypoints = new Vector3[]{
            new Vector3(-13, 0.12f, -5),
            new Vector3(-8,  0.12f, -3),
            new Vector3(-4,  0.12f,  0),
            new Vector3(-2,  0.12f,  2),
            new Vector3( 2,  0.12f,  2),
            new Vector3( 6,  0.12f,  0),
            new Vector3( 9,  0.12f,  3),
            new Vector3(11,  1.5f,   6),
            new Vector3(12,  3.5f,   8),
        };

        var pathGo = new GameObject("PathLine");
        pathGo.transform.SetParent(zone.transform);
        var lr = pathGo.AddComponent<LineRenderer>();
        var pm = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        pm.SetColor("_BaseColor", new Color(1f,0.9f,0.1f));
        lr.sharedMaterial = pm;
        lr.startWidth = 0.35f;
        lr.endWidth = 0.15f;
        lr.positionCount = waypoints.Length;
        lr.SetPositions(waypoints);
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;
        pathGo.AddComponent<EscapePathAnimator>();

        // ลูกศรตามทาง
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            var mid = (waypoints[i] + waypoints[i+1]) * 0.5f;
            var arrow = MakeCube($"Arrow_{i}", mid + Vector3.up * 0.08f,
                new Vector3(0.12f, 0.04f, 0.45f), new Color(1f,0.85f,0.1f));
            arrow.transform.SetParent(zone.transform);
            var dir = (waypoints[i+1] - waypoints[i]).normalized;
            if (dir.sqrMagnitude > 0.001f)
                arrow.transform.forward = dir;
        }
    }

    // ==================== ENVIRONMENT ====================
    static void CreateEnvironment(Transform parent)
    {
        var zone = new GameObject("Environment");
        zone.transform.SetParent(parent);

        string t1 = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/EvergreenTallThick.prefab";
        string t2 = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/Evergreen_Med.prefab";
        string t3 = "Assets/Kao/AssetPacks/Pine Forest_LowPoly/Prefabs/Trees/Dead.prefab";
        string rock = "Assets/Kao/JC_StylizedNature_Lite/Prefabs/SM_Rock_01.prefab";
        string raft = "Assets/Kao/Low Poly Boats & Raft/Low Poly Raft/Low Poly Prefab/Low Poly Raft.prefab";
        string boat = "Assets/Kao/Low Poly Boats & Raft/Low Poly Row Boats/Prefabs/NormalBoat.prefab";

        // ต้นไม้รอบเนิน
        var hillTrees = new[]{
            (t1, new Vector3(8,3.5f,11)),  (t2, new Vector3(14,3.5f,12)),
            (t1, new Vector3(16,3.5f,9)),  (t2, new Vector3(10,3.5f,9)),
            (t3, new Vector3(15,3.5f,6)),  (t1, new Vector3(7,3.5f,13)),
        };
        for (int i = 0; i < hillTrees.Length; i++)
        {
            float s = 0.45f + UnityEngine.Random.value * 0.3f;
            InstPrefab(zone.transform, hillTrees[i].Item1, $"Tree_H{i}",
                hillTrees[i].Item2, new Vector3(0, UnityEngine.Random.Range(0f,360f),0), s);
        }

        // ต้นไม้กลางแมพ
        var midTrees = new[]{
            (t2, new Vector3(-6,0,4)),  (t1, new Vector3(-4,0,6)),
            (t3, new Vector3(4,0,5)),   (t2, new Vector3(0,0,-5)),
            (t1, new Vector3(-10,0,0)),(t3, new Vector3(-8,0,-6)),
        };
        for (int i = 0; i < midTrees.Length; i++)
        {
            float s = 0.38f + UnityEngine.Random.value * 0.2f;
            InstPrefab(zone.transform, midTrees[i].Item1, $"Tree_M{i}",
                midTrees[i].Item2, new Vector3(0, UnityEngine.Random.Range(0f,360f),0), s);
        }

        // หิน
        var rocks = new[]{
            new Vector3(-5,0,-6), new Vector3(3,0,-7),
            new Vector3(13,3.5f,10), new Vector3(14,3.5f,7)
        };
        for (int i = 0; i < rocks.Length; i++)
        {
            float s = 0.5f + UnityEngine.Random.value * 0.8f;
            InstPrefab(zone.transform, rock, $"Rock_{i}", rocks[i],
                new Vector3(0, UnityEngine.Random.Range(0f,360f),0), s);
        }

        // เรือ/แพ
        InstPrefab(zone.transform, raft, "Raft", new Vector3(-3,0.05f,1), new Vector3(0,30,0), 0.4f);
        InstPrefab(zone.transform, boat, "Boat", new Vector3(4,0.05f,3), new Vector3(0,-45,0), 0.5f);
        InstPrefab(zone.transform, boat, "Boat_Damaged", new Vector3(-7,-0.1f,3), new Vector3(18,60,12), 0.5f);
    }

    // ==================== ATMOSPHERE ====================
    static void CreateAtmosphere(Transform parent)
    {
        var zone = new GameObject("Atmosphere_Lighting");
        zone.transform.SetParent(parent);

        // Directional light สีพายุ
        var dl = new GameObject("Dir_Storm");
        dl.transform.SetParent(zone.transform);
        dl.transform.localPosition = new Vector3(-10, 25, -10);
        dl.transform.eulerAngles = new Vector3(38, 28, 0);
        var dlight = dl.AddComponent<Light>();
        dlight.type = LightType.Directional;
        dlight.color = new Color(0.5f, 0.56f, 0.82f);
        dlight.intensity = 0.55f;
        dlight.shadows = LightShadows.Soft;

        // Ambient blue flood
        var al = new GameObject("Ambient_Flood");
        al.transform.SetParent(zone.transform);
        al.transform.localPosition = new Vector3(0, 6, 0);
        var alight = al.AddComponent<Light>();
        alight.type = LightType.Point;
        alight.color = new Color(0.15f, 0.45f, 0.85f);
        alight.intensity = 1.4f;
        alight.range = 35f;

        // ฟ้าผ่า
        var ll = new GameObject("Lightning");
        ll.transform.SetParent(zone.transform);
        ll.transform.localPosition = new Vector3(5, 28, -5);
        ll.transform.eulerAngles = new Vector3(90, 0, 0);
        var llight = ll.AddComponent<Light>();
        llight.type = LightType.Spot;
        llight.color = new Color(0.92f, 0.96f, 1f);
        llight.intensity = 0f;
        llight.range = 70f;
        llight.spotAngle = 130f;
        llight.shadows = LightShadows.Soft;
        ll.AddComponent<LightningAnimator>();

        // Fog
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.28f, 0.33f, 0.44f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 22f;
        RenderSettings.fogEndDistance = 65f;
        RenderSettings.ambientLight = new Color(0.08f, 0.1f, 0.16f);
        RenderSettings.ambientIntensity = 0.3f;
    }

    // ==================== CAMERA ====================
    static void SetupIsometricCamera(Transform parent)
    {
        // ลบ camera เดิมถ้ามี
        var existingCam = UnityEngine.Camera.main;
        if (existingCam != null && existingCam.gameObject.name != "Main Camera")
            UnityEngine.Object.DestroyImmediate(existingCam.gameObject);

        var camGo = new GameObject("Camera_IsometricMain");
        camGo.transform.SetParent(parent);
        camGo.transform.position = new Vector3(0, 32, -24);
        camGo.transform.eulerAngles = new Vector3(52, 0, 0);
        camGo.tag = "MainCamera";

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = false;
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;

        // URP Post Processing Volume
        var volGo = new GameObject("PP_Volume_Storm");
        volGo.transform.SetParent(camGo.transform);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        if (!System.IO.Directory.Exists("Assets/Kao/Generated"))
            System.IO.Directory.CreateDirectory("Assets/Kao/Generated");
        AssetDatabase.CreateAsset(profile, "Assets/Kao/Generated/PP_FloodStorm.asset");

        var ca = profile.Add<ColorAdjustments>(true);
        if (ca != null) { ca.postExposure.value = -0.6f; ca.contrast.value = 28f; ca.saturation.value = -18f; ca.active = true; }
        var vi = profile.Add<Vignette>(true);
        if (vi != null) { vi.intensity.value = 0.38f; vi.smoothness.value = 0.55f; vi.active = true; }
        var bl = profile.Add<Bloom>(true);
        if (bl != null) { bl.threshold.value = 0.9f; bl.intensity.value = 0.8f; bl.active = true; }

        vol.sharedProfile = profile;
        AssetDatabase.SaveAssets();
    }

    // ==================== HELPERS ====================
    static GameObject MakeCube(string name, Vector3 pos, Vector3 size, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        SetMatColor(go, col, 0f, 0.3f);
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static void SetMatColor(GameObject go, Color col, float metallic, float smooth)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", col);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smooth);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static GameObject InstPrefab(Transform parent, string path, string goName,
        Vector3 pos, Vector3 rot, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning($"[FloodMap] Missing prefab: {path}");
            var ph = MakeCube(goName+"_PH", pos, Vector3.one*0.6f, Color.magenta);
            ph.transform.SetParent(parent);
            ph.transform.localPosition = pos;
            return ph;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = goName;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        go.transform.localScale = Vector3.one * scale;
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return go;
    }

    static void MakeEmergencyLight(Transform parent, Vector3 pos)
    {
        var obj = new GameObject("EmergencyLight");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        var l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.25f, 0.05f);
        l.intensity = 2.5f;
        l.range = 6f;
        obj.AddComponent<EmergencyLightFlicker>();
    }

    static void MakeSpot(Transform parent, Vector3 pos, Color col, float intensity)
    {
        var obj = new GameObject("SpotLight");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localEulerAngles = new Vector3(90, 0, 0);
        var l = obj.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = col;
        l.intensity = intensity;
        l.range = 10f;
        l.spotAngle = 65f;
        l.shadows = LightShadows.Soft;
    }

    static void MakeWarningSign(Transform parent, Vector3 pos)
    {
        var sign = new GameObject("WarningSign");
        sign.transform.SetParent(parent);
        sign.transform.localPosition = pos;
        var p = MakeCube("Post", new Vector3(0,-0.5f,0), new Vector3(0.08f,1f,0.08f), new Color(0.55f,0.55f,0.55f));
        p.transform.SetParent(sign.transform);
        p.transform.localPosition = new Vector3(0,-0.5f,0);
        var b = MakeCube("Board", Vector3.zero, new Vector3(0.75f,0.65f,0.08f), new Color(1f,0.85f,0.08f));
        b.transform.SetParent(sign.transform);
    }

    static void MakeStairs(Transform parent, Vector3 basePos)
    {
        var stairs = new GameObject("Stairs_ToTemple");
        stairs.transform.SetParent(parent);
        stairs.transform.localPosition = basePos;
        for (int i = 0; i < 8; i++)
        {
            var step = MakeCube($"Step_{i}",
                new Vector3(0, i*0.3f, i*0.4f),
                new Vector3(2.5f,0.3f,0.5f),
                new Color(0.62f,0.52f,0.38f));
            step.transform.SetParent(stairs.transform);
        }
    }

    static void MakeVillagersGroup(Transform parent, Vector3 pos)
    {
        var grp = new GameObject("Villagers");
        grp.transform.SetParent(parent);
        grp.transform.localPosition = pos;
        Color[] cols = {
            new Color(0.8f,0.5f,0.3f), new Color(0.3f,0.5f,0.8f),
            new Color(0.5f,0.8f,0.3f), new Color(0.8f,0.3f,0.5f),
            new Color(0.9f,0.7f,0.2f)
        };
        var offsets = new Vector3[]{
            new Vector3(0,0,0),    new Vector3(0.6f,0,0.4f),
            new Vector3(-0.5f,0,0.3f), new Vector3(0.2f,0,-0.5f),
            new Vector3(-0.3f,0,-0.6f)
        };
        for (int i = 0; i < 5; i++)
        {
            var v = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            v.name = $"Villager_{i}";
            v.transform.SetParent(grp.transform);
            v.transform.localPosition = offsets[i];
            v.transform.localScale = new Vector3(0.28f, 0.48f, 0.28f);
            SetMatColor(v, cols[i], 0f, 0.2f);
        }
        grp.AddComponent<VillagerFollowAnimator>();
    }
#endif
}
