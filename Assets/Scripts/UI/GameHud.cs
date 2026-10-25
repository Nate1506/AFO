using System.Collections.Generic;
using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>In-game HUD: storm clock, role objective, briefing banner, radio feed, village hints.</summary>
    public sealed class GameHud : MonoBehaviour
    {
        const int FeedLines = 6;

        TextMeshProUGUI clock, objective, feed, hint, briefingTitle, briefingBody;
        GameObject briefing, clockPlate;
        readonly Queue<string> lines = new Queue<string>();
        Canvas canvas;
        int lastTick;

        void Start()
        {
            var c = UiKit.CreateCanvas("GameHud", 5);
            canvas = c;
            var canvasT = c.transform;
            canvasT.SetParent(transform, false);

            clockPlate = UiKit.Rect("ClockPlate", canvasT, new Vector2(0.42f, 0.9f), new Vector2(0.58f, 0.98f), Vector2.zero, Vector2.zero).gameObject;
            clockPlate.AddComponent<Image>().color = UiKit.Navy;
            clockPlate.transform.localRotation = Quaternion.Euler(0, 0, -2f);
            clock = UiKit.Label(clockPlate.transform, null, 56, UiKit.Yellow, TextAlignmentOptions.Center);
            clock.fontStyle = FontStyles.Bold;

            objective = UiKit.Label(UiKit.Rect("Objective", canvasT, new Vector2(0.015f, 0.9f), new Vector2(0.4f, 0.98f), Vector2.zero, Vector2.zero), null, 26, UiKit.White, TextAlignmentOptions.TopLeft);
            objective.fontStyle = FontStyles.Bold;
            hint = UiKit.Label(UiKit.Rect("Hint", canvasT, new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.32f), Vector2.zero, Vector2.zero), null, 32, UiKit.Yellow, TextAlignmentOptions.Center);
            hint.fontStyle = FontStyles.Bold;

            var feedRt = UiKit.Rect("Feed", canvasT, new Vector2(0.62f, 0.45f), new Vector2(0.985f, 0.88f), Vector2.zero, Vector2.zero);
            feedRt.gameObject.AddComponent<Image>().color = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.7f);
            feed = UiKit.Label(UiKit.Rect("Text", feedRt, Vector2.zero, Vector2.one, new Vector2(16, 12), new Vector2(-16, -12)), null, 24, UiKit.White, TextAlignmentOptions.BottomLeft);
            feed.richText = true;

            briefing = UiKit.Rect("Briefing", canvasT, new Vector2(0.15f, 0.55f), new Vector2(0.85f, 0.8f), Vector2.zero, Vector2.zero).gameObject;
            briefing.AddComponent<Image>().color = UiKit.Yellow;
            briefing.transform.localRotation = Quaternion.Euler(0, 0, 3f);
            briefingTitle = UiKit.Label(UiKit.Rect("Title", briefing.transform, new Vector2(0.03f, 0.55f), new Vector2(0.97f, 0.95f), Vector2.zero, Vector2.zero), null, 64, UiKit.Navy, TextAlignmentOptions.Center);
            briefingTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
            briefingBody = UiKit.Label(UiKit.Rect("Body", briefing.transform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.55f), Vector2.zero, Vector2.zero), null, 32, UiKit.Navy, TextAlignmentOptions.Center);

            MatchManager.RadioReceived += OnRadio;
        }

        void OnDestroy() => MatchManager.RadioReceived -= OnRadio;

        void OnRadio(RadioMessage msg)
        {
            lines.Enqueue(RadioFeedText.Format(msg));
            while (lines.Count > FeedLines) lines.Dequeue();
            feed.text = string.Join("\n", lines);
        }

        void Update()
        {
            if (canvas != null) canvas.enabled = !CutscenePlayer.Playing;
            var match = MatchManager.Instance;
            if (match == null || match.Level == null) return;
            var role = GameplayBootstrap.LocalRole;

            float left = match.SecondsLeft;
            clock.text = $"{(int)left / 60}:{(int)left % 60:00}";
            clock.color = left < 60f ? UiKit.Red : UiKit.Yellow;
            // Countdown tick for the last ten seconds.
            int whole = Mathf.CeilToInt(left);
            if (whole != lastTick && whole <= 10 && whole > 0 && match.phase.Value == MatchPhase.Playing) UiSfx.Play(UiSound.Tick);
            lastTick = whole;

            bool inBriefing = match.phase.Value == MatchPhase.Briefing;
            briefing.SetActive(inBriefing);
            if (inBriefing)
            {
                briefingTitle.text = Loc.Get(match.Level.nameKey);
                briefingBody.text = Loc.Get(match.Level.lessonKey) + "\n" + Loc.Format("brief_you_are", Loc.Get(role.NameKey()));
            }

            objective.text = Loc.Get("obj_" + role.ToString().ToLowerInvariant()) + "\n<size=70%>" + Loc.Get("hud_keys") + "</size>";

            var village = VillageDirector.Instance;
            string h = string.Empty;
            if (role == PlayerRole.Village && village != null && !match.IsOver)
            {
                if (village.PlayerInWater) h = Loc.Get(HazardZone.LastDeadlyRule == HazardZone.Rule.IfAvoidedCrossing ? "hint_landslide" : "hint_water");
                else if (Time.time - village.LastCallTime < 2f) h = Loc.Get("hint_called");
                else if (village.Following > 0) h = Loc.Format("hint_following", village.Following, village.Saved);
            }
            hint.text = h;
        }
    }
}
