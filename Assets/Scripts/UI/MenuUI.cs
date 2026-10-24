using System.Collections;
using SayItAgain.Net;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Front end: splash, main menu, level select (from LevelCatalog), settings, credits.
    /// Hands over to LobbyUI for create/join/solo. Angular slabs, tilted type, a drifting backdrop word and
    /// rain streaks; screens cascade in behind a three-band wipe (see MenuMotion).
    /// </summary>
    public sealed class MenuUI : MonoBehaviour
    {
        public LobbyUI lobby;
        public string gameplayScene = "Gameplay_Kao";

        static bool seenSplash;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => seenSplash = false;
        Transform canvas;
        GameObject splash, main, levels, settings, credits, current;
        BandWipe wipe;
        InputAction back, any;

        void Awake()
        {
            back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
            any = new InputAction("Any", InputActionType.Button, "<Keyboard>/anyKey");
            any.AddBinding("<Mouse>/leftButton");
        }

        void OnEnable() { back.Enable(); any.Enable(); }
        void OnDisable() { back.Disable(); any.Disable(); }
        void OnDestroy() { back.Dispose(); any.Dispose(); }

        void Start()
        {
            GameSettings.ApplyOnBoot();
            FirstPersonController.SetCursorLocked(false);
            canvas = UiKit.CreateCanvas("MenuCanvas", 1).transform;
            BuildSplash();
            BuildMain();
            BuildLevels();
            settings = SettingsPanel.Build(canvas, () => Show(main)).gameObject;
            BuildCredits();
            BuildWipe();
            lobby.BackRequested += () => Show(levels);

            foreach (var s in new[] { splash, main, levels, settings, credits }) s.SetActive(false);
            bool inRoom = SessionService.Instance != null && SessionService.Instance.InRoom;
            if (inRoom) ShowLobby();
            else if (seenSplash) Show(main, instant: true);
            else { seenSplash = true; lobby.SetVisible(false); Show(splash, instant: true); StartCoroutine(SplashTimer()); }
        }

        void Update()
        {
            if (current == splash && any.WasPressedThisFrame()) Show(main);
            else if (back.WasPressedThisFrame())
            {
                if (current == levels || current == settings || current == credits) Show(main);
            }
        }

        IEnumerator SplashTimer()
        {
            yield return new WaitForSecondsRealtime(2.2f);
            if (current == splash) Show(main);
        }

        // ---------- Screens ----------

        GameObject Screen(string name, string wordKey)
        {
            var s = UiKit.Stretch(name, canvas).gameObject;
            s.AddComponent<Image>().color = UiKit.Navy;
            DriftWord.Create(s.transform, wordKey);
            // Storm-grey diagonal slab behind every screen, with rain blowing across.
            var slab = UiKit.Rect("Slab", s.transform, new Vector2(-0.2f, -0.3f), new Vector2(0.55f, 1.3f), Vector2.zero, Vector2.zero);
            slab.gameObject.AddComponent<Image>().color = UiKit.StormGrey;
            slab.localRotation = Quaternion.Euler(0, 0, -14f);
            StormStreaks.Create(s.transform);
            return s;
        }

        /// <summary>Everything but the backdrop flies in, one part after another, each time the screen opens.</summary>
        static void Cascade(GameObject screen)
        {
            var cascade = screen.AddComponent<CascadeIn>();
            foreach (Transform child in screen.transform)
                if (child.name != "Slab" && child.name != "DriftWord" && child.name != "StormStreaks")
                    cascade.targets.Add((RectTransform)child);
        }

        void Title(Transform parent, string key, Vector2 min, Vector2 max, float size)
        {
            var bar = UiKit.Rect("TitleBar", parent, min, max, Vector2.zero, Vector2.zero);
            bar.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            bar.gameObject.AddComponent<UiSkew>().skew = 0.2f;
            bar.localRotation = Quaternion.Euler(0, 0, 5f);
            var t = UiKit.Label(bar, key, size, UiKit.Navy, TextAlignmentOptions.Center);
            t.fontStyle = FontStyles.Bold | FontStyles.Italic;
            var red = UiKit.Rect("RedLine", parent, new Vector2(min.x, min.y - 0.04f), new Vector2(max.x - 0.1f, min.y - 0.015f), Vector2.zero, Vector2.zero);
            red.gameObject.AddComponent<Image>().color = UiKit.Red;
            red.gameObject.AddComponent<UiSkew>().skew = 0.2f;
            red.localRotation = Quaternion.Euler(0, 0, 5f);
        }

        void BuildSplash()
        {
            splash = Screen("Splash", "game_title");
            Title(splash.transform, "game_title", new Vector2(0.12f, 0.42f), new Vector2(0.88f, 0.66f), 160);
            UiKit.Label(UiKit.Rect("Tag", splash.transform, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.3f), Vector2.zero, Vector2.zero), "splash_tagline", 36, UiKit.White, TextAlignmentOptions.Center);
            UiKit.Label(UiKit.Rect("Press", splash.transform, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.12f), Vector2.zero, Vector2.zero), "splash_press", 26, UiKit.Yellow, TextAlignmentOptions.Center);
            Cascade(splash);
        }

        void BuildMain()
        {
            main = Screen("Main", "game_title");
            Title(main.transform, "game_title", new Vector2(-0.03f, 0.7f), new Vector2(0.55f, 0.9f), 120);
            var items = new (string key, UnityEngine.Events.UnityAction action)[]
            {
                ("menu_play", () => Show(levels)),
                ("menu_tutorial", StartTutorial),
                ("menu_opening", ReplayOpening),
                ("menu_settings", () => Show(settings)),
                ("menu_credits", () => Show(credits)),
                ("menu_quit", Application.Quit),
            };
            for (int i = 0; i < items.Length; i++)
            {
                float y = 0.6f - i * 0.088f;
                // A slanted column of big words, each step pushed a little to the right.
                float x = 0.04f + i * 0.022f;
                var item = UiKit.Rect("Item" + i, main.transform, new Vector2(x, y - 0.078f), new Vector2(x + 0.4f, y), Vector2.zero, Vector2.zero);
                item.localRotation = Quaternion.Euler(0, 0, 4f);
                UiKit.MenuItem(item, items[i].key, items[i].action, 62);
            }
            UiKit.Label(UiKit.Rect("Footer", main.transform, new Vector2(0.55f, 0.03f), new Vector2(0.98f, 0.09f), Vector2.zero, Vector2.zero), "menu_footer", 22, UiKit.White, TextAlignmentOptions.BottomRight);
            Cascade(main);
        }

        void BuildLevels()
        {
            levels = Screen("LevelSelect", "levels_title");
            Title(levels.transform, "levels_title", new Vector2(-0.03f, 0.82f), new Vector2(0.45f, 0.96f), 72);
            var list = LevelCatalog.Instance.levels;
            for (int i = 0; i < list.Length; i++)
            {
                var level = list[i];
                int index = i;
                float x0 = 0.05f + i * 0.235f;
                var card = UiKit.Rect("Level" + level.number, levels.transform, new Vector2(x0, 0.22f), new Vector2(x0 + 0.21f, 0.72f), Vector2.zero, Vector2.zero);
                card.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 2.5f : -2.5f);
                var button = UiKit.Button(card, null, () => { if (level.playable) PickLevel(index); }, 30);
                Object.Destroy(button.GetComponentInChildren<TextMeshProUGUI>().gameObject);
                button.interactable = level.playable;

                var num = UiKit.Label(UiKit.Rect("Num", card, new Vector2(0, 0.55f), new Vector2(1, 1), Vector2.zero, Vector2.zero), null, 160, level.playable ? UiKit.Yellow : UiKit.StormGrey, TextAlignmentOptions.Center);
                num.text = level.number.ToString();
                num.fontStyle = FontStyles.Bold | FontStyles.Italic;
                UiKit.Label(UiKit.Rect("Name", card, new Vector2(0.05f, 0.32f), new Vector2(0.95f, 0.55f), Vector2.zero, Vector2.zero), level.nameKey, 30, UiKit.White, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
                UiKit.Label(UiKit.Rect("Lesson", card, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.32f), Vector2.zero, Vector2.zero), level.lessonKey, 22, UiKit.White, TextAlignmentOptions.Top);
                if (!level.playable)
                {
                    var stamp = UiKit.Rect("Locked", card, new Vector2(-0.05f, 0.45f), new Vector2(1.05f, 0.58f), Vector2.zero, Vector2.zero);
                    stamp.gameObject.AddComponent<Image>().color = UiKit.Red;
                    stamp.localRotation = Quaternion.Euler(0, 0, -12f);
                    UiKit.Label(stamp, "level_coming_soon", 30, UiKit.White, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
                }
            }
            UiKit.Button(UiKit.Rect("Back", levels.transform, new Vector2(0.05f, 0.06f), new Vector2(0.25f, 0.13f), Vector2.zero, Vector2.zero), "menu_back", () => Show(main), 32);
            Cascade(levels);
        }

        void BuildCredits()
        {
            credits = Screen("Credits", "menu_credits");
            Title(credits.transform, "menu_credits", new Vector2(-0.03f, 0.82f), new Vector2(0.4f, 0.96f), 72);
            var text = Resources.Load<TextAsset>("Credits");
            var body = UiKit.Label(UiKit.Rect("Body", credits.transform, new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero), null, 19, UiKit.White, TextAlignmentOptions.TopLeft);
            body.text = text != null ? text.text : string.Empty;
            UiKit.Button(UiKit.Rect("Back", credits.transform, new Vector2(0.05f, 0.05f), new Vector2(0.25f, 0.12f), Vector2.zero, Vector2.zero), "menu_back", () => Show(main), 32);
            Cascade(credits);
        }

        void BuildWipe() => wipe = BandWipe.Create(canvas);

        // ---------- Navigation ----------

        void Show(GameObject screen, bool instant = false)
        {
            lobby.SetVisible(false);
            if (current != null && current != screen) current.SetActive(false);
            current = screen;
            screen.SetActive(true);
            screen.transform.SetAsLastSibling();
            if (instant) wipe.transform.SetAsLastSibling();
            else wipe.Play();
            // Highlight the first choice so keyboard players see where they are.
            var first = screen.GetComponentInChildren<Selectable>();
            if (EventSystem.current != null && first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }

        void PickLevel(int index)
        {
            SessionService.PendingLevel = index;
            if (RoleManager.Instance != null && NetworkManager.Singleton.IsServer) RoleManager.Instance.levelIndex.Value = index;
            ShowLobby();
        }

        void ShowLobby()
        {
            if (current != null) current.SetActive(false);
            current = null;
            lobby.SetVisible(true);
        }

        void StartTutorial()
        {
            SessionService.PendingLevel = 0;
            TutorialOverlay.ForceNext = true;
            CutscenePlayer.SkipNext = true;
            SessionService.PreferredHostRole = PlayerRole.Village;
            if (!SessionService.Instance.StartSolo()) return;
            NetworkManager.Singleton.SceneManager.LoadScene(gameplayScene, LoadSceneMode.Single);
        }

        void ReplayOpening()
        {
            CutscenePlayer.ReplayOnly = true;
            SceneManager.LoadScene(gameplayScene);
        }
    }
}
