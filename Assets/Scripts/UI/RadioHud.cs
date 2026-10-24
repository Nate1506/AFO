using SayItAgain.Net;
using SayItAgain.Voice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>Bottom-left radio panel: push-to-talk state, who is talking, signal bars. Visible while in a room.</summary>
    public sealed class RadioHud : MonoBehaviour
    {
        GameObject root;
        Image pttLamp, micBar;
        TextMeshProUGUI pttLabel, linkLabel, stateLabel;
        readonly Image[] bars = new Image[5];

        void Start()
        {
            var canvas = UiKit.CreateCanvas("RadioHud", 20);
            canvas.transform.SetParent(transform, false);
            root = UiKit.Rect("Panel", canvas.transform, new Vector2(0.015f, 0.03f), new Vector2(0.3f, 0.2f), Vector2.zero, Vector2.zero).gameObject;
            var bg = root.AddComponent<Image>();
            bg.color = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.82f);
            root.transform.localRotation = Quaternion.Euler(0, 0, 1.5f);

            var lamp = UiKit.Rect("Lamp", root.transform, new Vector2(0.04f, 0.6f), new Vector2(0.12f, 0.9f), Vector2.zero, Vector2.zero);
            pttLamp = lamp.gameObject.AddComponent<Image>();
            pttLabel = UiKit.Label(UiKit.Rect("Ptt", root.transform, new Vector2(0.15f, 0.55f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero), null, 30, UiKit.White, TextAlignmentOptions.MidlineLeft);
            pttLabel.fontStyle = FontStyles.Bold;
            micBar = UiKit.Rect("MicLevel", root.transform, new Vector2(0.15f, 0.55f), new Vector2(0.15f, 0.6f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            micBar.color = UiKit.Red;
            linkLabel = UiKit.Label(UiKit.Rect("Link", root.transform, new Vector2(0.04f, 0.3f), new Vector2(0.7f, 0.55f), Vector2.zero, Vector2.zero), null, 24, UiKit.Yellow, TextAlignmentOptions.MidlineLeft);
            stateLabel = UiKit.Label(UiKit.Rect("State", root.transform, new Vector2(0.04f, 0.04f), new Vector2(0.98f, 0.3f), Vector2.zero, Vector2.zero), null, 20, UiKit.White, TextAlignmentOptions.MidlineLeft);

            for (int i = 0; i < bars.Length; i++)
            {
                float x0 = 0.74f + i * 0.048f;
                var bar = UiKit.Rect("Bar" + i, root.transform, new Vector2(x0, 0.32f), new Vector2(x0 + 0.035f, 0.32f + 0.05f * (i + 1)), Vector2.zero, Vector2.zero);
                bars[i] = bar.gameObject.AddComponent<Image>();
            }
        }

        void Update()
        {
            var session = SessionService.Instance;
            var voice = VoiceService.Instance;
            bool show = session != null && session.InRoom;
            if (root.activeSelf != show) root.SetActive(show);
            if (!show || voice == null) return;

            bool solo = session.IsSolo;
            bool on = voice.IsTransmitting;
            pttLamp.color = on ? UiKit.Red : UiKit.StormGrey;
            pttLabel.text = Loc.Get(on ? "radio_on_air" : "radio_hold_v");

            var me = RoleManager.Instance != null ? RoleManager.Instance.LocalRole : PlayerRole.None;
            bool lobby = !VoiceService.InMatch;
            string talking = null;
            foreach (var role in new[] { PlayerRole.Van, PlayerRole.Tent, PlayerRole.Village })
                if (role != me && (lobby || RelayChain.CanHear(me, role)) && voice.IsSpeaking(role)) talking = Loc.Format("radio_talking", Loc.Get(role.NameKey()));
            linkLabel.text = talking ?? Loc.Get(lobby ? "radio_link_lobby" : me == PlayerRole.Tent ? "radio_link_tent" : "radio_link_end");

            // Own mic level while transmitting, so a dead or wrong microphone is obvious.
            float level = on ? Mathf.Clamp01(voice.LocalMicLevel * 1.5f) : 0f;
            micBar.rectTransform.anchorMax = new Vector2(Mathf.Lerp(0.15f, 0.98f, level), micBar.rectTransform.anchorMax.y);
            micBar.enabled = on;

            stateLabel.text = solo ? Loc.Get("radio_solo") : voice.State switch
            {
                VoiceState.Connecting => Loc.Get("radio_connecting"),
                VoiceState.Failed => Loc.Get("radio_failed"),
                VoiceState.Ready => string.Empty,
                _ => Loc.Get("radio_connecting"),
            };

            float q = solo ? 0f : voice.SignalQuality;
            int lit = Mathf.RoundToInt(q * bars.Length);
            for (int i = 0; i < bars.Length; i++)
                bars[i].color = i < lit ? (q < 0.35f ? UiKit.Red : UiKit.Yellow) : new Color(1, 1, 1, 0.15f);
        }
    }
}
