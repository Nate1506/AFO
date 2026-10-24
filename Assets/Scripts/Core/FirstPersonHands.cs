using SayItAgain.UI;
using SayItAgain.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain
{
    /// <summary>
    /// First-person hands built from simple shapes: the right hand holds a handheld radio, the left hand points.
    /// Procedural poses: idle (with walk sway), raise to the mouth while talking, lower for binoculars,
    /// the card or pause, and point when the village player calls villagers (E).
    /// </summary>
    public sealed class FirstPersonHands : MonoBehaviour
    {
        public Color sleeve = new Color(1f, 0.5f, 0.1f);
        public Color skin = new Color(0.78f, 0.58f, 0.44f);

        Transform right, left;
        Material ledMat;
        FirstPersonController controller;
        Binoculars binoculars;
        CardPanel card;
        InputAction point;
        float pointUntil, swayTime;

        struct Pose { public Vector3 pos; public Vector3 rot; public Pose(Vector3 p, Vector3 r) { pos = p; rot = r; } }

        static readonly Pose RightIdle = new Pose(new Vector3(0.24f, -0.27f, 0.42f), new Vector3(8f, -12f, -4f));
        static readonly Pose RightTalk = new Pose(new Vector3(0.1f, -0.12f, 0.3f), new Vector3(-18f, -30f, 12f));
        static readonly Pose RightAway = new Pose(new Vector3(0.3f, -0.65f, 0.3f), new Vector3(40f, -10f, 0f));
        static readonly Pose LeftAway = new Pose(new Vector3(-0.3f, -0.7f, 0.3f), new Vector3(40f, 10f, 0f));
        static readonly Pose LeftPoint = new Pose(new Vector3(-0.16f, -0.16f, 0.55f), new Vector3(-6f, 6f, 0f));

        void Awake()
        {
            controller = GetComponentInParent<FirstPersonController>();
            binoculars = GetComponentInParent<Binoculars>();
            point = new InputAction("Point", InputActionType.Button, "<Keyboard>/e");
            right = BuildRadioHand();
            left = BuildPointingHand();
        }

        void OnEnable() => point.Enable();
        void OnDisable() => point.Disable();
        void OnDestroy() => point.Dispose();

        void Update()
        {
            if (card == null) card = FindAnyObjectByType<CardPanel>();
            bool blocked = (binoculars != null && binoculars.ZoomAmount > 0.2f) || (card != null && card.IsOpen) || PauseMenu.IsOpen;
            bool talking = VoiceService.Instance != null && VoiceService.Instance.IsTransmitting;
            if (point.WasPressedThisFrame() && !blocked && GameplayBootstrapRoleIsVillage()) pointUntil = Time.time + 0.9f;

            var rightTarget = blocked ? RightAway : talking ? RightTalk : RightIdle;
            var leftTarget = !blocked && Time.time < pointUntil ? LeftPoint : LeftAway;

            // Gentle sway while walking (off with reduce motion).
            Vector3 sway = Vector3.zero;
            if (controller != null && controller.IsMoving && !ComfortSettings.ReduceMotion)
            {
                swayTime += Time.deltaTime * 8f;
                sway = new Vector3(Mathf.Cos(swayTime * 0.5f) * 0.01f, Mathf.Sin(swayTime) * 0.012f, 0f);
            }

            float k = 1f - Mathf.Exp(-(ComfortSettings.ReduceMotion ? 30f : 12f) * Time.deltaTime);
            Blend(right, rightTarget, sway, k);
            Blend(left, leftTarget, sway, k);

            // Red light while transmitting, dim green while listening.
            ledMat.SetColor("_BaseColor", talking ? new Color(1f, 0.15f, 0.1f) : new Color(0.1f, 0.4f, 0.15f));
        }

        static void Blend(Transform t, Pose target, Vector3 sway, float k)
        {
            t.localPosition = Vector3.Lerp(t.localPosition, target.pos + sway, k);
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(target.rot), k);
        }

        static bool GameplayBootstrapRoleIsVillage() => Net.GameplayBootstrap.LocalRole == PlayerRole.Village;

        // ---------- Geometry ----------

        Transform BuildRadioHand()
        {
            var root = new GameObject("RightHand").transform;
            root.SetParent(transform, false);
            root.localPosition = RightAway.pos;
            var sleeveMat = Mat(sleeve);
            var skinMat = Mat(skin);
            var radioMat = Mat(new Color(0.12f, 0.13f, 0.14f));
            ledMat = Mat(Color.green);

            Part(PrimitiveType.Capsule, root, new Vector3(0.05f, -0.06f, -0.2f), new Vector3(70f, 0f, 0f), new Vector3(0.075f, 0.16f, 0.075f), sleeveMat);
            Part(PrimitiveType.Sphere, root, new Vector3(0.015f, -0.02f, -0.02f), Vector3.zero, new Vector3(0.085f, 0.075f, 0.1f), skinMat);
            // Radio body, antenna, speaker grille and status light.
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.05f, 0.01f), Vector3.zero, new Vector3(0.055f, 0.13f, 0.03f), radioMat);
            Part(PrimitiveType.Cylinder, root, new Vector3(0.015f, 0.15f, 0.01f), Vector3.zero, new Vector3(0.008f, 0.05f, 0.008f), radioMat);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.07f, -0.0155f), Vector3.zero, new Vector3(0.04f, 0.04f, 0.002f), Mat(new Color(0.3f, 0.32f, 0.34f)));
            Part(PrimitiveType.Sphere, root, new Vector3(-0.014f, 0.108f, -0.012f), Vector3.zero, new Vector3(0.008f, 0.008f, 0.008f), ledMat);
            // Thumb resting on the push-to-talk button.
            Part(PrimitiveType.Capsule, root, new Vector3(-0.03f, 0.02f, -0.02f), new Vector3(0f, 0f, 20f), new Vector3(0.022f, 0.03f, 0.022f), skinMat);
            return root;
        }

        Transform BuildPointingHand()
        {
            var root = new GameObject("LeftHand").transform;
            root.SetParent(transform, false);
            root.localPosition = LeftAway.pos;
            var skinMat = Mat(skin);
            Part(PrimitiveType.Capsule, root, new Vector3(-0.03f, -0.05f, -0.22f), new Vector3(75f, 0f, 0f), new Vector3(0.075f, 0.16f, 0.075f), Mat(sleeve));
            Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.zero, new Vector3(0.08f, 0.07f, 0.1f), skinMat);
            // Index finger forward.
            Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.01f, 0.07f), new Vector3(90f, 0f, 0f), new Vector3(0.02f, 0.04f, 0.02f), skinMat);
            return root;
        }

        static Renderer Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rot);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return r;
        }

        static Material Mat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.3f);
            return m;
        }
    }
}
