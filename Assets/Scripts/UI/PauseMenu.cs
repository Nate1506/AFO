using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Esc overlay. The match is shared, so the storm clock and other players keep going while it is open;
    /// the overlay says so. Opens behind a three-band wipe; the title and choices cascade in over a
    /// drifting backdrop word and blowing rain (see MenuMotion).
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public string lobbyScene = "Lobby";

        GameObject root, buttons, settings;
        BandWipe wipe;
        Button firstItem;
        InputAction toggle;

        public static bool IsOpen { get; private set; }

        void Awake() => toggle = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        void OnEnable() => toggle.Enable();
        void OnDisable() => toggle.Disable();

        void OnDestroy()
        {
            toggle.Dispose();
            IsOpen = false;
        }

        void Start()
        {
            var canvas = UiKit.CreateCanvas("PauseMenu", 50).transform;
            canvas.SetParent(transform, false);
            root = UiKit.Panel("Dim", canvas, new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.96f)).gameObject;
            DriftWord.Create(root.transform, "pause_title");

            var slab = UiKit.Rect("Slab", root.transform, new Vector2(-0.1f, -0.2f), new Vector2(0.42f, 1.2f), Vector2.zero, Vector2.zero);
            slab.gameObject.AddComponent<Image>().color = UiKit.StormGrey;
            slab.localRotation = Quaternion.Euler(0, 0, -12f);
            StormStreaks.Create(root.transform);
            var bar = UiKit.Rect("Title", root.transform, new Vector2(-0.02f, 0.76f), new Vector2(0.4f, 0.92f), Vector2.zero, Vector2.zero);
            bar.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            bar.gameObject.AddComponent<UiSkew>().skew = 0.2f;
            bar.localRotation = Quaternion.Euler(0, 0, 5f);
            UiKit.Label(bar, "pause_title", 96, UiKit.Navy, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold | FontStyles.Italic;
            var red = UiKit.Rect("RedLine", root.transform, new Vector2(-0.02f, 0.72f), new Vector2(0.3f, 0.745f), Vector2.zero, Vector2.zero);
            red.gameObject.AddComponent<Image>().color = UiKit.Red;
            red.gameObject.AddComponent<UiSkew>().skew = 0.2f;
            red.localRotation = Quaternion.Euler(0, 0, 5f);
            var cascade = root.AddComponent<CascadeIn>();
            cascade.targets.Add(bar);
            cascade.targets.Add(red);

            buttons = UiKit.Stretch("Buttons", root.transform).gameObject;
            var items = new (string, UnityEngine.Events.UnityAction)[]
            {
                ("pause_resume", () => SetOpen(false)),
                ("menu_settings", () => { buttons.SetActive(false); settings.SetActive(true); }),
                ("pause_leave", Leave),
            };
            for (int i = 0; i < items.Length; i++)
            {
                float y = 0.62f - i * 0.11f;
                // A slanted column of big words, each step pushed a little to the right.
                var item = UiKit.Rect("Item" + i, buttons.transform, new Vector2(0.05f + i * 0.025f, y - 0.09f), new Vector2(0.45f + i * 0.025f, y), Vector2.zero, Vector2.zero);
                item.localRotation = Quaternion.Euler(0, 0, 4f);
                var b = UiKit.MenuItem(item, items[i].Item1, items[i].Item2, 72);
                if (i == 0) firstItem = b;
                cascade.targets.Add(item);
            }
            var note = UiKit.Rect("Note", buttons.transform, new Vector2(0.5f, 0.12f), new Vector2(0.95f, 0.3f), Vector2.zero, Vector2.zero);
            UiKit.Label(note, "pause_note", 30, UiKit.White, TextAlignmentOptions.MidlineLeft);
            cascade.targets.Add(note);

            settings = SettingsPanel.Build(root.transform, () => { settings.SetActive(false); buttons.SetActive(true); }).gameObject;
            settings.SetActive(false);
            root.SetActive(false);
            wipe = BandWipe.Create(canvas);
        }

        void Update()
        {
            if (!toggle.WasPressedThisFrame()) return;
            if (settings.activeSelf) { settings.SetActive(false); buttons.SetActive(true); return; }
            var match = MatchManager.Instance;
            if (match != null && match.IsOver) return;
            SetOpen(!root.activeSelf);
        }

        void SetOpen(bool open)
        {
            if (open != IsOpen) UiSfx.Play(open ? UiSound.Pause : UiSound.Back);
            if (open != IsOpen) wipe.Play();
            IsOpen = open;
            root.SetActive(open);
            buttons.SetActive(true);
            settings.SetActive(false);
            if (open && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(firstItem.gameObject);
            var rig = GameplayBootstrap.LocalRig;
            if (rig != null)
            {
                rig.GetComponent<FirstPersonController>().InputBlocked = open;
                rig.GetComponent<Binoculars>().Blocked = open;
            }
            FirstPersonController.SetCursorLocked(!open);
        }

        async void Leave()
        {
            IsOpen = false;
            if (SessionService.Instance != null) await SessionService.Instance.LeaveAsync();
            SceneManager.LoadScene(lobbyScene);
        }
    }
}
