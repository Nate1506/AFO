using System;
using System.Collections;
using SayItAgain.Voice;
using Unity.Netcode;
using UnityEngine;

namespace SayItAgain.Net
{
    public enum MatchPhase : byte { Briefing, Playing, Won, Lost }

    /// <summary>
    /// One round: the secret HQ order, the storm clock, each relay card, bots for empty roles,
    /// rumours, and the result. Server-authoritative; spawned by the host when the gameplay scene loads.
    /// </summary>
    public sealed class MatchManager : NetworkBehaviour
    {
        public const float ShortBriefing = 5f;
        /// <summary>Long enough for the opening cutscene (about 20 s) plus the briefing banner.</summary>
        public const float CutsceneBriefing = 26f;
        static bool introShown;
        float BriefingSeconds;

        public static MatchManager Instance { get; private set; }
        public static event Action<RadioMessage> RadioReceived;
        public static event Action Changed;

        public readonly NetworkVariable<int> levelIndex = new NetworkVariable<int>();
        public readonly NetworkVariable<RelayCard> order = new NetworkVariable<RelayCard>(RelayCard.Empty);
        public readonly NetworkVariable<RelayCard> tentCard = new NetworkVariable<RelayCard>(RelayCard.Empty);
        public readonly NetworkVariable<RelayCard> villageCard = new NetworkVariable<RelayCard>(RelayCard.Empty);
        public readonly NetworkVariable<double> playStart = new NetworkVariable<double>();
        public readonly NetworkVariable<double> stormEnd = new NetworkVariable<double>();
        public readonly NetworkVariable<MatchPhase> phase = new NetworkVariable<MatchPhase>();
        public readonly NetworkVariable<int> saved = new NetworkVariable<int>();
        public readonly NetworkVariable<int> lost = new NetworkVariable<int>();
        public readonly NetworkVariable<int> askHqLeft = new NetworkVariable<int>();
        public readonly NetworkVariable<float> finishSeconds = new NetworkVariable<float>(-1f);

        public LevelData Level => LevelCatalog.Instance.Get(levelIndex.Value);
        public OrderData Orders => Level.orders;

