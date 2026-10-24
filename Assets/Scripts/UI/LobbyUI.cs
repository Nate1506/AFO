using SayItAgain.Net;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>Create / join a room by code, or start solo with bots. Shows the role table and lets the host start.</summary>
    public sealed class LobbyUI : MonoBehaviour
    {
        public string gameplayScene = "Gameplay_Test";

        static readonly PlayerRole[] Roles = { PlayerRole.Van, PlayerRole.Tent, PlayerRole.Village };

        GameObject entryPanel, roomPanel, canvasRoot;
        TMP_InputField codeInput;
        TextMeshProUGUI statusLabel, codeLabel, waitLabel;
        Button startButton, levelButton;
        TextMeshProUGUI levelLabel;
        readonly TextMeshProUGUI[] occupantLabels = new TextMeshProUGUI[3];
        readonly Button[] takeButtons = new Button[3];

        void Start()
        {
            FirstPersonController.SetCursorLocked(false);
            Build();
            SessionService.Instance.StatusChanged += Refresh;
            RoleManager.RolesChanged += Refresh;
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (SessionService.Instance != null) SessionService.Instance.StatusChanged -= Refresh;
            RoleManager.RolesChanged -= Refresh;
            Loc.LanguageChanged -= Refresh;
        }

        void Build()
        {
            var canvas = UiKit.CreateCanvas("LobbyCanvas", 0).transform;
            canvasRoot = canvas.gameObject;
            UiKit.Panel("Background", canvas, UiKit.Navy);

            // Tilted title slab on the left.
            var slab = UiKit.Rect("TitleSlab", canvas, new Vector2(0, 0.55f), new Vector2(0.55f, 0.85f), new Vector2(-80, 0), Vector2.zero);
            slab.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            slab.localRotation = Quaternion.Euler(0, 0, 6f);
            var title = UiKit.Label(slab, "game_title", 140, UiKit.Navy, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;
            var redBar = UiKit.Rect("RedBar", canvas, new Vector2(0, 0.5f), new Vector2(0.45f, 0.53f), new Vector2(-80, 0), Vector2.zero);
            redBar.gameObject.AddComponent<Image>().color = UiKit.Red;
            redBar.localRotation = Quaternion.Euler(0, 0, 6f);

            // Sits above the radio HUD, which owns the bottom-left corner.
            statusLabel = UiKit.Label(UiKit.Rect("Status", canvas, new Vector2(0.02f, 0.24f), new Vector2(0.5f, 0.32f), Vector2.zero, Vector2.zero),
                null, 34, UiKit.White, TextAlignmentOptions.BottomLeft);

            BuildEntryPanel(canvas);
            BuildRoomPanel(canvas);
            canvasRoot.SetActive(visible);
        }

        void BuildEntryPanel(Transform canvas)
        {
            entryPanel = UiKit.Rect("EntryPanel", canvas, new Vector2(0.58f, 0.18f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero).gameObject;
            var t = entryPanel.transform;

            UiKit.Button(Row(t, 0), "lobby_host", () => _ = SessionService.Instance.CreateRoomAsync());

            var joinRow = Row(t, 1);
            codeInput = UiKit.Input(UiKit.Rect("CodeBox", joinRow, Vector2.zero, new Vector2(0.62f, 1), Vector2.zero, new Vector2(-12, 0)), "lobby_code_hint", 8);
            UiKit.Button(UiKit.Rect("JoinBox", joinRow, new Vector2(0.62f, 0), Vector2.one, Vector2.zero, Vector2.zero),
                "lobby_join", () => _ = SessionService.Instance.JoinRoomAsync(codeInput.text));

            UiKit.Button(Row(t, 2), "lobby_solo", () => SessionService.Instance.StartSolo());
            UiKit.Button(Row(t, 3), "menu_back", () => BackRequested?.Invoke(), 32);
        }

        void BuildRoomPanel(Transform canvas)
        {
            roomPanel = UiKit.Rect("RoomPanel", canvas, new Vector2(0.58f, 0.16f), new Vector2(0.96f, 0.9f), Vector2.zero, Vector2.zero).gameObject;
            var t = roomPanel.transform;

            var header = Row(t, 0);
            UiKit.Label(UiKit.Rect("CodeCaption", header, new Vector2(0, 0.62f), Vector2.one, Vector2.zero, Vector2.zero), "lobby_room_code", 30, UiKit.Yellow);
            codeLabel = UiKit.Label(UiKit.Rect("Code", header, Vector2.zero, new Vector2(0.55f, 0.7f), Vector2.zero, Vector2.zero), null, 72, UiKit.White);
            codeLabel.fontStyle = FontStyles.Bold;
            levelButton = UiKit.Button(UiKit.Rect("LevelBox", header, new Vector2(0.56f, 0.05f), new Vector2(1f, 0.62f), Vector2.zero, Vector2.zero), null, CycleLevel, 28);
            levelLabel = levelButton.GetComponentInChildren<TextMeshProUGUI>();

            for (int i = 0; i < Roles.Length; i++)
            {
                var row = Row(t, i + 1);
                row.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.06f);
                UiKit.Label(UiKit.Rect("Role", row, Vector2.zero, new Vector2(0.45f, 1), new Vector2(20, 0), Vector2.zero), Roles[i].NameKey(), 34, UiKit.Yellow, TextAlignmentOptions.MidlineLeft);
                occupantLabels[i] = UiKit.Label(UiKit.Rect("Occupant", row, new Vector2(0.45f, 0), new Vector2(0.75f, 1), Vector2.zero, Vector2.zero), null, 32, UiKit.White, TextAlignmentOptions.MidlineLeft);
                var role = Roles[i];
                takeButtons[i] = UiKit.Button(UiKit.Rect("TakeBox", row, new Vector2(0.77f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero),
                    "lobby_take_role", () => RoleManager.Instance?.RequestRoleRpc(role), 28);
            }

            var bottom = Row(t, 4);
            startButton = UiKit.Button(UiKit.Rect("StartBox", bottom, Vector2.zero, new Vector2(0.6f, 1), Vector2.zero, new Vector2(-12, 0)), "lobby_start", StartGame, 48);
            waitLabel = UiKit.Label(UiKit.Rect("Wait", bottom, Vector2.zero, new Vector2(0.6f, 1), Vector2.zero, Vector2.zero), "lobby_wait_host", 30, UiKit.White, TextAlignmentOptions.Center);
            UiKit.Button(UiKit.Rect("LeaveBox", bottom, new Vector2(0.6f, 0), Vector2.one, Vector2.zero, Vector2.zero), "lobby_leave", () => _ = SessionService.Instance.LeaveAsync());
        }

        /// <summary>Five equal rows with gaps, top to bottom.</summary>
        static RectTransform Row(Transform parent, int index)
        {
            const float h = 0.2f, gap = 0.025f;
            float top = 1f - index * h;
            return UiKit.Rect("Row" + index, parent, new Vector2(0, top - h + gap), new Vector2(1, top), Vector2.zero, Vector2.zero);
        }

        void Refresh()
        {
            var session = SessionService.Instance;
            bool inRoom = session != null && session.InRoom;
            entryPanel.SetActive(!inRoom);
            roomPanel.SetActive(inRoom);
            statusLabel.text = session != null ? Loc.Get(session.StatusKey) : string.Empty;
            if (!inRoom) return;

            codeLabel.text = session.IsSolo ? Loc.Get("lobby_solo") : session.RoomCode;
            var nm = NetworkManager.Singleton;
            var rm = RoleManager.Instance;
            for (int i = 0; i < Roles.Length; i++)
            {
                ulong owner = rm != null ? rm.OwnerOf(Roles[i]) : RoleManager.NoOwner;
                bool bot = owner == RoleManager.NoOwner;
                bool mine = owner == nm.LocalClientId;
                occupantLabels[i].text = bot ? Loc.Get("role_bot") : mine ? Loc.Get("lobby_you") : $"P{owner + 1}";
                takeButtons[i].gameObject.SetActive(bot && rm != null);
            }
            startButton.gameObject.SetActive(nm.IsServer);
            waitLabel.gameObject.SetActive(!nm.IsServer);

            var level = rm != null ? LevelCatalog.Instance.Get(rm.levelIndex.Value) : null;
            levelLabel.text = level != null ? Loc.Get(level.nameKey) : string.Empty;
            levelButton.interactable = nm.IsServer;
        }

        /// <summary>Host picks among playable levels. The level select screen will replace this.</summary>
        void CycleLevel()
        {
            var rm = RoleManager.Instance;
            var levels = LevelCatalog.Instance.levels;
            if (rm == null || !NetworkManager.Singleton.IsServer || levels == null) return;
            int i = rm.levelIndex.Value;
            do i = (i + 1) % levels.Length; while (!levels[i].playable);
            rm.levelIndex.Value = i;
        }

        void StartGame()
        {
            var nm = NetworkManager.Singleton;
            if (!nm.IsServer) return;
            nm.SceneManager.LoadScene(gameplayScene, LoadSceneMode.Single);
        }

        public event System.Action BackRequested;

        bool visible = true;

        /// <summary>MenuUI may call this before Start builds the canvas; the flag is applied after building.</summary>
        public void SetVisible(bool show)
        {
            visible = show;
            if (canvasRoot != null) canvasRoot.SetActive(show);
        }
    }
}
