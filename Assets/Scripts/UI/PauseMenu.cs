using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Esc overlay. The match is shared, so the storm clock and other players keep going while it is open;
    /// the overlay says so.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public string lobbyScene = "Lobby";

        GameObject root, buttons, settings;
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
            root = UiKit.Panel("Dim", canvas, new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.86f)).gameObject;

            var slab = UiKit.Rect("Slab", root.transform, new Vector2(-0.1f, -0.2f), new Vector2(0.42f, 1.2f), Vector2.zero, Vector2.zero);
            slab.gameObject.AddComponent<Image>().color = UiKit.StormGrey;
            slab.localRotation = Quaternion.Euler(0, 0, -12f);
            var bar = UiKit.Rect("Title", root.transform, new Vector2(-0.02f, 0.76f), new Vector2(0.4f, 0.92f), Vector2.zero, Vector2.zero);
            bar.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            bar.localRotation = Quaternion.Euler(0, 0, 5f);
            UiKit.Label(bar, "pause_title", 96, UiKit.Navy, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold | FontStyles.Italic;

            buttons = UiKit.Stretch("Buttons", root.transform).gameObject;
            var items = new (string, UnityEngine.Events.UnityAction)[]
            {
                ("pause_resume", () => SetOpen(false)),
                ("menu_settings", () => { buttons.SetActive(false); settings.SetActive(true); }),
                ("pause_leave", Leave),
            };
            for (int i = 0; i < items.Length; i++)
            {
                float y = 0.62f - i * 0.1f;
                UiKit.Button(UiKit.Rect("Item" + i, buttons.transform, new Vector2(0.06f + i * 0.015f, y - 0.08f), new Vector2(0.4f + i * 0.015f, y), Vector2.zero, Vector2.zero), items[i].Item1, items[i].Item2, 44);
            }
            UiKit.Label(UiKit.Rect("Note", buttons.transform, new Vector2(0.5f, 0.4f), new Vector2(0.95f, 0.6f), Vector2.zero, Vector2.zero), "pause_note", 30, UiKit.White, TextAlignmentOptions.MidlineLeft);

            settings = SettingsPanel.Build(root.transform, () => { settings.SetActive(false); buttons.SetActive(true); }).gameObject;
            settings.SetActive(false);
            root.SetActive(false);
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
            IsOpen = open;
            root.SetActive(open);
            buttons.SetActive(true);
            settings.SetActive(false);
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
