using System.Collections.Generic;
using System.IO;
using System.Linq;
using SayItAgain.Net;
using SayItAgain.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SayItAgain.EditorTools
{
    /// <summary>Generates the lobby scene, the gameplay test scene and the prefabs they need. Safe to re-run.</summary>
    public static class SayItAgainSceneBuilder
    {
        const string Root = "Assets/SayItAgain";
        const string PrefabDir = Root + "/Prefabs";
        const string MaterialDir = Root + "/Materials";
        const string SceneDir = Root + "/Scenes";
        const string LobbyScene = SceneDir + "/Lobby.unity";
        const string GameplayScene = SceneDir + "/Gameplay_Test.unity";
        const string FontSource = "Assets/Fonts/NotoSansThai/NotoSansThai.ttf";
        const string FontAssetPath = "Assets/Resources/Fonts/NotoSansThai SDF.asset";

        [MenuItem("Tools/Say It Again/Build Phase 1 Scenes")]
        public static void BuildAll()
        {
            EnsureFolders();
            BuildFontAsset();
            var rig = BuildPlayerRig();
            var roleManager = BuildRoleManagerPrefab();
            BuildLobbyScene(roleManager);
            BuildGameplayScene(rig);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[SayItAgain] Phase 1 scenes built");
        }

        static void EnsureFolders()
        {
            foreach (var dir in new[] { Root, PrefabDir, MaterialDir, SceneDir, "Assets/Resources/Fonts" })
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

        static GameObject BuildPlayerRig()
        {
            var root = new GameObject("PlayerRig");
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0, 0.9f, 0);

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

        static GameObject BuildRoleManagerPrefab()
        {
            var go = new GameObject("RoleManager");
            go.AddComponent<NetworkObject>();
            go.AddComponent<RoleManager>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/RoleManager.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void BuildLobbyScene(GameObject roleManagerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("LobbyCamera", typeof(Camera));
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Navy;

            var nmGo = new GameObject("NetworkManager");
            var transport = nmGo.AddComponent<UnityTransport>();
            var nm = nmGo.AddComponent<NetworkManager>();
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = true };

            var list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            list.Add(new NetworkPrefab { Prefab = roleManagerPrefab });
            const string listPath = PrefabDir + "/SayItAgainNetworkPrefabs.asset";
            AssetDatabase.DeleteAsset(listPath);
            AssetDatabase.CreateAsset(list, listPath);
            nm.NetworkConfig.Prefabs.NetworkPrefabsLists = new List<NetworkPrefabsList> { list };

            var session = nmGo.AddComponent<SessionService>();
            session.roleManagerPrefab = roleManagerPrefab.GetComponent<NetworkObject>();

            new GameObject("LobbyUI").AddComponent<LobbyUI>();

            EditorSceneManager.SaveScene(scene, LobbyScene);
        }

        static void BuildGameplayScene(GameObject rigPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            // The rig brings its own camera.
            Object.DestroyImmediate(Camera.main.gameObject);
            var sun = Object.FindAnyObjectByType<Light>();
            sun.intensity = 0.6f;
            sun.color = new Color(0.75f, 0.8f, 0.9f);

            var ground = Mat("Ground", new Color(0.28f, 0.33f, 0.24f));
            var mud = Mat("Mud", new Color(0.33f, 0.26f, 0.18f));
            var white = Mat("VanWhite", new Color(0.9f, 0.9f, 0.88f));
            var red = Mat("AlertRed", UiKit.Red);
            var canvas = Mat("TentCanvas", new Color(0.85f, 0.72f, 0.35f));
            var wood = Mat("Wood", new Color(0.45f, 0.32f, 0.2f));
            var leaf = Mat("Leaf", new Color(0.18f, 0.35f, 0.18f));
            var concrete = Mat("Concrete", new Color(0.55f, 0.55f, 0.55f));

            var world = new GameObject("World").transform;
            Prim(PrimitiveType.Plane, "Ground", world, Vector3.zero, new Vector3(60, 1, 60), ground);

            // Van: open rear tailgate, player stands behind it.
            var van = new GameObject("RescueVan").transform;
            van.SetParent(world);
            van.position = new Vector3(-150, 0, -40);
            Prim(PrimitiveType.Cube, "Body", van, new Vector3(0, 1.3f, 0), new Vector3(2.2f, 2.2f, 5.2f), white);
            Prim(PrimitiveType.Cube, "Stripe", van, new Vector3(0, 1.3f, 0), new Vector3(2.25f, 0.3f, 5.25f), red);
            Prim(PrimitiveType.Cube, "TailgateOpen", van, new Vector3(0, 2.7f, -3f), new Vector3(2.1f, 0.08f, 1.6f), white)
                .localRotation = Quaternion.Euler(-15, 0, 0);
            Spawn(PlayerRole.Van, MoveMode.LookOnly, 0f, van.position + new Vector3(0, 0, -3.6f), Quaternion.LookRotation(Vector3.right + Vector3.forward * 0.3f));

            // Tent: walk inside a small radius.
            var tent = new GameObject("AidTent").transform;
            tent.SetParent(world);
            tent.position = new Vector3(0, 0, 160);
            Prim(PrimitiveType.Cube, "Floor", tent, new Vector3(0, 0.02f, 0), new Vector3(9, 0.04f, 7), wood);
            Prim(PrimitiveType.Cube, "RoofLeft", tent, new Vector3(-2.3f, 2.2f, 0), new Vector3(0.1f, 5.4f, 7.2f), canvas).localRotation = Quaternion.Euler(0, 0, -35);
            Prim(PrimitiveType.Cube, "RoofRight", tent, new Vector3(2.3f, 2.2f, 0), new Vector3(0.1f, 5.4f, 7.2f), canvas).localRotation = Quaternion.Euler(0, 0, 35);
            Prim(PrimitiveType.Cube, "MapTable", tent, new Vector3(0, 0.45f, 1.5f), new Vector3(2f, 0.9f, 1f), wood);
            Spawn(PlayerRole.Tent, MoveMode.Confined, 3f, tent.position + new Vector3(0, 0.05f, -1f), Quaternion.identity);

            // Village: houses on low ground, free roam.
            var village = new GameObject("Village").transform;
            village.SetParent(world);
            village.position = new Vector3(120, 0, -30);
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3((i % 3) * 12 - 12, 1.5f, (i / 3) * 14 - 7);
                Prim(PrimitiveType.Cube, "House" + i, village, p, new Vector3(6, 3, 6), wood);
            }
            Prim(PrimitiveType.Cube, "MudRoad", village, new Vector3(0, 0.02f, 0), new Vector3(4, 0.04f, 80), mud);
            Spawn(PlayerRole.Village, MoveMode.Free, 0f, village.position + new Vector3(0, 0.1f, -20f), Quaternion.identity);

            // Landmarks for the binoculars.
            var hill = Prim(PrimitiveType.Sphere, "HighHill", world, new Vector3(200, -10, 150), new Vector3(110, 60, 110), ground);
            AddLandmark(hill.gameObject, "lm_hill", new Vector3(200, 20, 150));
            var tree = new GameObject("BigRainTree").transform;
            tree.SetParent(world);
            tree.position = new Vector3(90, 0, 50);
            Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 4, 0), new Vector3(1.2f, 4, 1.2f), wood);
            Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0, 10, 0), new Vector3(12, 7, 12), leaf);
            AddLandmark(tree.gameObject, "lm_bigtree", tree.position + Vector3.up * 8);
            var bridge = Prim(PrimitiveType.Cube, "RiverBridge", world, new Vector3(50, 1, -80), new Vector3(30, 0.6f, 5), concrete);
            AddLandmark(bridge.gameObject, "lm_bridge", bridge.position);
            var towerRoot = new GameObject("WaterTower").transform;
            towerRoot.SetParent(world);
            towerRoot.position = new Vector3(-40, 0, 40);
            Prim(PrimitiveType.Cylinder, "Leg", towerRoot, new Vector3(0, 6, 0), new Vector3(0.8f, 6, 0.8f), concrete);
            Prim(PrimitiveType.Cylinder, "Tank", towerRoot, new Vector3(0, 14, 0), new Vector3(6, 2.5f, 6), red);
            AddLandmark(towerRoot.gameObject, "lm_tower", towerRoot.position + Vector3.up * 14);

            var boot = new GameObject("GameplayBootstrap").AddComponent<GameplayBootstrap>();
            boot.playerRigPrefab = rigPrefab;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            RenderSettings.fogColor = new Color(0.42f, 0.47f, 0.52f);

            EditorSceneManager.SaveScene(scene, GameplayScene);
        }

        static void RegisterScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != LobbyScene && s.path != GameplayScene).ToList();
            scenes.Add(new EditorBuildSettingsScene(LobbyScene, true));
            scenes.Add(new EditorBuildSettingsScene(GameplayScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
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

        static Material Mat(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.55f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
