using System;
using System.Collections.Generic;
using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Short in-match tutorial (well under 2 minutes): one prompt at a time that clears when the player
    /// does it. Shown automatically the first time, and from the menu's Tutorial button. H skips.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        const string DonePref = "tutorial_done";

        /// <summary>Menu "Tutorial": show even if already done.</summary>
        public static bool ForceNext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ForceNext = false;

        readonly List<(string key, Func<bool> done)> steps = new List<(string, Func<bool>)>();
        GameObject root;
        TextMeshProUGUI text, counter;
        InputAction skipKey;
        int index;
        float lookTravel, zoomTime, pttTime;

        void Awake() => skipKey = new InputAction("SkipTutorial", InputActionType.Button, "<Keyboard>/h");
        void OnEnable() => skipKey.Enable();
        void OnDisable() => skipKey.Disable();
        void OnDestroy() => skipKey.Dispose();

        void Start()
        {
            bool show = ForceNext || PlayerPrefs.GetInt(DonePref, 0) == 0;
            ForceNext = false;
            if (!show) { enabled = false; return; }

            var canvas = UiKit.CreateCanvas("Tutorial", 25).transform;
            canvas.SetParent(transform, false);
            root = UiKit.Rect("Box", canvas, new Vector2(0.3f, 0.74f), new Vector2(0.7f, 0.87f), Vector2.zero, Vector2.zero).gameObject;
            root.AddComponent<Image>().color = UiKit.Yellow;
            root.transform.localRotation = Quaternion.Euler(0, 0, -1.5f);
            text = UiKit.Label(UiKit.Rect("Text", root.transform, new Vector2(0.03f, 0.25f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero), null, 30, UiKit.Navy, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            counter = UiKit.Label(UiKit.Rect("Counter", root.transform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.28f), Vector2.zero, Vector2.zero), null, 20, UiKit.Navy, TextAlignmentOptions.Center);
            BuildSteps();
            root.SetActive(false);
        }

        void BuildSteps()
        {
            var role = GameplayBootstrap.LocalRole;
            steps.Add(("tut_look", () => lookTravel > 500f));
            steps.Add(("tut_binoculars", () => zoomTime > 1.5f));
            steps.Add(("tut_card", () => FindAnyObjectByType<CardPanel>() is { IsOpen: true }));
            steps.Add(("tut_quickchat", () => CardPanel.QuickChatsSent > 0));
            steps.Add(("tut_ptt", () => pttTime > 0.8f));
            switch (role)
            {
                case PlayerRole.Van: steps.Add(("tut_van", () => CardPanel.CardsSent > 0 || pttTime > 4f)); break;
                case PlayerRole.Tent: steps.Add(("tut_tent", () => MatchManager.Instance != null && MatchManager.Instance.tentCard.Value.submitted)); break;
                case PlayerRole.Village: steps.Add(("tut_village", () => VillageDirector.Instance != null && VillageDirector.Instance.Following > 0)); break;
            }
        }

        void Update()
        {
            var match = MatchManager.Instance;
            bool visible = match != null && match.phase.Value == MatchPhase.Playing && !CutscenePlayer.Playing && !PauseMenu.IsOpen && index < steps.Count;
            root.SetActive(visible);
            if (!visible) return;

            if (skipKey.WasPressedThisFrame()) { Finish(); return; }
            var mouse = Mouse.current;
            if (mouse != null) lookTravel += mouse.delta.ReadValue().magnitude;
            var rig = GameplayBootstrap.LocalRig;
            if (rig != null && rig.GetComponent<Binoculars>().ZoomAmount > 0.8f) zoomTime += Time.deltaTime;
            if (Keyboard.current != null && Keyboard.current.vKey.isPressed) pttTime += Time.deltaTime;

            if (steps[index].done())
            {
                index++;
                if (index >= steps.Count) { Finish(); return; }
            }
            text.text = Loc.Get(steps[index].key);
            counter.text = Loc.Format("tut_counter", index + 1, steps.Count);
        }

        void Finish()
        {
            PlayerPrefs.SetInt(DonePref, 1);
            index = steps.Count;
            root.SetActive(false);
            enabled = false;
        }
    }
}
