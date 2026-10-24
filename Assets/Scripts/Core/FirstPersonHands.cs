using SayItAgain.UI;
using SayItAgain.Voice;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain
{
    /// <summary>
    /// First-person gloved hands built from simple shapes: the right hand holds a handheld radio, the left hand points.
    /// Procedural poses: idle (with walk sway), raise to the mouth while talking, lower for binoculars,
    /// the card or pause, and point when the village player calls villagers (E).
    /// </summary>
    public sealed class FirstPersonHands : MonoBehaviour
    {
        public Color sleeve = new Color(1f, 0.5f, 0.1f);
        public Color glove = new Color(0.16f, 0.17f, 0.19f);

        Transform right, left;
        Material ledMat;
        FirstPersonController controller;
        Binoculars binoculars;
        CardPanel card;
        InputAction point;
        float pointUntil, swayTime;

        /// <summary>Test hook: hold the talking pose.</summary>
        public static bool DebugTalk;

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
            // Raise the radio on the key itself, so it also works solo and while voice is offline.
            bool keyHeld = GameSettings.PushToTalk && Keyboard.current != null && Keyboard.current.vKey.isPressed;
            bool talking = DebugTalk || keyHeld || (VoiceService.Instance != null && VoiceService.Instance.IsTransmitting);
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
            var led = talking ? new Color(1f, 0.15f, 0.1f) : new Color(0.1f, 0.5f, 0.15f);
            ledMat.SetColor("_BaseColor", led);
            ledMat.SetColor("_EmissionColor", led * 2f);
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
            var gloveMat = Mat(glove);
            var radioMat = Mat(new Color(0.11f, 0.12f, 0.13f));
            var rubberMat = Mat(new Color(0.04f, 0.04f, 0.045f));
            var greyMat = Mat(new Color(0.34f, 0.36f, 0.38f));
            ledMat = Glow(Color.green);

            Sleeve(root, new Vector3(0.05f, -0.08f, -0.2f), 70f);

            // Radio: body, rubber side grips, yellow band, knobs, antenna.
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.06f, 0.01f), Vector3.zero, new Vector3(0.06f, 0.14f, 0.036f), radioMat);
            Part(PrimitiveType.Cube, root, new Vector3(-0.032f, 0.055f, 0.01f), Vector3.zero, new Vector3(0.006f, 0.11f, 0.03f), rubberMat);
            Part(PrimitiveType.Cube, root, new Vector3(0.032f, 0.055f, 0.01f), Vector3.zero, new Vector3(0.006f, 0.11f, 0.03f), rubberMat);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.002f, 0.01f), Vector3.zero, new Vector3(0.061f, 0.008f, 0.037f), Mat(UiKit.Yellow));
            Part(PrimitiveType.Cylinder, root, new Vector3(-0.015f, 0.134f, 0.01f), Vector3.zero, new Vector3(0.016f, 0.006f, 0.016f), rubberMat);
            Part(PrimitiveType.Cylinder, root, new Vector3(0.006f, 0.133f, 0.01f), Vector3.zero, new Vector3(0.011f, 0.005f, 0.011f), Mat(UiKit.Red));
            Part(PrimitiveType.Cylinder, root, new Vector3(0.019f, 0.172f, 0.01f), Vector3.zero, new Vector3(0.012f, 0.04f, 0.012f), rubberMat);
            Part(PrimitiveType.Sphere, root, new Vector3(0.019f, 0.213f, 0.01f), Vector3.zero, new Vector3(0.015f, 0.015f, 0.015f), rubberMat);

            // Front: speaker slots, lit screen, keypad, status light.
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, root, new Vector3(0f, 0.108f - i * 0.007f, -0.0085f), Vector3.zero, new Vector3(0.042f, 0.0025f, 0.002f), greyMat);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.07f, -0.0085f), Vector3.zero, new Vector3(0.042f, 0.022f, 0.002f), Glow(new Color(0.45f, 0.85f, 0.55f) * 0.8f));
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                Part(PrimitiveType.Cube, root, new Vector3(-0.014f + col * 0.014f, 0.046f - row * 0.012f, -0.009f), Vector3.zero, new Vector3(0.01f, 0.007f, 0.003f), greyMat);
            Part(PrimitiveType.Sphere, root, new Vector3(-0.02f, 0.124f, -0.007f), Vector3.zero, new Vector3(0.008f, 0.008f, 0.008f), ledMat);

            // Gloved hand: palm and fingers wrapped round the back, thumb on the push-to-talk key.
            Part(PrimitiveType.Cube, root, new Vector3(-0.035f, 0.075f, 0.01f), Vector3.zero, new Vector3(0.006f, 0.03f, 0.016f), Mat(UiKit.Red));
            Part(PrimitiveType.Sphere, root, new Vector3(0.012f, 0.02f, 0.045f), Vector3.zero, new Vector3(0.08f, 0.09f, 0.05f), gloveMat);
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Capsule, root, new Vector3(0.006f, 0.0f + i * 0.021f, 0.034f), new Vector3(0f, 0f, 90f), new Vector3(0.021f, 0.036f, 0.021f), gloveMat);
            Part(PrimitiveType.Capsule, root, new Vector3(-0.04f, 0.055f, 0.012f), new Vector3(0f, 0f, 12f), new Vector3(0.023f, 0.032f, 0.023f), gloveMat);
            return root;
        }

        Transform BuildPointingHand()
        {
            var root = new GameObject("LeftHand").transform;
            root.SetParent(transform, false);
            root.localPosition = LeftAway.pos;
            var gloveMat = Mat(glove);
            Sleeve(root, new Vector3(-0.03f, -0.07f, -0.22f), 75f);
            // Fist with the index finger out in two joints, curled fingers and thumb along the side.
            Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.zero, new Vector3(0.08f, 0.07f, 0.09f), gloveMat);
            Part(PrimitiveType.Capsule, root, new Vector3(0.012f, 0.015f, 0.06f), new Vector3(90f, 0f, 0f), new Vector3(0.022f, 0.03f, 0.022f), gloveMat);
            Part(PrimitiveType.Capsule, root, new Vector3(0.012f, 0.015f, 0.1f), new Vector3(90f, 0f, 0f), new Vector3(0.02f, 0.022f, 0.02f), gloveMat);
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Sphere, root, new Vector3(0.018f - i * 0.02f, -0.018f, 0.042f), Vector3.zero, new Vector3(0.024f, 0.026f, 0.03f), gloveMat);
            Part(PrimitiveType.Capsule, root, new Vector3(0.04f, 0.0f, 0.03f), new Vector3(60f, 0f, -30f), new Vector3(0.022f, 0.03f, 0.022f), gloveMat);
            return root;
        }

        /// <summary>Orange rescue sleeve with a reflective band and a dark glove cuff, running back from the wrist.</summary>
        void Sleeve(Transform root, Vector3 center, float tilt)
        {
            var axis = Quaternion.Euler(tilt, 0f, 0f) * Vector3.up;
            var rot = new Vector3(tilt, 0f, 0f);
            Part(PrimitiveType.Cylinder, root, center, rot, new Vector3(0.088f, 0.13f, 0.088f), Mat(sleeve));
            Part(PrimitiveType.Cylinder, root, center + axis * 0.05f, rot, new Vector3(0.091f, 0.012f, 0.091f), Mat(new Color(0.85f, 0.88f, 0.86f)));
            Part(PrimitiveType.Cylinder, root, center + axis * 0.145f, rot, new Vector3(0.072f, 0.022f, 0.072f), Mat(glove * 0.7f));
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

        static Material Glow(Color c)
        {
            var m = Mat(c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
            return m;
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
