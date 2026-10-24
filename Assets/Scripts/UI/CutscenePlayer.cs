using System.Collections;
using SayItAgain.Net;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>One camera shot: Cinemachine camera that drifts while it is live.</summary>
    public sealed class CutsceneShot : MonoBehaviour
    {
        public string captionKey;
        public float seconds = 4f;
        public Vector3 drift = new Vector3(0, 0, 6);
        [Tooltip("Lightning strike this many seconds into the shot; negative = none.")]
        public float strikeAt = -1f;
    }

    /// <summary>
    /// The in-engine opening (about 20 s): storm over the village, the water rising, the van, the tent,
    /// the high hill, with short captions. Skippable. Lightning is single soft pulses at least 4 s apart.
    /// </summary>
    public sealed class CutscenePlayer : MonoBehaviour
    {
        /// <summary>Menu "Opening": play, then return to the menu.</summary>
        public static bool ReplayOnly;
        /// <summary>Tutorial start: skip the opening once.</summary>
        public static bool SkipNext;
        public static bool Playing { get; private set; }

        public CutsceneShot[] shots;
        public string menuScene = "Lobby";

        InputAction skip;
        bool skipped, started;

        void Awake()
        {
            skip = new InputAction("Skip", InputActionType.Button, "<Keyboard>/space");
            skip.AddBinding("<Keyboard>/escape");
            skip.AddBinding("<Keyboard>/enter");
            skip.AddBinding("<Mouse>/leftButton");
        }

        void OnEnable() => skip.Enable();
        void OnDisable() => skip.Disable();
        void OnDestroy() { skip.Dispose(); Playing = false; }

        void Update()
        {
            if (Playing && skip.WasPressedThisFrame()) skipped = true;
            if (started) return;

            if (ReplayOnly) { Begin(); return; }
            var match = MatchManager.Instance;
            if (match == null || match.Level == null) return;
            // The host chose a long briefing only when this round opens with the cutscene.
            bool longBriefing = match.phase.Value == MatchPhase.Briefing && match.playStart.Value - match.Now > MatchManager.ShortBriefing + 2f;
            started = true;
            if (longBriefing && !SkipNext) Begin();
            SkipNext = false;
        }

        void Begin()
        {
            started = true;
            StartCoroutine(Play());
        }

        IEnumerator Play()
        {
            Playing = true;
            var rig = GameplayBootstrap.LocalRig;
            Camera rigCam = rig != null ? rig.GetComponentInChildren<Camera>() : null;
            if (rigCam != null) { rigCam.enabled = false; rigCam.GetComponent<AudioListener>().enabled = false; }
            FirstPersonController.SetCursorLocked(true);

            var camGo = new GameObject("CutsceneCamera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
            camGo.tag = "MainCamera";
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(camGo.GetComponent<Camera>()).renderPostProcessing = true;
            var brain = camGo.GetComponent<CinemachineBrain>();
            brain.DefaultBlend = ComfortSettings.ReduceMotion
                ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f)
                : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.2f);

            var (canvas, caption) = BuildCaption();
            StormWeather.IntensityOverride = 0.9f;
            float total = 0f;
            foreach (var s in shots) total += s.seconds;
            float elapsed = 0f;

            for (int i = 0; i < shots.Length && !skipped; i++)
            {
                var shot = shots[i];
                var vcam = shot.GetComponent<CinemachineCamera>();
                vcam.Priority = 100 + i;
                caption.text = Loc.Get(shot.captionKey);
                var start = shot.transform.position;
                bool struck = false;
                for (float t = 0; t < shot.seconds && !skipped; t += Time.deltaTime)
                {
                    if (!ComfortSettings.ReduceMotion) shot.transform.position = start + shot.drift * (t / shot.seconds);
                    if (!struck && shot.strikeAt >= 0f && t >= shot.strikeAt && StormWeather.Instance != null)
                    {
                        struck = true;
                        StormWeather.Instance.Strike(0.9f);
                    }
                    elapsed += Time.deltaTime;
                    // The flood creeps up over the low ground during the opening.
                    FloodRise.ProgressOverride = Mathf.Lerp(0f, 0.55f, elapsed / total);
                    yield return null;
                }
                shot.transform.position = start;
            }

            FloodRise.ProgressOverride = -1f;
            StormWeather.IntensityOverride = -1f;
            foreach (var s in shots) s.GetComponent<CinemachineCamera>().Priority = 0;
            Destroy(camGo);
            Destroy(canvas);
            if (rigCam != null) { rigCam.enabled = true; rigCam.GetComponent<AudioListener>().enabled = true; }
            Playing = false;

            if (ReplayOnly)
            {
                ReplayOnly = false;
                SceneManager.LoadScene(menuScene);
            }
        }

        (GameObject, TextMeshProUGUI) BuildCaption()
        {
            var canvas = UiKit.CreateCanvas("CutsceneCaptions", 60);
            // Letterbox bars.
            UiKit.Rect("Top", canvas.transform, new Vector2(0, 0.9f), Vector2.one, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>().color = Color.black;
            UiKit.Rect("Bottom", canvas.transform, Vector2.zero, new Vector2(1, 0.16f), Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>().color = Color.black;
            var caption = UiKit.Label(UiKit.Rect("Caption", canvas.transform, new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.15f), Vector2.zero, Vector2.zero), null, 40, UiKit.White, TextAlignmentOptions.Center);
            caption.fontStyle = FontStyles.Bold;
            UiKit.Label(UiKit.Rect("Skip", canvas.transform, new Vector2(0.7f, 0.91f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero), "cut_skip", 24, UiKit.Yellow, TextAlignmentOptions.MidlineRight);
            return (canvas.gameObject, caption);
        }
    }
}
