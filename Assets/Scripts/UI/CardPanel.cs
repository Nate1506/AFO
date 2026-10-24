using SayItAgain.Net;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>
    /// Tab / Q: the order card. Van sees HQ's order; Tent and Village fill in "what I understood".
    /// Also the toolbox: quick-chat phrases (voice fallback) and, for the Tent, Ask HQ to confirm.
    /// </summary>
    public sealed class CardPanel : MonoBehaviour
    {
        GameObject root;
        TextMeshProUGUI title, sendLabelHint, askLabel;
        readonly TextMeshProUGUI[] values = new TextMeshProUGUI[4];
        readonly GameObject[] arrows = new GameObject[4];
        readonly Button[] askButtons = new Button[4];
        Button sendButton;
        InputAction toggleCard, toggleChat;
        RelayCard card = RelayCard.Empty;
        PlayerRole role;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            toggleCard = new InputAction("Card", InputActionType.Button, "<Keyboard>/tab");
            toggleChat = new InputAction("QuickChat", InputActionType.Button, "<Keyboard>/q");
        }

        void OnEnable() { toggleCard.Enable(); toggleChat.Enable(); }
        void OnDisable() { toggleCard.Disable(); toggleChat.Disable(); }
        void OnDestroy() { toggleCard.Dispose(); toggleChat.Dispose(); }

        void Start()
        {
            role = GameplayBootstrap.LocalRole;
            var canvas = UiKit.CreateCanvas("CardPanel", 30).transform;
            canvas.SetParent(transform, false);
            root = UiKit.Rect("Card", canvas, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f), Vector2.zero, Vector2.zero).gameObject;
            root.AddComponent<Image>().color = new Color(UiKit.Navy.r, UiKit.Navy.g, UiKit.Navy.b, 0.96f);
            var t = root.transform;

            var head = UiKit.Rect("Head", t, new Vector2(0, 0.88f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            head.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            title = UiKit.Label(UiKit.Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(24, 0), Vector2.zero), null, 44, UiKit.Navy, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;

            for (int i = 0; i < 4; i++) BuildRow(t, i);

            var sendRow = UiKit.Rect("SendRow", t, new Vector2(0.04f, 0.3f), new Vector2(0.96f, 0.38f), Vector2.zero, Vector2.zero);
            sendButton = UiKit.Button(UiKit.Rect("SendBox", sendRow, Vector2.zero, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero), SendKey(), Send, 30);
            sendLabelHint = UiKit.Label(UiKit.Rect("SendHint", sendRow, new Vector2(0.52f, 0), Vector2.one, Vector2.zero, Vector2.zero), null, 22, UiKit.White, TextAlignmentOptions.MidlineLeft);
            askLabel = UiKit.Label(UiKit.Rect("AskLabel", t, new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.29f), Vector2.zero, Vector2.zero), null, 22, UiKit.Yellow, TextAlignmentOptions.MidlineLeft);

            // Toolbox: quick-chat phrases (also how humans talk to bots and the fallback when voice fails).
            var phrases = new[] { QuickPhrase.SayAgain, QuickPhrase.ReadBack, QuickPhrase.Correct, QuickPhrase.Wrong, QuickPhrase.Wait, QuickPhrase.Copy };
            for (int i = 0; i < phrases.Length; i++)
            {
                var p = phrases[i];
                float x0 = 0.04f + (i % 3) * 0.31f;
                float y0 = i < 3 ? 0.11f : 0.02f;
                UiKit.Button(UiKit.Rect("QC" + i, t, new Vector2(x0, y0), new Vector2(x0 + 0.29f, y0 + 0.08f), Vector2.zero, Vector2.zero),
                    RadioFeedText.PhraseKey(p), () => MatchManager.Instance?.QuickChatRpc(p, card), 24);
            }

            root.SetActive(false);
        }

        void BuildRow(Transform t, int i)
        {
            var slot = OrderData.Slots[i];
            float top = 0.86f - i * 0.12f;
            var row = UiKit.Rect("Row" + i, t, new Vector2(0.04f, top - 0.105f), new Vector2(0.96f, top), Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.06f);
            UiKit.Label(UiKit.Rect("Slot", row, Vector2.zero, new Vector2(0.25f, 1), new Vector2(16, 0), Vector2.zero), OrderData.SlotNameKey(slot), 26, UiKit.Yellow, TextAlignmentOptions.MidlineLeft);
            values[i] = UiKit.Label(UiKit.Rect("Value", row, new Vector2(0.33f, 0), new Vector2(0.67f, 1), Vector2.zero, Vector2.zero), null, 30, UiKit.White, TextAlignmentOptions.Center);
            values[i].fontStyle = FontStyles.Bold;

            var arrowRoot = UiKit.Stretch("Arrows", row).gameObject;
            UiKit.Button(UiKit.Rect("Prev", arrowRoot.transform, new Vector2(0.26f, 0.12f), new Vector2(0.32f, 0.88f), Vector2.zero, Vector2.zero), "card_prev", () => Step(slot, -1), 30);
            UiKit.Button(UiKit.Rect("Next", arrowRoot.transform, new Vector2(0.68f, 0.12f), new Vector2(0.74f, 0.88f), Vector2.zero, Vector2.zero), "card_next", () => Step(slot, +1), 30);
            arrows[i] = arrowRoot;
            askButtons[i] = UiKit.Button(UiKit.Rect("Ask", row, new Vector2(0.76f, 0.12f), new Vector2(0.98f, 0.88f), Vector2.zero, Vector2.zero), "card_ask_hq", () => MatchManager.Instance?.AskHqRpc(slot), 22);
        }

        string SendKey() => role switch
        {
            PlayerRole.Van => "card_send_van",
            PlayerRole.Tent => "card_send_tent",
            _ => "card_send_village",
        };

        void Update()
        {
            var match = MatchManager.Instance;
            if (match == null || role == PlayerRole.None) return;
            bool toggle = toggleCard.WasPressedThisFrame() || toggleChat.WasPressedThisFrame();
            if (toggle && !match.IsOver) SetOpen(!IsOpen);
            if (match.IsOver && IsOpen) SetOpen(false);
            if (!IsOpen) return;

            var data = match.Orders;
            bool editable = role != PlayerRole.Van;
            var shown = editable ? card : match.order.Value;
            title.text = Loc.Get(editable ? "card_title_understood" : "card_title_hq");
            for (int i = 0; i < 4; i++)
            {
                values[i].text = OrderText.Value(data, OrderData.Slots[i], shown[OrderData.Slots[i]]);
                arrows[i].SetActive(editable);
                askButtons[i].gameObject.SetActive(role == PlayerRole.Tent && match.askHqLeft.Value > 0);
            }
            askLabel.text = role == PlayerRole.Tent ? Loc.Format("card_ask_left", match.askHqLeft.Value) : string.Empty;

            var next = role == PlayerRole.Van ? PlayerRole.Tent : PlayerRole.Village;
            bool nextIsBot = RoleManager.Instance != null && RoleManager.Instance.IsBot(next);
            sendButton.gameObject.SetActive(role != PlayerRole.Van || nextIsBot);
            sendLabelHint.text = role == PlayerRole.Village ? Loc.Get("card_hint_village")
                : nextIsBot ? Loc.Get("card_hint_bot") : Loc.Get("card_hint_voice");
        }

        void SetOpen(bool open)
        {
            root.SetActive(open);
            var rig = GameplayBootstrap.LocalRig;
            if (rig != null)
            {
                rig.GetComponent<FirstPersonController>().InputBlocked = open;
                rig.GetComponent<Binoculars>().Blocked = open;
            }
            FirstPersonController.SetCursorLocked(!open);
        }

        void Step(OrderSlot slot, int dir)
        {
            int n = MatchManager.Instance.Orders.Options(slot).Length;
            // Cycle through "?" (unknown) and every option.
            int v = card[slot] + dir;
            if (v < -1) v = n - 1;
            if (v >= n) v = -1;
            card[slot] = v;
            MatchManager.Instance.SetCardRpc(card, false);
        }

        void Send()
        {
            card.submitted = true;
            MatchManager.Instance.SetCardRpc(card, true);
        }
    }
}
