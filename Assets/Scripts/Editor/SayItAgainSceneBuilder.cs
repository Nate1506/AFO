using System.Collections.Generic;
using System.IO;
using System.Linq;
using SayItAgain.Net;
using SayItAgain.UI;
using SayItAgain.Voice;
using TMPro;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SayItAgain.EditorTools
{
    /// <summary>Generates the lobby scene, the gameplay test scene, level data and the prefabs they need. Safe to re-run.</summary>
    public static class SayItAgainSceneBuilder
    {
        const string Root = "Assets/SayItAgain";
        const string PrefabDir = Root + "/Prefabs";
        const string MaterialDir = Root + "/Materials";
        const string SceneDir = Root + "/Scenes";
        const string DataDir = Root + "/Data";
        const string LobbyScene = SceneDir + "/Lobby.unity";
        const string GameplayScene = SceneDir + "/Gameplay_Test.unity";
        const string FontSource = "Assets/Fonts/NotoSansThai/NotoSansThai.ttf";
        const string FontAssetPath = "Assets/Resources/Fonts/NotoSansThai SDF.asset";

        [MenuItem("Tools/Say It Again/Build Scenes")]
        public static void BuildAll()
        {
            EnsureFolders();
            BuildFontAsset();
            BuildLevelData();
            var rig = BuildPlayerRig();
            var roleManager = BuildNetworkPrefab<RoleManager>("RoleManager");
            var matchManager = BuildNetworkPrefab<MatchManager>("MatchManager");
            var villager = BuildVillagerPrefab();
            BuildLobbyScene(roleManager, matchManager);
            BuildGameplayScene(rig, matchManager, villager);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[SayItAgain] Scenes built");
        }

        static void EnsureFolders()
        {
            foreach (var dir in new[] { Root, PrefabDir, MaterialDir, SceneDir, DataDir, "Assets/Resources/Fonts" })
                if (!AssetDatabase.IsValidFolder(dir))
                    Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        static void BuildFontAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null) return;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontSource);
            if (font == null)
            {
                Debug.LogError($"[SayItAgain] Missing {FontSource}");
                return;
            }
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                AtlasPopulationMode.Dynamic, true);
            asset.name = "NotoSansThai SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            // Dynamic atlas texture and material must live inside the font asset file.
            asset.atlasTextures[0].name = "NotoSansThai Atlas";
            asset.material.name = "NotoSansThai Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
        }

        // ---------- Data ----------

        static void BuildLevelData()
        {
            var orders = LoadOrCreate<OrderData>($"{DataDir}/OrderData.asset");
            var w1 = BuildWeather("Weather_Level1", 8f, 0.03f);
            var w2 = BuildWeather("Weather_Level2", 5f, 0.02f);

            var l1 = LoadOrCreate<LevelData>($"{DataDir}/Level_1.asset");
            Set(l1, 1, "level1_name", "level1_lesson", true, w1, orders, rumors: false, fakeHq: false, askHq: 0);
            var l2 = LoadOrCreate<LevelData>($"{DataDir}/Level_2.asset");
            Set(l2, 2, "level2_name", "level2_lesson", true, w2, orders, rumors: true, fakeHq: true, askHq: 2);
            var l3 = LoadOrCreate<LevelData>($"{DataDir}/Level_3.asset");
            Set(l3, 3, "level3_name", "level_coming_soon", false, w2, orders, false, false, 0);
            var l4 = LoadOrCreate<LevelData>($"{DataDir}/Level_4.asset");
            Set(l4, 4, "level4_name", "level_coming_soon", false, w2, orders, false, false, 0);

            var catalog = LoadOrCreate<LevelCatalog>("Assets/Resources/LevelCatalog.asset");
            catalog.levels = new[] { l1, l2, l3, l4 };
            EditorUtility.SetDirty(catalog);
        }

        static void Set(LevelData l, int n, string name, string lesson, bool playable, WeatherConfig w, OrderData o, bool rumors, bool fakeHq, int askHq)
        {
            l.number = n;
            l.nameKey = name;
            l.lessonKey = lesson;
            l.playable = playable;
            l.weather = w;
            l.orders = o;
            l.rumorsEnabled = rumors;
            l.fakeHqMessage = fakeHq;
            l.askHqUses = askHq;
            l.gameplayScene = "Gameplay_Test";
            EditorUtility.SetDirty(l);
        }

        /// <summary>Level 1 is about dropouts (more of them); Level 2 is about rumours (cleaner radio).</summary>
        static WeatherConfig BuildWeather(string name, float dropoutsPerMinute, float noisePerKm)
        {
            string path = $"{DataDir}/{name}.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<WeatherConfig>(path);
            if (cfg != null) return cfg;
            cfg = ScriptableObject.CreateInstance<WeatherConfig>();
            cfg.dropoutsPerMinute = dropoutsPerMinute;
            cfg.noisePerKm = noisePerKm;
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------- Prefabs ----------

        static GameObject BuildPlayerRig()
        {
            var root = new GameObject("PlayerRig");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.35f;

            var camGo = new GameObject("ViewCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
            var cam = camGo.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1200f;

            var fpc = root.AddComponent<FirstPersonController>();
            fpc.viewCamera = cam;
            var bino = root.AddComponent<Binoculars>();
            bino.viewCamera = cam;
            root.AddComponent<BinocularsOverlay>().binoculars = bino;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/PlayerRig.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildNetworkPrefab<T>(string name) where T : NetworkBehaviour
        {
            var go = new GameObject(name);
            go.AddComponent<NetworkObject>();
            go.AddComponent<T>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Placeholder body until the CC0 character models are imported.</summary>
        static Villager BuildVillagerPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Villager";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.7f, 0.85f, 0.7f);
            go.GetComponent<Renderer>().sharedMaterial = Mat("Villager", new Color(0.85f, 0.55f, 0.3f));
            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3.4f;
            agent.radius = 0.35f;
            agent.baseOffset = 0.85f;
            agent.stoppingDistance = 0.8f;
            go.AddComponent<Villager>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Villager.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Villager>();
        }

        // ---------- Scenes ----------

        static void BuildLobbyScene(GameObject roleManagerPrefab, GameObject matchManagerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // The listener lets players hear radio voice while waiting in the lobby.
            var camGo = new GameObject("LobbyCamera", typeof(Camera), typeof(AudioListener));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Navy;

            var nmGo = new GameObject("NetworkManager");
            nmGo.AddComponent<SingletonGuard>();
            var transport = nmGo.AddComponent<UnityTransport>();
            var nm = nmGo.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = true };

            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            list.Add(new NetworkPrefab { Prefab = roleManagerPrefab });
            list.Add(new NetworkPrefab { Prefab = matchManagerPrefab });
            const string listPath = PrefabDir + "/SayItAgainNetworkPrefabs.asset";
            AssetDatabase.DeleteAsset(listPath);
            AssetDatabase.CreateAsset(list, listPath);
            nm.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList> { list };

            var session = nmGo.AddComponent<SessionService>();
            session.roleManagerPrefab = roleManagerPrefab.GetComponent<NetworkObject>();
            nmGo.AddComponent<VoiceService>().defaultWeather = AssetDatabase.LoadAssetAtPath<WeatherConfig>($"{DataDir}/Weather_Level1.asset");
            nmGo.AddComponent<RadioHud>();

            new GameObject("LobbyUI").AddComponent<LobbyUI>();

            EditorSceneManager.SaveScene(scene, LobbyScene);
        }

        static void BuildGameplayScene(GameObject rigPrefab, GameObject matchManagerPrefab, Villager villagerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // The rig brings its own camera.
            Object.DestroyImmediate(Camera.main.gameObject);
            var sun = Object.FindAnyObjectByType<Light>();
            sun.intensity = 0.6f;
            sun.color = new Color(0.75f, 0.8f, 0.9f);

            var world = new GameObject("World").transform;
            Prim(PrimitiveType.Plane, "Ground", world, Vector3.zero, new Vector3(60, 1, 60), Mat("Ground", new Color(0.28f, 0.33f, 0.24f)));
            BuildVan(world);
            BuildTent(world);
            var shouters = BuildVillage(world);
            BuildLowlands(world);
            BuildHill(world);
            BuildFarLandmarks(world);

            var water = Prim(PrimitiveType.Plane, "FloodWater", null, new Vector3(145, -0.6f, 20), new Vector3(21, 1, 20), Mat("FloodWater", new Color(0.32f, 0.27f, 0.18f, 1f), 0.9f));
            Object.DestroyImmediate(water.GetComponent<Collider>());
            // Rises to just under the causeway tops, so the safe route stays readable to the end.
            water.gameObject.AddComponent<FloodRise>().endY = 0.2f;

            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            const string navPath = SceneDir + "/Gameplay_Test_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);

            var boot = new GameObject("GameplayBootstrap").AddComponent<GameplayBootstrap>();
            boot.playerRigPrefab = rigPrefab;
            boot.matchManagerPrefab = matchManagerPrefab.GetComponent<NetworkObject>();
            var director = boot.gameObject.AddComponent<VillageDirector>();
            director.villagerPrefab = villagerPrefab;
            director.shouters = shouters;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = new Color(0.42f, 0.47f, 0.52f);

            EditorSceneManager.SaveScene(scene, GameplayScene);
        }

        static void BuildVan(Transform world)
        {
            var white = Mat("VanWhite", new Color(0.9f, 0.9f, 0.88f));
            var van = Group("RescueVan", world, new Vector3(-150, 0, -40));
            Prim(PrimitiveType.Cube, "Body", van, new Vector3(0, 1.3f, 0), new Vector3(2.2f, 2.2f, 5.2f), white);
            Prim(PrimitiveType.Cube, "Stripe", van, new Vector3(0, 1.3f, 0), new Vector3(2.25f, 0.3f, 5.25f), Mat("AlertRed", UiKit.Red));
            Prim(PrimitiveType.Cube, "TailgateOpen", van, new Vector3(0, 2.7f, -3f), new Vector3(2.1f, 0.08f, 1.6f), white)
                .localRotation = Quaternion.Euler(-15, 0, 0);
            Spawn(PlayerRole.Van, MoveMode.LookOnly, 0f, van.position + new Vector3(0, 0, -3.6f), Quaternion.LookRotation(new Vector3(1, 0, 0.6f)));
        }

        static void BuildTent(Transform world)
        {
            var wood = Mat("Wood", new Color(0.45f, 0.32f, 0.2f));
            var canvas = Mat("TentCanvas", new Color(0.85f, 0.72f, 0.35f));
            var tent = Group("AidTent", world, new Vector3(0, 0, 160));
            Prim(PrimitiveType.Cube, "Floor", tent, new Vector3(0, 0.02f, 0), new Vector3(9, 0.04f, 7), wood);
            Prim(PrimitiveType.Cube, "RoofLeft", tent, new Vector3(-2.3f, 2.2f, 0), new Vector3(0.1f, 5.4f, 7.2f), canvas).localRotation = Quaternion.Euler(0, 0, -35);
            Prim(PrimitiveType.Cube, "RoofRight", tent, new Vector3(2.3f, 2.2f, 0), new Vector3(0.1f, 5.4f, 7.2f), canvas).localRotation = Quaternion.Euler(0, 0, 35);
            Prim(PrimitiveType.Cube, "MapTable", tent, new Vector3(0, 0.45f, 1.5f), new Vector3(2f, 0.9f, 1f), wood);
            Spawn(PlayerRole.Tent, MoveMode.Confined, 3f, tent.position + new Vector3(0, 0.05f, -1f), Quaternion.identity);
        }

        /// <summary>Village on the south bank: three possible gathering points, houses, shouting villagers.</summary>
        static WorldLabel[] BuildVillage(Transform world)
        {
            var wood = Mat("Wood", new Color(0.45f, 0.32f, 0.2f));
            var village = Group("Village", world, Vector3.zero);
            foreach (var p in new[] { new Vector3(92, 1.5f, -22), new Vector3(150, 1.5f, -22), new Vector3(85, 1.5f, -58), new Vector3(158, 1.5f, -62), new Vector3(108, 1.5f, -70), new Vector3(134, 1.5f, -70) })
                Prim(PrimitiveType.Cube, "House", village, p, new Vector3(7, 3, 6), wood);

            var gathers = new[] { ("Market", new Vector3(100, 0, -40), "sign_market", "lm_market"), ("OldSchool", new Vector3(142, 0, -44), "sign_school", "lm_school"), ("RiceMill", new Vector3(120, 0, -14), "sign_mill", "lm_mill") };
            var shouters = new List<WorldLabel>();
            var shouterMat = Mat("Shouter", new Color(0.8f, 0.35f, 0.3f));
            for (int i = 0; i < gathers.Length; i++)
            {
                var (name, pos, sign, _) = gathers[i];
                var g = Group(name, village, pos);
                g.gameObject.AddComponent<GatherPoint>().optionIndex = i;
                Sign(g, new Vector3(0, 0, 6), sign);
                shouters.Add(Shouter(g, new Vector3(4, 0, 4), shouterMat));
            }
            shouters.Add(Shouter(village, new Vector3(120, 0, -32), shouterMat));
            Spawn(PlayerRole.Village, MoveMode.Free, 0f, new Vector3(120, 0.1f, -55), Quaternion.identity);
            return shouters.ToArray();
        }

        /// <summary>
        /// North of the village: the river (three crossings), the bank road, then a flooded field
        /// crossed by three raised causeways to the hill. Order indices match OrderData.
        /// </summary>
        static void BuildLowlands(Transform world)
        {
            var water = Mat("DeepWater", new Color(0.25f, 0.24f, 0.18f), 0.9f);
            var deck = Mat("Wood", new Color(0.45f, 0.32f, 0.2f));
            var mud = Mat("Mud", new Color(0.33f, 0.26f, 0.18f));
            var low = Group("Lowlands", world, Vector3.zero);

            Hazard(low, "River", new Vector3(140, 0, 10), new Vector3(180, 4, 10), HazardZone.Rule.Always, 0, water, 0.15f);
            var crossings = new[] { ("RiverBridge", 120f, "sign_bridge"), ("SchoolFord", 142f, "sign_school_ford"), ("MarketFord", 100f, "sign_market_ford") };
            for (int i = 0; i < crossings.Length; i++)
            {
                var (name, x, sign) = crossings[i];
                Prim(PrimitiveType.Cube, name + "_Deck", low, new Vector3(x, 0.1f, 10), new Vector3(6, 0.2f, 11), deck);
                Hazard(low, name, new Vector3(x, 0, 10), new Vector3(6, 4, 10.5f), HazardZone.Rule.IfAvoidedCrossing, i, water, 0.45f);
                Sign(low, new Vector3(x + 4, 0, 2), sign);
            }

            Hazard(low, "FloodedField", new Vector3(145, 0, 61), new Vector3(190, 4, 78), HazardZone.Rule.Always, 0, water, 0.15f);
            var causeways = new[] { ("EastRoad", 209f, "sign_east"), ("WestRidge", 74f, "sign_west"), ("NorthTrack", 140f, "sign_north") };
            for (int i = 0; i < causeways.Length; i++)
            {
                var (name, x, sign) = causeways[i];
                Prim(PrimitiveType.Cube, name + "_Causeway", low, new Vector3(x, 0.125f, 61), new Vector3(8, 0.25f, 78.5f), mud);
                Hazard(low, name, new Vector3(x, 0, 61), new Vector3(8, 4, 78.5f), HazardZone.Rule.UnlessSafeRoute, i, water, 0.45f);
                Sign(low, new Vector3(x + 5, 0, 19), sign);
            }

            // Villagers path only over crossings and causeways, so they drown because of wrong
            // information (a collapsed crossing, a flooded route), not because they cut a corner.
            foreach (var (x0, x1) in Gaps(50, 230, new[] { 100f, 120f, 142f }, 6f)) NoWalk(low, x0, x1, 5, 15);
            foreach (var (x0, x1) in Gaps(50, 240, new[] { 74f, 140f, 209f }, 8f)) NoWalk(low, x0, x1, 22, 100);
        }

        /// <summary>Spans between [min,max] that are not covered by strips of the given width.</summary>
        static IEnumerable<(float, float)> Gaps(float min, float max, float[] centers, float width)
        {
            float start = min;
            foreach (var c in centers.OrderBy(c => c))
            {
                yield return (start, c - width * 0.5f);
                start = c + width * 0.5f;
            }
            yield return (start, max);
        }

        static void NoWalk(Transform parent, float x0, float x1, float z0, float z1)
        {
            var vol = Group("NoWalk", parent, new Vector3((x0 + x1) * 0.5f, 0, (z0 + z1) * 0.5f)).gameObject.AddComponent<NavMeshModifierVolume>();
            vol.size = new Vector3(x1 - x0, 4f, z1 - z0);
            vol.center = new Vector3(0, 1f, 0);
            vol.area = NavMesh.GetAreaFromName("Not Walkable");
        }

        static void BuildHill(Transform world)
        {
            var hill = Prim(PrimitiveType.Sphere, "HighHill", world, new Vector3(200, -12, 150), new Vector3(120, 40, 120), Mat("Ground", new Color(0.28f, 0.33f, 0.24f)));
            // Non-uniform sphere colliders stay round; a mesh collider follows the flattened hill.
            Object.DestroyImmediate(hill.GetComponent<SphereCollider>());
            hill.gameObject.AddComponent<MeshCollider>();
            AddLandmark(hill.gameObject, "lm_hill", new Vector3(200, 12, 150));
            var top = Group("SafeZone", world, new Vector3(200, 0, 150));
            top.gameObject.AddComponent<SafeZone>();
            Sign(top, new Vector3(0, 7.8f, 0), "sign_hill");
        }

        static void BuildFarLandmarks(Transform world)
        {
            var wood = Mat("Wood", new Color(0.45f, 0.32f, 0.2f));
            var concrete = Mat("Concrete", new Color(0.55f, 0.55f, 0.55f));
            var tree = Group("BigRainTree", world, new Vector3(90, 0, 120));
            Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 4, 0), new Vector3(1.2f, 4, 1.2f), wood);
            Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0, 10, 0), new Vector3(12, 7, 12), Mat("Leaf", new Color(0.18f, 0.35f, 0.18f)));
            AddLandmark(tree.gameObject, "lm_bigtree", tree.position + Vector3.up * 8);
            var tower = Group("WaterTower", world, new Vector3(-40, 0, 40));
            Prim(PrimitiveType.Cylinder, "Leg", tower, new Vector3(0, 6, 0), new Vector3(0.8f, 6, 0.8f), concrete);
            Prim(PrimitiveType.Cylinder, "Tank", tower, new Vector3(0, 14, 0), new Vector3(6, 2.5f, 6), Mat("AlertRed", UiKit.Red));
            AddLandmark(tower.gameObject, "lm_tower", tower.position + Vector3.up * 14);
        }

        static void RegisterScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != LobbyScene && s.path != GameplayScene).ToList();
            scenes.Add(new EditorBuildSettingsScene(LobbyScene, true));
            scenes.Add(new EditorBuildSettingsScene(GameplayScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Helpers ----------

        static void Hazard(Transform parent, string name, Vector3 center, Vector3 size, HazardZone.Rule rule, int option, Material water, float surfaceY)
        {
            var zone = Group(name + "_Hazard", parent, center).gameObject.AddComponent<HazardZone>();
            zone.rule = rule;
            zone.optionIndex = option;
            zone.size = size;
            var vis = Prim(PrimitiveType.Cube, "Water", zone.transform, new Vector3(0, surfaceY - 0.05f, 0), new Vector3(size.x, 0.1f, size.z), water);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            zone.waterVisual = vis.gameObject;
        }

        static void Sign(Transform parent, Vector3 localPos, string key)
        {
            var post = Prim(PrimitiveType.Cube, "SignPost", parent, localPos + new Vector3(0, 1.2f, 0), new Vector3(0.15f, 2.4f, 0.15f), Mat("Wood", new Color(0.45f, 0.32f, 0.2f)));
            Object.DestroyImmediate(post.GetComponent<Collider>());
            WorldLabel.Create(post.parent, localPos + new Vector3(0, 2.8f, 0), key, 2.5f, UiKit.Yellow);
        }

        static WorldLabel Shouter(Transform parent, Vector3 localPos, Material mat)
        {
            var body = Prim(PrimitiveType.Capsule, "Shouter", parent, localPos + new Vector3(0, 0.85f, 0), new Vector3(0.7f, 0.85f, 0.7f), mat);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            return WorldLabel.Create(body, new Vector3(0, 1.6f, 0), "", 3.2f, UiKit.White);
        }

        static Transform Group(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = position;
            return t;
        }

        static void Spawn(PlayerRole role, MoveMode mode, float radius, Vector3 pos, Quaternion rot)
        {
            var sp = new GameObject("Spawn_" + role).AddComponent<RoleSpawnPoint>();
            sp.role = role;
            sp.moveMode = mode;
            sp.confineRadius = radius;
            sp.transform.SetPositionAndRotation(pos, rot);
        }

        static void AddLandmark(GameObject go, string key, Vector3 focus)
        {
            var lm = go.AddComponent<Landmark>();
            lm.nameKey = key;
            var fp = new GameObject("Focus").transform;
            fp.SetParent(go.transform, true);
            fp.position = focus;
            lm.focusPoint = fp;
        }

        static Transform Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        static Material Mat(string name, Color color, float smoothness = 0.55f)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
