using System.Collections.Generic;
using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// M opens a top-down map of the level: the three roads, the gathering points, the hill stairs,
    /// the safe hill, the van and the tent, and an arrow for where you are and which way you face.
    /// The picture is rendered once each time the map opens; only the arrow moves.
    /// </summary>
    public sealed class MapOverlay : MonoBehaviour
    {
        const int Size = 1024;
        static readonly Color[] RoadColors = { UiKit.Yellow, new Color(0.45f, 0.85f, 1f), UiKit.White };

        InputAction toggle;
        GameObject root;
        RectTransform mapRect, arrow;
        RawImage picture;
        Camera mapCam;
        RenderTexture texture;
        bool built;

        public static bool IsOpen { get; private set; }

        void Awake() => toggle = new InputAction("Map", InputActionType.Button, "<Keyboard>/m");
        void OnEnable() => toggle.Enable();
        void OnDisable() => toggle.Disable();

        void OnDestroy()
        {
            toggle.Dispose();
            IsOpen = false;
            if (texture != null) texture.Release();
        }

        void Update()
        {
            if (toggle.WasPressedThisFrame() && !PauseMenu.IsOpen) SetOpen(!IsOpen);
            if (IsOpen && PauseMenu.IsOpen) SetOpen(false);
            if (IsOpen) UpdateArrow();
        }

        void SetOpen(bool open)
        {
            if (open && !Build()) return;
            IsOpen = open;
            root.SetActive(open);
            if (open) Snapshot();
            UiSfx.Play(open ? UiSound.Confirm : UiSound.Back);
        }

        // ---------- Building ----------

        /// <summary>Builds the overlay the first time, once the level and its markers exist.</summary>
        bool Build()
        {
            if (built) return true;
            var match = MatchManager.Instance;
            if (match == null || match.Level == null || Terrain.activeTerrains.Length == 0) return false;
            built = true;

            var canvas = UiKit.CreateCanvas("MapOverlay", 40).transform;
            canvas.SetParent(transform, false);
            root = UiKit.Panel("Dim", canvas, new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.82f)).gameObject;
            var frame = UiKit.Rect("Frame", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480f, -500f), new Vector2(480f, 460f));
            frame.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            mapRect = UiKit.Rect("Map", frame, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            picture = mapRect.gameObject.AddComponent<RawImage>();
            var title = UiKit.Label(UiKit.Rect("Title", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480f, 465f), new Vector2(480f, 535f)), "map_title", 40, UiKit.Yellow, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;

            SetupCamera();
            AddMarkers(match.Level.orders);

            arrow = UiKit.Rect("You", mapRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), new Vector2(14f, 14f));
            arrow.gameObject.AddComponent<Image>().color = UiKit.Red;
            var tip = UiKit.Rect("Tip", arrow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-4f, -2f), new Vector2(4f, 22f));
            tip.gameObject.AddComponent<Image>().color = UiKit.White;
            Tag(arrow, new Vector2(0f, -34f), "map_you", UiKit.Red);
            root.SetActive(false);
            return true;
        }

        /// <summary>An overhead orthographic camera framing every terrain tile, used only for snapshots.</summary>
        void SetupCamera()
        {
            var bounds = new Bounds(Terrain.activeTerrains[0].transform.position, Vector3.zero);
            foreach (var t in Terrain.activeTerrains)
            {
                bounds.Encapsulate(t.transform.position);
                bounds.Encapsulate(t.transform.position + t.terrainData.size);
            }
            var go = new GameObject("MapCamera");
            go.transform.SetParent(transform, false);
            mapCam = go.AddComponent<Camera>();
            mapCam.enabled = false;
            mapCam.orthographic = true;
            mapCam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z);
            mapCam.transform.SetPositionAndRotation(new Vector3(bounds.center.x, 200f, bounds.center.z), Quaternion.Euler(90f, 0f, 0f));
            mapCam.nearClipPlane = 1f;
            mapCam.farClipPlane = 400f;
            mapCam.clearFlags = CameraClearFlags.SolidColor;
            mapCam.backgroundColor = UiKit.Navy;
            texture = new RenderTexture(Size, Size, 24);
            mapCam.targetTexture = texture;
            picture.texture = texture;
        }

        void Snapshot()
        {
            // No fog from above, or the far side of the map is a grey blur.
            bool fog = RenderSettings.fog;
            RenderSettings.fog = false;
            mapCam.Render();
            RenderSettings.fog = fog;
        }

        void AddMarkers(OrderData orders)
        {
            foreach (var road in FindObjectsByType<RoutePath>(FindObjectsSortMode.None))
            {
                var color = RoadColors[road.optionIndex % RoadColors.Length];
                for (int i = 1; i < road.points.Length; i++) Line(road.points[i - 1], road.points[i], color);
                Label(Vector3.Lerp(road.points[1], road.points[2], 0.5f), orders.routeKeys[road.optionIndex], color);
            }
            foreach (var g in FindObjectsByType<GatherPoint>(FindObjectsSortMode.None))
                Pin(g.transform.position, orders.gatherKeys[g.optionIndex], UiKit.White);
            foreach (var z in HazardZone.All)
                if (z.rule == HazardZone.Rule.IfAvoidedCrossing) Pin(z.transform.position, orders.avoidKeys[z.optionIndex], new Color(1f, 0.6f, 0.3f));
            if (SafeZone.Instance != null) Pin(SafeZone.Instance.transform.position, "lm_hill", new Color(0.4f, 1f, 0.5f));
            foreach (var spawn in FindObjectsByType<RoleSpawnPoint>(FindObjectsSortMode.None))
                if (spawn.role != PlayerRole.Village) Pin(spawn.transform.position, spawn.role.NameKey(), UiKit.Yellow);
        }

        Vector2 ToMap(Vector3 world)
        {
            var v = mapCam.WorldToViewportPoint(world);
            var size = mapRect.rect.size;
            if (size.x < 1f) size = new Vector2(944f, 944f);
            return new Vector2((v.x - 0.5f) * size.x, (v.y - 0.5f) * size.y);
        }

        void Line(Vector3 a, Vector3 b, Color color)
        {
            Vector2 pa = ToMap(a), pb = ToMap(b);
            var line = UiKit.Rect("Road", mapRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            line.sizeDelta = new Vector2(Vector2.Distance(pa, pb), 7f);
            line.anchoredPosition = (pa + pb) * 0.5f;
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(pb.y - pa.y, pb.x - pa.x) * Mathf.Rad2Deg);
            var img = line.gameObject.AddComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.85f);
            img.raycastTarget = false;
        }

        void Pin(Vector3 world, string key, Color color)
        {
            var dot = UiKit.Rect("Pin", mapRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-9f, -9f), new Vector2(9f, 9f));
            dot.anchoredPosition = ToMap(world);
            dot.localRotation = Quaternion.Euler(0, 0, 45f);
            dot.gameObject.AddComponent<Image>().color = color;
            Label(world, key, color, 26f);
        }

        void Label(Vector3 world, string key, Color color, float above = 0f)
        {
            var box = UiKit.Rect("Label", mapRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-110f, -16f), new Vector2(110f, 16f));
            box.anchoredPosition = ToMap(world) + new Vector2(0f, above);
            Tag(box, Vector2.zero, key, color);
        }

        static void Tag(RectTransform parent, Vector2 offset, string key, Color color)
        {
            var bg = UiKit.Rect("Tag", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-100f, -15f), new Vector2(100f, 15f));
            bg.anchoredPosition = offset;
            var back = bg.gameObject.AddComponent<Image>();
            back.color = new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, 0.7f);
            back.raycastTarget = false;
            var label = UiKit.Label(bg, key, 20, color, TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void UpdateArrow()
        {
            var rig = GameplayBootstrap.LocalRig;
            if (rig == null) return;
            arrow.anchoredPosition = ToMap(rig.transform.position);
            // Map up is world +Z, so the arrow turns opposite to the player's yaw.
            arrow.localRotation = Quaternion.Euler(0, 0, -rig.transform.eulerAngles.y);
            arrow.GetChild(1).localRotation = Quaternion.Inverse(arrow.localRotation);
        }
    }
}