        System.Random rng;
        // What each bot last heard and last said, so it can repeat or correct itself.
        RelayCard tentHeard = RelayCard.Empty, villageHeard = RelayCard.Empty;
        int tentAskedAgain, villageAskedAgain;
        bool tentVerifiedRoute, fakeSent;
        Coroutine rumorLoop;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            levelIndex.OnValueChanged += (_, __) => Changed?.Invoke();
            phase.OnValueChanged += (_, __) => Changed?.Invoke();
            tentCard.OnValueChanged += (_, __) => Changed?.Invoke();
            villageCard.OnValueChanged += (_, __) => Changed?.Invoke();
            saved.OnValueChanged += (_, __) => Changed?.Invoke();
            lost.OnValueChanged += (_, __) => Changed?.Invoke();
            askHqLeft.OnValueChanged += (_, __) => Changed?.Invoke();
            if (IsServer) StartRound();
            Changed?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this) Instance = null;
            Changed?.Invoke();
        }

        // ---------- Shared helpers (all clients) ----------

        public double Now => NetworkManager.ServerTime.Time;

        /// <summary>Seconds left on the storm clock (counts the briefing as full time).</summary>
        public float SecondsLeft => (float)Math.Max(0, stormEnd.Value - Math.Max(Now, playStart.Value));

        /// <summary>0 at the start of play, 1 when the clock runs out.</summary>
        public float Progress
        {
            get
            {
                double len = stormEnd.Value - playStart.Value;
                return len <= 0 ? 0f : Mathf.Clamp01((float)((Now - playStart.Value) / len));
            }
        }

        public bool IsOver => phase.Value == MatchPhase.Won || phase.Value == MatchPhase.Lost;

        public RelayCard CardOf(PlayerRole role) => role switch
        {
            PlayerRole.Van => order.Value,
            PlayerRole.Tent => tentCard.Value,
            PlayerRole.Village => villageCard.Value,
            _ => RelayCard.Empty,
        };

        /// <summary>Chance a word is lost on one hop, from the level weather and the current storm.</summary>
        public float LinkDropChance(PlayerRole a, PlayerRole b)
        {
            var w = Level.weather;
            float intensity = w.IntensityAt(Progress);
            return Mathf.Clamp(w.dropoutsPerMinute / 60f * 1.5f * intensity + w.noisePerKm * RelayChain.DistanceKm(a, b), 0.05f, 0.6f);
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (Level != null) StormState.Intensity = Level.weather.IntensityAt(Progress);
            if (!IsServer) return;
            if (phase.Value == MatchPhase.Briefing && Now >= playStart.Value) phase.Value = MatchPhase.Playing;
            if (phase.Value == MatchPhase.Playing && Now >= stormEnd.Value) EndRound(false);
        }

        // ---------- Client -> server ----------

        /// <summary>Every edit of a relay card is mirrored so the Chain Reveal shows what each player believed.</summary>
        [Rpc(SendTo.Server)]
        public void SetCardRpc(RelayCard card, bool send, RpcParams p = default)
        {
            var role = RoleManager.Instance.RoleOf(p.Receive.SenderClientId);
            if (IsOver) return;
            card.submitted = send || CardOf(role).submitted;
            if (role == PlayerRole.Tent) tentCard.Value = card;
            else if (role == PlayerRole.Village) villageCard.Value = card;
            if (!send) return;

            // A card sent to a bot is the only way to "talk" to it.
            var next = role == PlayerRole.Van ? PlayerRole.Tent : role == PlayerRole.Tent ? PlayerRole.Village : PlayerRole.None;
            if (next != PlayerRole.None && RoleManager.Instance.IsBot(next))
                StartCoroutine(BotHear(next, role, role == PlayerRole.Van ? order.Value : card));
        }

        [Rpc(SendTo.Server)]
        public void QuickChatRpc(QuickPhrase phrase, RelayCard card, RpcParams p = default)
        {
            var from = RoleManager.Instance.RoleOf(p.Receive.SenderClientId);
            foreach (var to in new[] { PlayerRole.Van, PlayerRole.Tent, PlayerRole.Village })
            {
                if (!RelayChain.CanHear(to, from)) continue;
                var msg = new RadioMessage { kind = RadioKind.QuickChat, from = from, phrase = phrase, card = card, seed = rng.Next(), drop = LinkDropChance(from, to) * 0.5f };
                if (RoleManager.Instance.IsBot(to)) StartCoroutine(BotReply(to, from, msg));
                else Send(to, msg);
            }
        }

        [Rpc(SendTo.Server)]
        public void AskHqRpc(OrderSlot slot, RpcParams p = default)
        {
            var role = RoleManager.Instance.RoleOf(p.Receive.SenderClientId);
            if (role != PlayerRole.Tent || askHqLeft.Value <= 0 || IsOver) return;
            askHqLeft.Value--;
            Send(role, new RadioMessage { kind = RadioKind.HqReply, from = PlayerRole.None, slot = slot, option = (sbyte)order.Value[slot] });
        }

        /// <summary>The village player's live count. Finishing at the hill ends the round.</summary>
        [Rpc(SendTo.Server)]
        public void ReportVillagersRpc(int savedCount, int lostCount, bool finished, RpcParams p = default)
        {
            if (IsOver || RoleManager.Instance.RoleOf(p.Receive.SenderClientId) != PlayerRole.Village) return;
            saved.Value = savedCount;
            lost.Value = lostCount;
            if (finished) EndRound(true);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void ShowRadioRpc(RadioMessage msg, RpcParams p) => RadioReceived?.Invoke(msg);

        // ---------- Server ----------

        void Send(PlayerRole to, RadioMessage msg)
        {
            ulong id = RoleManager.Instance.OwnerOf(to);
            if (id == RoleManager.NoOwner) return;
            ShowRadioRpc(msg, RpcTarget.Single(id, RpcTargetUse.Temp));
        }

        void StartRound()
        {
            rng = new System.Random(Environment.TickCount);
            levelIndex.Value = RoleManager.Instance != null ? RoleManager.Instance.levelIndex.Value : 0;
            var data = Orders;
            order.Value = new RelayCard
            {
                route = (sbyte)rng.Next(data.routeKeys.Length),
                avoid = (sbyte)rng.Next(data.avoidKeys.Length),
                gather = (sbyte)rng.Next(data.gatherKeys.Length),
                code = (sbyte)rng.Next(data.codeKeys.Length),
                submitted = true,
            };
            tentCard.Value = RelayCard.Empty;
            villageCard.Value = RelayCard.Empty;
            askHqLeft.Value = Level.askHqUses;
            saved.Value = lost.Value = 0;
            finishSeconds.Value = -1f;
            bool cutscene = Level.introCutscene && !introShown && !UI.CutscenePlayer.SkipNext;
            introShown |= cutscene;
            BriefingSeconds = cutscene ? CutsceneBriefing : ShortBriefing;
            playStart.Value = Now + BriefingSeconds;
            stormEnd.Value = playStart.Value + Level.stormSeconds;
            phase.Value = MatchPhase.Briefing;

            if (RoleManager.Instance.IsBot(PlayerRole.Van)) StartCoroutine(BotVanSpeaks());
            if (Level.rumorsEnabled) rumorLoop = StartCoroutine(RumorLoop());
        }

        void EndRound(bool reachedHill)
        {
            if (IsOver) return;
            int need = Mathf.CeilToInt(Level.villagerCount * Level.winFraction);
            finishSeconds.Value = (float)(Now - playStart.Value);
            phase.Value = reachedHill && saved.Value >= need ? MatchPhase.Won : MatchPhase.Lost;
            if (rumorLoop != null) StopCoroutine(rumorLoop);
            StopAllCoroutines();
        }

        /// <summary>Host only: replay the same level with a new order.</summary>
        public void Restart()
        {
            if (!IsServer) return;
            StopAllCoroutines();
            tentHeard = villageHeard = RelayCard.Empty;
            tentAskedAgain = villageAskedAgain = 0;
            tentVerifiedRoute = fakeSent = false;
            StartRound();
        }

        // ---------- Bots ----------

        IEnumerator BotVanSpeaks()
        {
            yield return new WaitForSeconds(BriefingSeconds + 3f);
            BotTransmit(PlayerRole.Van, PlayerRole.Tent, order.Value);
        }

        /// <summary>A bot says its card to the next role: text with gaps for a human, a noisy copy for a bot.</summary>
        void BotTransmit(PlayerRole from, PlayerRole to, RelayCard card)
        {
            if (IsOver) return;
            if (RoleManager.Instance.IsBot(to)) StartCoroutine(BotHear(to, from, card));
            else Send(to, new RadioMessage { kind = RadioKind.Order, from = from, card = card, seed = rng.Next(), drop = LinkDropChance(from, to) });
        }

        IEnumerator BotHear(PlayerRole bot, PlayerRole from, RelayCard spoken)
        {
            yield return new WaitForSeconds(2f);
            float drop = LinkDropChance(from, bot);
            var heard = bot == PlayerRole.Tent ? tentHeard : villageHeard;
            foreach (var s in OrderData.Slots)
            {
                // Each slot is heard right, lost (keeps what was heard before), or misheard as another option.
                double r = rng.NextDouble();
                if (r < drop * 0.6) continue;
                heard[s] = r < drop * 0.8 ? rng.Next(Orders.Options(s).Length) : spoken[s];
            }
            StoreHeard(bot, heard);

            int asked = bot == PlayerRole.Tent ? tentAskedAgain : villageAskedAgain;
            bool gaps = heard.route < 0 || heard.avoid < 0 || heard.gather < 0 || heard.code < 0;
            if (gaps && asked < 2)
            {
                if (bot == PlayerRole.Tent) tentAskedAgain++; else villageAskedAgain++;
                yield return BotSay(bot, from, QuickPhrase.SayAgain, heard);
                if (RoleManager.Instance.IsBot(from)) StartCoroutine(BotHear(bot, from, CardOf(from)));
                yield break;
            }
            // Read back once so a human sender can catch mistakes.
            yield return BotSay(bot, from, QuickPhrase.ReadBack, heard);
            yield return new WaitForSeconds(4f);
            BotPass(bot);
        }

        void StoreHeard(PlayerRole bot, RelayCard heard)
        {
            if (bot == PlayerRole.Tent) { tentHeard = heard; tentCard.Value = heard; }
            else { villageHeard = heard; villageCard.Value = heard; }
        }

        /// <summary>The bot fills any gap with a guess and acts: the tent passes it on, the village runs the evacuation.</summary>
        void BotPass(PlayerRole bot)
        {
            var card = bot == PlayerRole.Tent ? tentHeard : villageHeard;
            foreach (var s in OrderData.Slots)
                if (card[s] < 0) card[s] = rng.Next(Orders.Options(s).Length);
            card.submitted = true;
            StoreHeard(bot, card);
            if (bot == PlayerRole.Tent) BotTransmit(PlayerRole.Tent, PlayerRole.Village, card);
            else StartCoroutine(BotVillageEvacuates(card));
        }

        IEnumerator BotSay(PlayerRole from, PlayerRole to, QuickPhrase phrase, RelayCard card)
        {
            yield return new WaitForSeconds(1.5f);
            if (!RoleManager.Instance.IsBot(to))
                Send(to, new RadioMessage { kind = RadioKind.QuickChat, from = from, phrase = phrase, card = card, seed = rng.Next(), drop = LinkDropChance(from, to) * 0.5f });
        }

        /// <summary>How a bot answers a human's quick-chat.</summary>
        IEnumerator BotReply(PlayerRole bot, PlayerRole human, RadioMessage msg)
        {
            yield return new WaitForSeconds(1.5f);
            var mine = bot == PlayerRole.Van ? order.Value : bot == PlayerRole.Tent ? tentHeard : villageHeard;
            switch (msg.phrase)
            {
                case QuickPhrase.SayAgain:
                    // Repeat what this bot told the human.
                    BotTransmit(bot, human, mine);
                    break;
                case QuickPhrase.ReadBack:
                    bool upstream = (bot == PlayerRole.Van && human == PlayerRole.Tent) || (bot == PlayerRole.Tent && human == PlayerRole.Village);
                    if (!upstream) break;
                    bool right = msg.card.route == mine.route && msg.card.avoid == mine.avoid && msg.card.gather == mine.gather && msg.card.code == mine.code;
                    yield return BotSay(bot, human, right ? QuickPhrase.Correct : QuickPhrase.Wrong, mine);
                    if (!right) BotTransmit(bot, human, mine);
                    break;
            }
        }

        IEnumerator BotVillageEvacuates(RelayCard plan)
        {
            var truth = order.Value;
            // Wrong info costs time and lives, like it would for a human player.
            float seconds = 90f;
            float keep = 1f;
            if (plan.gather != truth.gather) { seconds += 60f; keep *= 0.8f; }
            if (plan.avoid != truth.avoid) keep *= 0.5f;
            if (plan.route != truth.route) keep *= 0.34f;
            yield return new WaitForSeconds(Mathf.Min(seconds, SecondsLeft + 1f));
            if (Now >= stormEnd.Value) yield break;
            int total = Level.villagerCount;
            saved.Value = Mathf.RoundToInt(total * keep);
            lost.Value = total - saved.Value;
            EndRound(true);
        }

        // ---------- Rumours (level 2) ----------

        IEnumerator RumorLoop()
        {
            yield return new WaitForSeconds(BriefingSeconds + 15f);
            while (!IsOver)
            {
                SendRumor();
                if (Level.fakeHqMessage && !fakeSent && Progress > 0.3f) SendFakeHq();
                var range = Level.rumorIntervalSeconds;
                yield return new WaitForSeconds(Mathf.Lerp(range.x, range.y, (float)rng.NextDouble()));
            }
        }

        void SendRumor()
        {
            var truth = order.Value;
            var type = (RumorType)rng.Next(4);
            int option = type switch
            {
                RumorType.RouteFlooded => truth.route,
                RumorType.RouteDry => Other(OrderSlot.Route, truth.route),
                RumorType.GatherWrong => Other(OrderSlot.Gather, truth.gather),
                _ => truth.avoid,
            };
            var msg = new RadioMessage { kind = RadioKind.Rumor, rumor = type, option = (sbyte)option, seed = rng.Next() };
            foreach (var role in new[] { PlayerRole.Tent, PlayerRole.Village })
            {
                if (RoleManager.Instance.IsBot(role)) BotHearsRumor(role, msg);
                else Send(role, msg);
            }
        }

        void SendFakeHq()
        {
            fakeSent = true;
            var fake = order.Value;
            fake.route = (sbyte)Other(OrderSlot.Route, fake.route);
            fake.code = (sbyte)Other(OrderSlot.Code, fake.code);
            var msg = new RadioMessage { kind = RadioKind.FakeHq, from = PlayerRole.None, card = fake, seed = rng.Next() };
            if (RoleManager.Instance.IsBot(PlayerRole.Tent))
            {
                // A careful bot checks the code word most of the time.
                if (rng.NextDouble() > 0.7 && !tentHeard.submitted) { var t = tentHeard; t.route = fake.route; tentHeard = t; }
            }
            else Send(PlayerRole.Tent, msg);
        }

        void BotHearsRumor(PlayerRole bot, RadioMessage msg)
        {
            var card = bot == PlayerRole.Tent ? tentHeard : villageHeard;
            if (card.submitted || rng.NextDouble() > 0.25) return;
            if (bot == PlayerRole.Tent && msg.rumor is RumorType.RouteFlooded or RumorType.RouteDry && !tentVerifiedRoute && askHqLeft.Value > 0)
            {
                // The tent bot verifies with the source instead of trusting the shout.
                askHqLeft.Value--;
                tentVerifiedRoute = true;
                card.route = order.Value.route;
            }
            else if (msg.rumor == RumorType.RouteDry) card.route = msg.option;
            else if (msg.rumor == RumorType.GatherWrong) card.gather = msg.option;
            StoreHeard(bot, card);
        }

        int Other(OrderSlot slot, int not)
        {
            int n = Orders.Options(slot).Length;
            return (not + 1 + rng.Next(n - 1)) % n;
        }
    }
}
