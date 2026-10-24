using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Motion for the menus: punchy selection, cascading entrances, diagonal band wipes, a drifting
    /// backdrop word and wind-blown rain streaks. Everything runs on unscaled time and settles
    /// instantly with reduce motion.
    /// </summary>
    static class Motion
    {
        public static float Dt => Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        /// <summary>Overshoots and settles back, for slam-in entrances.</summary>
        public static float OutBack(float t, float s = 1.70158f)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

        /// <summary>Damped spring step; a stiff spring with light damping gives a small overshoot.</summary>
        public static void Spring(ref float value, ref float velocity, float target, float stiffness, float damping)
        {
            float dt = Dt;
            velocity += (target - value) * stiffness * dt;
            velocity *= Mathf.Exp(-damping * dt);
            value += velocity * dt;
        }
    }

    /// <summary>Slants a graphic into a parallelogram (shifts vertices sideways by height).</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class UiSkew : BaseMeshEffect
    {
        public float skew = 0.35f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            float mid = graphic.rectTransform.rect.center.y;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.x += (v.position.y - mid) * skew;
                vh.SetUIVertex(v, i);
            }
        }
    }

    /// <summary>
    /// A big italic menu word. When selected a red slash snaps out behind it with a yellow echo a beat
    /// later, the word grows and keeps a slight wobble; the other words stay small and dim.
    /// Hovering with the mouse selects it, so mouse and keyboard show the same highlight.
    /// </summary>
    public sealed class PunchItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        public RectTransform slash, echo, word;
        public TextMeshProUGUI label, shadow;

        float focus, focusVel, lag, lagVel, wobble;
        bool selected;
        Quaternion rest;

        void Awake() => rest = transform.localRotation;

        void OnDisable()
        {
            selected = false;
            focus = focusVel = lag = lagVel = 0f;
            Apply();
        }

        public void OnSelect(BaseEventData _) => selected = true;
        public void OnDeselect(BaseEventData _) => selected = false;

        public void OnPointerEnter(PointerEventData _)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        void Update()
        {
            float target = selected ? 1f : 0f;
            if (ComfortSettings.ReduceMotion)
            {
                focus = lag = target;
                focusVel = lagVel = 0f;
            }
            else
            {
                Motion.Spring(ref focus, ref focusVel, target, 420f, 16f);
                Motion.Spring(ref lag, ref lagVel, target, 220f, 14f);
                wobble += Motion.Dt;
            }
            Apply();
        }

        void Apply()
        {
            slash.localScale = new Vector3(Mathf.Max(0f, focus), 1f, 1f);
            echo.localScale = new Vector3(Mathf.Max(0f, lag), 1f, 1f);
            float grow = 1f + 0.22f * focus;
            word.localScale = new Vector3(grow, grow, 1f);
            word.anchoredPosition = new Vector2(28f * focus, 0f);
            label.color = Color.Lerp(new Color(0.78f, 0.82f, 0.88f, 0.85f), UiKit.White, focus);
            shadow.color = new Color(UiKit.Ink.r, UiKit.Ink.g, UiKit.Ink.b, Mathf.Clamp01(focus));
            float tilt = ComfortSettings.ReduceMotion ? 0f : Mathf.Sin(wobble * 4.5f) * 1.2f * Mathf.Clamp01(focus);
            transform.localRotation = rest * Quaternion.Euler(0f, 0f, tilt - 3f * focus);
        }
    }

    /// <summary>A small springy pop and tilt on any slab button when it is hovered or selected.</summary>
    public sealed class HoverPop : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        float value, velocity;
        bool hover, selected;
        Vector3 restScale;
        Quaternion restRotation;

        void Awake()
        {
            restScale = transform.localScale;
            restRotation = transform.localRotation;
        }

        void OnDisable()
        {
            hover = selected = false;
            value = velocity = 0f;
            transform.localScale = restScale;
            transform.localRotation = restRotation;
        }

        public void OnSelect(BaseEventData _) => selected = true;
        public void OnDeselect(BaseEventData _) => selected = false;
        public void OnPointerEnter(PointerEventData _) => hover = true;
        public void OnPointerExit(PointerEventData _) => hover = false;

        void Update()
        {
            float target = hover || selected ? 1f : 0f;
            if (ComfortSettings.ReduceMotion) { value = target; velocity = 0f; }
            else Motion.Spring(ref value, ref velocity, target, 500f, 18f);
            transform.localScale = restScale * (1f + 0.06f * value);
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, -1.5f * value);
        }
    }

    /// <summary>
    /// Every time the screen opens its parts fly in from the left one after another, spun a little,
    /// and slam into place with an overshoot.
    /// </summary>
    public sealed class CascadeIn : MonoBehaviour
    {
        public List<RectTransform> targets = new List<RectTransform>();
        public Vector2 from = new Vector2(-900f, 140f);
        public float stagger = 0.045f, duration = 0.34f;

        readonly Dictionary<RectTransform, (Vector2 pos, float rot)> rest = new Dictionary<RectTransform, (Vector2, float)>();

        void OnEnable()
        {
            foreach (var t in targets)
                if (t != null && !rest.ContainsKey(t)) rest[t] = (t.anchoredPosition, t.localEulerAngles.z);
            if (ComfortSettings.ReduceMotion) { Settle(); return; }
            StartCoroutine(Play());
        }

        void OnDisable() => Settle();

        void Settle()
        {
            StopAllCoroutines();
            foreach (var t in targets)
                if (t != null && rest.TryGetValue(t, out var r)) Place(t, r, 1f);
        }

        IEnumerator Play()
        {
            foreach (var t in targets)
                if (t != null) Place(t, rest[t], 0f);
            float end = stagger * targets.Count + duration;
            for (float time = 0f; time < end; time += Motion.Dt)
            {
                for (int i = 0; i < targets.Count; i++)
                    if (targets[i] != null) Place(targets[i], rest[targets[i]], Mathf.Clamp01((time - i * stagger) / duration));
                yield return null;
            }
            Settle();
        }

        void Place(RectTransform t, (Vector2 pos, float rot) r, float k)
        {
            float e = k <= 0f ? 0f : Motion.OutBack(k, 1.4f);
            t.anchoredPosition = r.pos + from * (1f - e);
            t.localRotation = Quaternion.Euler(0f, 0f, r.rot + 22f * (1f - e));
        }
    }

    /// <summary>Three slanted colour bands (yellow, red, ink) sweep across the screen one after another.</summary>
    public sealed class BandWipe : MonoBehaviour
    {
        readonly RectTransform[] bands = new RectTransform[3];

        public static BandWipe Create(Transform canvas)
        {
            var root = UiKit.Stretch("BandWipe", canvas);
            var wipe = root.gameObject.AddComponent<BandWipe>();
            var colors = new[] { UiKit.Yellow, UiKit.Red, UiKit.Ink };
            for (int i = 0; i < 3; i++)
            {
                var band = UiKit.Rect("Band" + i, root, new Vector2(0.5f, -0.2f), new Vector2(0.5f, 1.2f), new Vector2(-260f, 0f), new Vector2(260f, 0f));
                var img = band.gameObject.AddComponent<Image>();
                img.color = colors[i];
                img.raycastTarget = false;
                band.gameObject.AddComponent<UiSkew>().skew = -0.45f;
                band.gameObject.SetActive(false);
                wipe.bands[i] = band;
            }
            return wipe;
        }

        public void Play()
        {
            transform.SetAsLastSibling();
            if (ComfortSettings.ReduceMotion || !isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(Sweep());
        }

        IEnumerator Sweep()
        {
            float width = ((RectTransform)transform).rect.width;
            const float duration = 0.38f, stagger = 0.06f;
            foreach (var b in bands) b.gameObject.SetActive(true);
            for (float t = 0f; t < duration + stagger * 2f; t += Motion.Dt)
            {
                for (int i = 0; i < bands.Length; i++)
                {
                    float k = Motion.OutCubic((t - i * stagger) / duration);
                    bands[i].anchoredPosition = new Vector2(Mathf.Lerp(-width, width, k), 0f);
                }
                yield return null;
            }
            foreach (var b in bands) b.gameObject.SetActive(false);
        }
    }

    /// <summary>A huge faint word behind a screen that slams in and then drifts slowly sideways.</summary>
    public sealed class DriftWord : MonoBehaviour
    {
        RectTransform rt;
        float time;

        public static DriftWord Create(Transform parent, string locKey)
        {
            var box = UiKit.Rect("DriftWord", parent, new Vector2(-0.4f, 0.05f), new Vector2(1.6f, 0.75f), Vector2.zero, Vector2.zero);
            box.localRotation = Quaternion.Euler(0, 0, 9f);
            var label = UiKit.Label(box, locKey, 340, new Color(1f, 1f, 1f, 0.08f), TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold | FontStyles.Italic;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return box.gameObject.AddComponent<DriftWord>();
        }

        void Awake() => rt = (RectTransform)transform;
        void OnEnable() => time = 0f;

        void Update()
        {
            if (ComfortSettings.ReduceMotion) { rt.localScale = Vector3.one; rt.anchoredPosition = Vector2.zero; return; }
            time += Motion.Dt;
            float slam = time < 0.35f ? Mathf.Lerp(1.7f, 1f, Motion.OutCubic(time / 0.35f)) : 1f;
            rt.localScale = new Vector3(slam, slam, 1f);
            rt.anchoredPosition = new Vector2(Mathf.Sin(time * 0.12f) * 220f, 0f);
        }
    }

    /// <summary>Wind-blown rain streaks across the backdrop, plus a few slow dark slabs for depth.</summary>
    public sealed class StormStreaks : MonoBehaviour
    {
        struct Streak { public RectTransform rt; public float speed; }
        readonly List<Streak> streaks = new List<Streak>();
        RectTransform area;
        System.Random rng;

        public static StormStreaks Create(Transform parent, int count = 34)
        {
            var area = UiKit.Stretch("StormStreaks", parent);
            var s = area.gameObject.AddComponent<StormStreaks>();
            s.area = area;
            s.rng = new System.Random(11);
            for (int i = 0; i < count; i++)
            {
                bool slab = i < 5;
                var rt = UiKit.Rect(slab ? "Slab" : "Streak", area, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                rt.sizeDelta = slab ? new Vector2(220f + i * 60f, 900f) : new Vector2(3f, 50f + (float)s.rng.NextDouble() * 90f);
                rt.localRotation = Quaternion.Euler(0, 0, -24f);
                var img = rt.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = slab ? new Color(0f, 0f, 0f, 0.12f) : new Color(1f, 1f, 1f, 0.08f + (float)s.rng.NextDouble() * 0.14f);
                if (slab) rt.gameObject.AddComponent<UiSkew>().skew = 0.3f;
                s.streaks.Add(new Streak { rt = rt, speed = slab ? 30f + i * 12f : 700f + (float)s.rng.NextDouble() * 700f });
            }
            s.Scatter();
            return s;
        }

        void Scatter()
        {
            var size = area.rect.size;
            if (size.x < 1f) size = new Vector2(1920f, 1080f);
            foreach (var s in streaks)
                s.rt.anchoredPosition = new Vector2(((float)rng.NextDouble() - 0.5f) * size.x * 1.3f, ((float)rng.NextDouble() - 0.5f) * size.y * 1.3f);
        }

        void Update()
        {
            if (ComfortSettings.ReduceMotion) return;
            var size = area.rect.size;
            // Falling down and to the left, along the streak angle.
            var dir = new Vector2(-0.42f, -0.9f);
            foreach (var s in streaks)
            {
                var p = s.rt.anchoredPosition + dir * (s.speed * Motion.Dt);
                if (p.y < -size.y * 0.7f || p.x < -size.x * 0.7f)
                    p = new Vector2(((float)rng.NextDouble() - 0.2f) * size.x * 1.2f, size.y * 0.65f);
                s.rt.anchoredPosition = p;
            }
        }
    }
}
