using SayItAgain.Net;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SayItAgain.UI
{
    /// <summary>End of round: win/lose, villagers saved, and the Chain Reveal (HQ order vs what each player understood).</summary>
    public sealed class ResultPanel : MonoBehaviour
    {
        public string lobbyScene = "Lobby";

        GameObject root;
        TextMeshProUGUI title, summary, broke;
        readonly TextMeshProUGUI[,] cells = new TextMeshProUGUI[4, 3];
        GameObject hostButtons;
        TextMeshProUGUI waitLabel;

        void Start()
        {
            var canvas = UiKit.CreateCanvas("ResultPanel", 40).transform;
            canvas.SetParent(transform, false);
            root = UiKit.Panel("Dim", canvas, new Color(0, 0, 0, 0.75f)).gameObject;
            var card = UiKit.Rect("Card", root.transform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f), Vector2.zero, Vector2.zero);
            card.gameObject.AddComponent<Image>().color = UiKit.Navy;
            var t = card.transform;

            var head = UiKit.Rect("Head", t, new Vector2(-0.02f, 0.84f), new Vector2(0.7f, 0.98f), Vector2.zero, Vector2.zero);
            head.gameObject.AddComponent<Image>().color = UiKit.Yellow;
            head.localRotation = Quaternion.Euler(0, 0, 2f);
            title = UiKit.Label(UiKit.Rect("Title", head, Vector2.zero, Vector2.one, new Vector2(30, 0), Vector2.zero), null, 64, UiKit.Navy, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;
            summary = UiKit.Label(UiKit.Rect("Summary", t, new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.83f), Vector2.zero, Vector2.zero), null, 30, UiKit.White, TextAlignmentOptions.MidlineLeft);

            // Chain Reveal grid: columns HQ / Tent / Village, rows = order slots.
            string[] heads = { "reveal_hq", "role_tent", "role_village" };
            for (int c = 0; c < 3; c++)
                UiKit.Label(UiKit.Rect("ColHead" + c, t, new Vector2(0.24f + c * 0.25f, 0.66f), new Vector2(0.48f + c * 0.25f, 0.73f), Vector2.zero, Vector2.zero), heads[c], 26, UiKit.Yellow, TextAlignmentOptions.Center);
            for (int r = 0; r < 4; r++)
            {
                float top = 0.65f - r * 0.085f;
                var row = UiKit.Rect("Row" + r, t, new Vector2(0.04f, top - 0.075f), new Vector2(0.96f, top), Vector2.zero, Vector2.zero);
                row.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.05f);
                UiKit.Label(UiKit.Rect("Slot", row, Vector2.zero, new Vector2(0.2f, 1), new Vector2(14, 0), Vector2.zero), OrderData.SlotNameKey(OrderData.Slots[r]), 24, UiKit.Yellow, TextAlignmentOptions.MidlineLeft);
                for (int c = 0; c < 3; c++)
                    cells[r, c] = UiKit.Label(UiKit.Rect("Cell" + c, row, new Vector2(0.21f + c * 0.26f, 0), new Vector2(0.46f + c * 0.26f, 1), Vector2.zero, Vector2.zero), null, 24, UiKit.White, TextAlignmentOptions.Center);
            }
            broke = UiKit.Label(UiKit.Rect("Broke", t, new Vector2(0.04f, 0.18f), new Vector2(0.96f, 0.29f), Vector2.zero, Vector2.zero), null, 28, UiKit.White, TextAlignmentOptions.MidlineLeft);

            hostButtons = UiKit.Rect("HostButtons", t, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.15f), Vector2.zero, Vector2.zero).gameObject;
            UiKit.Button(UiKit.Rect("Again", hostButtons.transform, Vector2.zero, new Vector2(0.48f, 1), Vector2.zero, Vector2.zero), "result_again", PlayAgain, 34);
            UiKit.Button(UiKit.Rect("Lobby", hostButtons.transform, new Vector2(0.52f, 0), Vector2.one, Vector2.zero, Vector2.zero), "result_lobby", BackToLobby, 34);
            waitLabel = UiKit.Label(UiKit.Rect("Wait", t, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.15f), Vector2.zero, Vector2.zero), "lobby_wait_host", 28, UiKit.White, TextAlignmentOptions.Center);
            root.SetActive(false);
        }

        void Update()
        {
            var match = MatchManager.Instance;
            bool show = match != null && match.IsOver;
            if (root.activeSelf != show)
            {
                root.SetActive(show);
                if (show) FirstPersonController.SetCursorLocked(false);
            }
            if (!show) return;

            bool won = match.phase.Value == MatchPhase.Won;
            title.text = Loc.Get(won ? "result_won" : "result_lost");
            float secs = Mathf.Max(0f, match.finishSeconds.Value);
            summary.text = Loc.Format("result_summary", match.saved.Value, match.Level.villagerCount, match.lost.Value, (int)secs / 60, (int)secs % 60);

            var data = match.Orders;
            var truth = match.order.Value;
            var columns = new[] { truth, match.tentCard.Value, match.villageCard.Value };
            for (int r = 0; r < 4; r++)
            {
                var slot = OrderData.Slots[r];
                for (int c = 0; c < 3; c++)
                {
                    cells[r, c].text = OrderText.Value(data, slot, columns[c][slot]);
                    cells[r, c].color = columns[c][slot] == truth[slot] ? UiKit.White : UiKit.Red;
                }
            }
            // Name the first hop where the message changed: Van -> Tent before Tent -> Village.
            string firstBreak = null;
            for (int c = 1; c < 3 && firstBreak == null; c++)
                foreach (var slot in OrderData.Slots)
                    if (columns[c][slot] != columns[c - 1][slot])
                    {
                        firstBreak = Loc.Format("reveal_broke", Loc.Get(c == 1 ? "reveal_link_van_tent" : "reveal_link_tent_village"), Loc.Get(OrderData.SlotNameKey(slot)));
                        break;
                    }
            broke.text = firstBreak ?? Loc.Get("reveal_clean");

            bool host = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            hostButtons.SetActive(host);
            waitLabel.gameObject.SetActive(!host);
        }

        void PlayAgain()
        {
            var nm = NetworkManager.Singleton;
            nm.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        void BackToLobby() => NetworkManager.Singleton.SceneManager.LoadScene(lobbyScene, LoadSceneMode.Single);
    }
}
