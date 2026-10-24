using System.Collections.Generic;
using SayItAgain.Net;
using SayItAgain.UI;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SayItAgain.EditorTools
{
    /// <summary>
    /// Gameplay on Kao's flood map (Assets/Kao/Kao.unity). The original scene is copied, never edited:
    /// the copy gets the three role spawns, gathering points, the three roads to the temple hill with
    /// flood zones on the wrong roads, the three hill stairs, the storm and the opening cutscene.
    /// Layout: docs/kao_layout_proposal.png.
    /// </summary>
    public static partial class SayItAgainSceneBuilder
    {
        const string KaoSource = "Assets/Kao/Kao.unity";
        const string KaoScene = SceneDir + "/Gameplay_Kao.unity";
        static readonly Vector3 KaoTemple = new Vector3(56.4f, 0f, 213.9f);
        static readonly Vector3 KaoVan = new Vector3(145f, 0f, 98f);
        static readonly Vector3 KaoTent = new Vector3(-14.3f, 0f, 0f);
        static readonly Vector3 KaoVillage = new Vector3(64f, 0f, 66f);

        /// <summary>Road centre lines from the village to the foot of the hill. Index = OrderData.routeKeys.</summary>
        static readonly Vector2[][] KaoRoads =
        {
            // 0: east road over the east bridge
            new[] { new Vector2(90, 100), new Vector2(130, 104), new Vector2(150, 135), new Vector2(140, 175), new Vector2(100, 196) },
            // 1: west road over the west bridge
            new[] { new Vector2(70, 98), new Vector2(40, 118), new Vector2(12, 140), new Vector2(0, 165), new Vector2(20, 195) },
            // 2: centre road along the lake
            new[] { new Vector2(76, 100), new Vector2(96, 130), new Vector2(86, 165), new Vector2(62, 190) },
        };
        static readonly string[] KaoRoadSigns = { "sign_east", "sign_west", "sign_north" };

        static void BuildKaoScene(GameObject rigPrefab, GameObject matchManagerPrefab, Villager villagerPrefab)
        {
            AssetDatabase.DeleteAsset(KaoScene);
            AssetDatabase.CopyAsset(KaoSource, KaoScene);
            var scene = EditorSceneManager.OpenScene(KaoScene, OpenSceneMode.Single);

            Light sun = null;
            Material floodMat = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                switch (go.name)
                {
                    // Kao's test player, camera and flash light are replaced by ours.
                    case "Main Camera":
                    case "Player":
                    case "LightningFlash":
                        Object.DestroyImmediate(go);
                        break;
                    case "Directional Light":
                        sun = go.GetComponent<Light>();
                        break;
                    case "FloodWater":
                        // Visual only: the water must not block walking or the NavMesh.
                        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
                        var rise = go.AddComponent<FloodRise>();
                        rise.startY = 0.15f;
                        rise.endY = 0.5f;
                        floodMat = go.GetComponent<Renderer>().sharedMaterial;
                        break;
                }
            }
            ReplaceKaoTerrainTextures(scene);
            Physics.SyncTransforms();
            sun.intensity = 0.6f;
            sun.color = new Color(0.75f, 0.8f, 0.9f);

            var world = new GameObject("SayItAgain").transform;
            BuildKaoVan(world);
            Spawn(PlayerRole.Tent, MoveMode.Confined, 3f, OnGround(KaoTent, 0.1f), Quaternion.LookRotation(new Vector3(1, 0, 1)));
            var shouters = BuildKaoVillage(world);
            BuildKaoRoads(world, floodMat);
            BuildKaoHill(world);

            var nav = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.All;
            nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            nav.BuildNavMesh();
            const string navPath = SceneDir + "/Gameplay_Kao_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(nav.navMeshData, navPath);

            var boot = new GameObject("GameplayBootstrap").AddComponent<GameplayBootstrap>();
            boot.playerRigPrefab = rigPrefab;
            boot.matchManagerPrefab = matchManagerPrefab.GetComponent<NetworkObject>();
            var director = boot.gameObject.AddComponent<VillageDirector>();
            director.villagerPrefab = villagerPrefab;
            director.shouters = shouters;
            director.followClip = Clip("Voices/villager_come_on_lets_run.ogg");
            director.cheerClip = Clip("Voices/villagers_cheer.ogg");
            director.sweptClips = new[] { Clip("Voices/villager_scream.ogg") };

            BuildStorm(sun);
            GameObject.Find("FloodSiren").transform.position = OnGround(KaoVillage, 8f);
            BuildKaoCutscene();

            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The rescue van on the east shore, back doors open towards the lake.</summary>
        static void BuildKaoVan(Transform world)
        {
            var van = Group("RescueVan", world, OnGround(KaoVan, 0f));
            // Facing east, so the open back looks west over the water to the village and the hill.
            van.rotation = Quaternion.Euler(0, 90f, 0);
            Model("CarKit/ambulance.fbx", van, Vector3.zero, 0f, 1.6f);
            Prim(PrimitiveType.Cube, "TailgateOpen", van, new Vector3(0, 2.7f, -2.9f), new Vector3(2.2f, 0.08f, 1.4f), Mat("VanWhite", new Color(0.9f, 0.9f, 0.88f)))
                .localRotation = Quaternion.Euler(-15, 0, 0);
            Model("CarKit/cone.fbx", van, new Vector3(-2.2f, 0, -4.5f), 0f, 1.6f);
            Model("CarKit/cone.fbx", van, new Vector3(2.2f, 0, -4.5f), 0f, 1.6f);
            Model("SurvivalKit/box-large.fbx", van, new Vector3(1.8f, 0, -3.4f), 20f, 1.5f);
            var spawn = van.TransformPoint(new Vector3(0, 0, -3.8f));
            Spawn(PlayerRole.Van, MoveMode.LookOnly, 0f, OnGround(spawn, 0.1f), Quaternion.LookRotation(new Vector3(-1, 0, 0.5f)));
        }

        /// <summary>Gathering points around Kao's village (A market, B old school, C rice mill) and rumour shouters.</summary>
        static WorldLabel[] BuildKaoVillage(Transform world)
        {
            var village = Group("Village", world, Vector3.zero);
            var gathers = new[]
            {
                ("Market", new Vector3(100, 0, 104), "sign_market"),
                ("OldSchool", new Vector3(47, 0, 74), "sign_school"),
                ("RiceMill", new Vector3(7, 0, 119), "sign_mill"),
            };
            var shouters = new List<WorldLabel>();
            for (int i = 0; i < gathers.Length; i++)
            {
                var (name, pos, sign) = gathers[i];
                var g = Group(name, village, OnGround(pos, 0f));
                g.gameObject.AddComponent<GatherPoint>().optionIndex = i;
                Sign(g, new Vector3(0, 0, 4), sign);
                shouters.Add(Shouter(g, new Vector3(3, 0, 3)));
            }
            AddLandmark(village.Find("OldSchool").gameObject, "lm_school", village.Find("OldSchool").position + Vector3.up * 3);
            shouters.Add(Shouter(village, OnGround(KaoVillage + new Vector3(6, 0, 10), 0f)));
            Spawn(PlayerRole.Village, MoveMode.Free, 0f, OnGround(KaoVillage, 0.1f), Quaternion.LookRotation(new Vector3(0, 0, 1)));
            return shouters.ToArray();
        }

        /// <summary>
        /// Each road gets a flood zone on the middle stretch only it uses: deep water unless HQ named this road.
        /// The route markers drive the debug autopilot.
        /// </summary>
        static void BuildKaoRoads(Transform world, Material water)
        {
            var roads = Group("Roads", world, Vector3.zero);
            for (int i = 0; i < KaoRoads.Length; i++)
            {
                var pts = KaoRoads[i];
                var path = Group("Route_" + i, roads, Vector3.zero).gameObject.AddComponent<RoutePath>();
                path.optionIndex = i;
                path.points = System.Array.ConvertAll(pts, p => OnGround(new Vector3(p.x, 0, p.y), 0f));
                Sign(roads, OnGround(Lerp(pts[0], pts[1], 0.2f), 0f), KaoRoadSigns[i]);

                // The flooded stretch: from the second to the third point past the road's start.
                var a = pts[2];
                var b = pts[3];
                Zone(roads, $"Route_{i}_Flood", a, b, 22f, HazardZone.Rule.UnlessSafeRoute, i, water, 0.5f);
            }
            var bridge = GameObject.Find("Bridge_Sec_3");
            if (bridge != null) AddLandmark(bridge, "lm_bridge", new Vector3(3, 2, 134));
        }

        /// <summary>The temple hill: three stairs up (one may be blocked by a landslide) and the safe zone on top.</summary>
        static void BuildKaoHill(Transform world)
        {
            var hill = Group("TempleHill", world, OnGround(KaoTemple, 0f));
            var mud = Mat("Mud", new Color(0.33f, 0.26f, 0.18f));
            var stairs = new[]
            {
                ("WestStairs", new Vector2(33, 206), "sign_bridge"),
                ("SouthStairs", new Vector2(56, 194), "sign_school_ford"),
                ("EastStairs", new Vector2(80, 210), "sign_market_ford"),
            };
            var top = new Vector2(KaoTemple.x, KaoTemple.z);
            for (int i = 0; i < stairs.Length; i++)
            {
                var (name, p, sign) = stairs[i];
                var outward = (p - top).normalized;
                // A short box across the stairs, along the climb.
                Zone(hill, name, p - outward * 5f, p + outward * 5f, 18f, HazardZone.Rule.IfAvoidedCrossing, i, mud, 0.6f);
                Sign(hill, hill.InverseTransformPoint(OnGround(new Vector3(p.x + outward.x * 8f, 0, p.y + outward.y * 8f), 0f)), sign);
            }
            AddLandmark(hill.gameObject, "lm_hill", hill.position + Vector3.up * 6f);
            var safe = Group("SafeZone", world, hill.position);
            var zone = safe.gameObject.AddComponent<SafeZone>();
            zone.radius = 13f;
            zone.minHeight = hill.position.y - 1.5f;
            Sign(safe, new Vector3(-7f, 0, -9f), "sign_hill");
        }

        /// <summary>A hazard box along the segment a-b (ground level), with a slab shown while it is deadly.</summary>
        static void Zone(Transform parent, string name, Vector2 a, Vector2 b, float width, HazardZone.Rule rule, int option, Material mat, float depth)
        {
            float low = float.PositiveInfinity, high = float.NegativeInfinity;
            for (int s = 0; s <= 8; s++)
            {
                float y = GroundY(Lerp(a, b, s / 8f));
                low = Mathf.Min(low, y);
                high = Mathf.Max(high, y);
            }
            var mid = (a + b) * 0.5f;
            var t = Group(name + "_Hazard", parent, new Vector3(mid.x, low - 0.5f, mid.y));
            t.rotation = Quaternion.LookRotation(new Vector3(b.x - a.x, 0, b.y - a.y));
            var zone = t.gameObject.AddComponent<HazardZone>();
            zone.rule = rule;
            zone.optionIndex = option;
            zone.size = new Vector3(width, high - low + 6f, Vector2.Distance(a, b));
            var vis = Prim(PrimitiveType.Cube, "Slab", t, new Vector3(0, high - low + 0.5f + depth - 0.05f, 0), new Vector3(width, 0.1f, zone.size.z), mat);
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            zone.waterVisual = vis.gameObject;
        }

        static void BuildKaoCutscene()
        {
            var root = new GameObject("OpeningCutscene");
            var van = GameObject.Find("RescueVan").transform;
            var shots = new[]
            {
                Shot(root.transform, "Shot1_Village", new Vector3(40, 16, 40), OnGround(KaoVillage, 1f) + new Vector3(0, 0, 18), new Vector3(8, 0, 6), "cut_1", 1f),
                Shot(root.transform, "Shot2_Water", new Vector3(108, 3, 112), new Vector3(130, 0, 150), new Vector3(0, 0, 8), "cut_2", -1f),
                Shot(root.transform, "Shot3_Van", van.TransformPoint(new Vector3(4, 2.4f, -9)), van.TransformPoint(new Vector3(0, 1.4f, -1)), new Vector3(0, 0, 2), "cut_3", 1.6f),
                Shot(root.transform, "Shot4_Tent", OnGround(KaoTent, 3f) + new Vector3(10, 0, 8), OnGround(KaoTent, 1.6f), new Vector3(-3, 0, 2), "cut_4", -1f),
                Shot(root.transform, "Shot5_Hill", new Vector3(40, 18, 168), OnGround(KaoTemple, 3f), new Vector3(0, 3, 6), "cut_5", -1f),
            };
            root.AddComponent<CutscenePlayer>().shots = shots;
            Rescuer(root.transform, van.TransformPoint(new Vector3(0.6f, 0, -4.4f)), van.eulerAngles.y + 180f);
            Rescuer(root.transform, OnGround(KaoTent + new Vector3(1.2f, 0, 1f), 0f), 220f);
        }

        /// <summary>
        /// Kao's terrain layers use textures of unknown origin (web images, a stock preview, pack roof textures).
        /// The copy gets its own terrain data with the same paint, retextured with our CC0 Poly Haven set.
        /// </summary>
        static void ReplaceKaoTerrainTextures(UnityEngine.SceneManagement.Scene scene)
        {
            const string dir = SceneDir + "/KaoTerrain";
            AssetDatabase.DeleteAsset(dir);
            AssetDatabase.CreateFolder(SceneDir, "KaoTerrain");
            // Look of each original texture -> closest CC0 texture.
            var swap = new Dictionary<string, string>
            {
                { "360_F_259143134_BdNxyMFtkC0osFtewc0JzLIMlyZbselt", "brown_mud_leaves_01" },
                { "89e8471b3f46a5a31f04381e93de727d", "forrest_ground_01" },
                { "950cdb0c0c813efdbc3e43ba0bb5b6a3", "rocky_terrain_02" },
                { "Red-Dirt-Road-texture", "grass_path_2" },
            };
            var layers = new Dictionary<TerrainLayer, TerrainLayer>();
            var datas = new Dictionary<TerrainData, TerrainData>();
            foreach (var terrain in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
            {
                if (terrain.gameObject.scene != scene) continue;
                var src = terrain.terrainData;
                if (!datas.TryGetValue(src, out var copy))
                {
                    string path = $"{dir}/{src.name}.asset";
                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(src), path);
                    copy = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                    var old = copy.terrainLayers;
                    var mapped = new TerrainLayer[old.Length];
                    for (int i = 0; i < old.Length; i++)
                    {
                        var o = old[i];
                        if (o == null) continue;
                        if (!layers.TryGetValue(o, out var n))
                        {
                            string tex = o.diffuseTexture != null && swap.TryGetValue(o.diffuseTexture.name, out var t) ? t : "brown_mud_leaves_01";
                            n = new TerrainLayer
                            {
                                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Textures/PolyHaven/{tex}_diff_1k.jpg"),
                                tileSize = o.tileSize,
                                smoothness = 0.3f,
                            };
                            AssetDatabase.CreateAsset(n, $"{dir}/Layer_{o.name}.terrainlayer");
                            layers[o] = n;
                        }
                        mapped[i] = n;
                    }
                    copy.terrainLayers = mapped;
                    EditorUtility.SetDirty(copy);
                    datas[src] = copy;
                }
                terrain.terrainData = copy;
                var col = terrain.GetComponent<TerrainCollider>();
                if (col != null) col.terrainData = copy;
            }
        }

        static Vector3 Lerp(Vector2 a, Vector2 b, float t)
        {
            var p = Vector2.Lerp(a, b, t);
            return new Vector3(p.x, 0, p.y);
        }

        static float GroundY(Vector3 p) => GroundY(new Vector2(p.x, p.z));

        /// <summary>Terrain height at a point of Kao's tiled terrain.</summary>
        static float GroundY(Vector2 p)
        {
            foreach (var t in Terrain.activeTerrains)
            {
                var o = t.transform.position;
                var s = t.terrainData.size;
                if (p.x >= o.x && p.x <= o.x + s.x && p.y >= o.z && p.y <= o.z + s.z)
                    return o.y + t.SampleHeight(new Vector3(p.x, 0, p.y));
            }
            return 0f;
        }

        static Vector3 OnGround(Vector3 p, float lift) => new Vector3(p.x, GroundY(p) + lift, p.z);
    }
}
