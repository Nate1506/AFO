using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Settings rows (audio, voice, mic test, language, comfort, quality). Built into any parent,
    /// so the main menu and the pause overlay share it. The mic test only shows a live level; nothing is recorded.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        public Action onBack;

        readonly List<(TextMeshProUGUI label, Func<string> value)> values = new List<(TextMeshProUGUI, Func<string>)>();
        Image micMeter;
        AudioClip micClip;
        string micName;
        bool micTesting;

        public static SettingsPanel Build(Transform parent, Action onBack)
        {
            var root = UiKit.Stretch("Settings", parent);
            var panel = root.gameObject.AddComponent<SettingsPanel>();
            panel.onBack = onBack;
            panel.BuildRows(root);
            return panel;
        }

        void BuildRows(Transform root)
        {
            var bg = UiKit.Rect("Card", root, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            bg.gameObject.AddComponent<Image>().color = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.97f);
            var head = UiKit.Rect("Head", bg, new Vector2(-0.02f, 0.9f), new Vector2(0.45f, 1.02f), Vector2.zero, Vector2.zero);
            head.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            head.localRotation = Quaternion.Euler(0, 0, 2f);
            UiKit.Label(UiKit.Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(28, 0), Vector2.zero), "settings_title", 56, UiKit.Navy, TextAlignmentOptions.MidlineLeft).fontStyle = FontStyles.Bold | FontStyles.Italic;

            int row = 0;
            Slider(bg, row++, "set_master", () => GameSettings.MasterVolume, v => GameSettings.MasterVolume = v);
            Slider(bg, row++, "set_voice", () => GameSettings.VoiceVolume, v => GameSettings.VoiceVolume = v);
            Slider(bg, row++, "set_sfx", () => GameSettings.SfxVolume, v => GameSettings.SfxVolume = v);
            Toggle(bg, row++, "set_ptt", () => GameSettings.PushToTalk, v => GameSettings.PushToTalk = v);
            Slider(bg, row++, "set_vad", () => GameSettings.VoiceThreshold, v => GameSettings.VoiceThreshold = v);
            MicRow(bg, row++);
            Choice(bg, row++, "set_language", () => Loc.Current == Language.Thai ? "ภาษาไทย" : "English",
                d => Loc.Current = Loc.Current == Language.Thai ? Language.English : Language.Thai);
            Slider(bg, row++, "set_sens", () => Mathf.InverseLerp(0.1f, 4f, ComfortSettings.MouseSensitivity), v => ComfortSettings.MouseSensitivity = Mathf.Lerp(0.1f, 4f, v));
            Slider(bg, row++, "set_fov", () => Mathf.InverseLerp(55f, 95f, ComfortSettings.FieldOfView), v => ComfortSettings.FieldOfView = Mathf.Lerp(55f, 95f, v));
            Toggle(bg, row++, "set_headbob", () => ComfortSettings.HeadBob, v => ComfortSettings.HeadBob = v);
            Toggle(bg, row++, "set_reducemotion", () => ComfortSettings.ReduceMotion, v => ComfortSettings.ReduceMotion = v);
            Choice(bg, row++, "set_quality", () => Loc.Get("quality_" + GameSettings.Quality), d => GameSettings.Quality = (GameSettings.Quality + 1) % 3);

            UiKit.Button(UiKit.Rect("Back", bg, new Vector2(0.7f, 0.02f), new Vector2(0.97f, 0.09f), Vector2.zero, Vector2.zero), "menu_back", () => onBack?.Invoke(), 32);
            Refresh();
        }

        RectTransform Row(Transform parent, int i, string key)
        {
            const float top = 0.88f, h = 0.064f;
            float y1 = top - i * h;
            var rt = UiKit.Rect("Row_" + key, parent, new Vector2(0.04f, y1 - h + 0.008f), new Vector2(0.96f, y1), Vector2.zero, Vector2.zero);
            rt.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, i % 2 == 0 ? 0.05f : 0.02f);
            UiKit.Label(UiKit.Rect("Name", rt, Vector2.zero, new Vector2(0.4f, 1), new Vector2(18, 0), Vector2.zero), key, 26, UiKit.White, TextAlignmentOptions.MidlineLeft);
            return rt;
        }

        void Slider(Transform parent, int i, string key, Func<float> get, Action<float> set)
        {
            var rt = Row(parent, i, key);
            var value = UiKit.Label(UiKit.Rect("Value", rt, new Vector2(0.84f, 0), Vector2.one, Vector2.zero, Vector2.zero), null, 26, UiKit.Yellow, TextAlignmentOptions.Center);
            UiKit.Button(UiKit.Rect("Minus", rt, new Vector2(0.42f, 0.12f), new Vector2(0.5f, 0.88f), Vector2.zero, Vector2.zero), "card_prev", () => { set(Mathf.Clamp01(get() - 0.1f)); Refresh(); }, 26);
            var barBg = UiKit.Rect("Bar", rt, new Vector2(0.52f, 0.35f), new Vector2(0.72f, 0.65f), Vector2.zero, Vector2.zero);
            barBg.gameObject.AddComponent<Image>().color = UiKit.StormGrey;
            var fill = UiKit.Rect("Fill", barBg, Vector2.zero, new Vector2(1, 1), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            fill.color = UiKit.Yellow;
            UiKit.Button(UiKit.Rect("Plus", rt, new Vector2(0.74f, 0.12f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero), "card_next", () => { set(Mathf.Clamp01(get() + 0.1f)); Refresh(); }, 26);
            values.Add((value, () =>
            {
                fill.rectTransform.anchorMax = new Vector2(get(), 1);
                return Mathf.RoundToInt(get() * 100) + "%";
            }));
        }

        void Toggle(Transform parent, int i, string key, Func<bool> get, Action<bool> set)
        {
            var rt = Row(parent, i, key);
            var b = UiKit.Button(UiKit.Rect("Toggle", rt, new Vector2(0.42f, 0.12f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero), null, () => { set(!get()); Refresh(); }, 26);
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            values.Add((label, () => Loc.Get(get() ? "set_on" : "set_off")));
        }

        void Choice(Transform parent, int i, string key, Func<string> get, Action<int> step)
        {
            var rt = Row(parent, i, key);
            var b = UiKit.Button(UiKit.Rect("Choice", rt, new Vector2(0.42f, 0.12f), new Vector2(0.82f, 0.88f), Vector2.zero, Vector2.zero), null, () => { step(1); Refresh(); }, 26);
            values.Add((b.GetComponentInChildren<TextMeshProUGUI>(), get));
        }

        void MicRow(Transform parent, int i)
        {
            var rt = Row(parent, i, "set_mic");
            var b = UiKit.Button(UiKit.Rect("Mic", rt, new Vector2(0.42f, 0.12f), new Vector2(0.72f, 0.88f), Vector2.zero, Vector2.zero), null, () => { NextMic(); Refresh(); }, 22);
            values.Add((b.GetComponentInChildren<TextMeshProUGUI>(), () => string.IsNullOrEmpty(GameSettings.MicDevice) ? Loc.Get("set_mic_default") : GameSettings.MicDevice));
            var test = UiKit.Button(UiKit.Rect("Test", rt, new Vector2(0.74f, 0.12f), new Vector2(0.86f, 0.88f), Vector2.zero, Vector2.zero), "set_mic_test", ToggleMicTest, 22);
            var meterBg = UiKit.Rect("Meter", rt, new Vector2(0.88f, 0.2f), new Vector2(0.98f, 0.8f), Vector2.zero, Vector2.zero);
            meterBg.gameObject.AddComponent<Image>().color = UiKit.StormGrey;
            micMeter = UiKit.Rect("Level", meterBg, Vector2.zero, new Vector2(0, 1), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            micMeter.color = UiKit.Yellow;
        }

        static void NextMic()
        {
            var devices = Microphone.devices;
            if (devices.Length == 0) { GameSettings.MicDevice = string.Empty; return; }
            int i = Array.IndexOf(devices, GameSettings.MicDevice);
            // Cycle: default, then each device.
            GameSettings.MicDevice = i + 1 >= devices.Length ? string.Empty : devices[i + 1];
        }

        void ToggleMicTest()
        {
            if (micTesting) { StopMicTest(); return; }
            if (Microphone.devices.Length == 0) return;
            micName = string.IsNullOrEmpty(GameSettings.MicDevice) ? null : GameSettings.MicDevice;
            // Short looping buffer read for a level meter only; it is never saved or sent.
            micClip = Microphone.Start(micName, true, 1, 16000);
            micTesting = micClip != null;
        }

        void StopMicTest()
        {
            if (micTesting) Microphone.End(micName);
            micTesting = false;
            if (micMeter != null) micMeter.rectTransform.anchorMax = new Vector2(0, 1);
        }

        void OnDisable() => StopMicTest();

        void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDestroy() => Loc.LanguageChanged -= Refresh;

        void Refresh()
        {
            foreach (var (label, value) in values) label.text = value();
        }

        void Update()
        {
            if (!micTesting) return;
            int pos = Microphone.GetPosition(micName) - 256;
            if (pos < 0) return;
            var buf = new float[256];
            micClip.GetData(buf, pos);
            float peak = 0f;
            foreach (var s in buf) peak = Mathf.Max(peak, Mathf.Abs(s));
            micMeter.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(peak * 3f), 1);
        }
    }
}
