using System.Collections.Generic;
using SayItAgain.Net;
using SayItAgain.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain
{
    /// <summary>
    /// Runs the village side of a round on the village player's machine: places villagers at the true
    /// gathering point, lets the player call them (E), applies flooded zones, puts rumours in villagers'
    /// mouths and reports the count to the server.
    /// </summary>
    public sealed class VillageDirector : MonoBehaviour
    {
        public static VillageDirector Instance { get; private set; }

        public Villager villagerPrefab;
        public float callRadius = 9f;
        [Tooltip("Villagers who shout (rumours in level 2, panic in level 1). They never follow.")]
        public WorldLabel[] shouters;

        public bool PlayerInWater { get; private set; }
        public int Following { get; private set; }
        public int Saved { get; private set; }
        public int Lost { get; private set; }
        /// <summary>Set for a moment after pressing E, for the HUD.</summary>
        public float LastCallTime { get; private set; } = -10f;

        readonly List<Villager> villagers = new List<Villager>();
        InputAction call;
        bool configured, finished;
        int reportedSaved = -1, reportedLost = -1;
        float nextPanicLine;

        void Awake()
        {
            Instance = this;
            call = new InputAction("CallVillagers", InputActionType.Button, "<Keyboard>/e");
        }

        void OnEnable()
        {
            call.Enable();
            MatchManager.RadioReceived += OnRadio;
        }

        void OnDisable()
        {
            call.Disable();
            MatchManager.RadioReceived -= OnRadio;
        }

        void OnDestroy()
        {
            call.Dispose();
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var match = MatchManager.Instance;
            if (match == null || match.order.Value.route < 0) return;
            if (!configured) Configure(match);
            if (GameplayBootstrap.LocalRole != PlayerRole.Village || GameplayBootstrap.LocalRig == null) return;

            var player = GameplayBootstrap.LocalRig.transform;
            var fpc = GameplayBootstrap.LocalRig.GetComponent<FirstPersonController>();
            PlayerInWater = HazardZone.IsDeadlyAt(player.position);
            fpc.speedMultiplier = PlayerInWater ? 0.3f : 1f;

            if (match.IsOver) return;
            if (call.WasPressedThisFrame() && !fpc.InputBlocked) CallVillagersNear(player);
            CountAndReport(match, player);
            PanicChatter(match);
        }

        void Configure(MatchManager match)
        {
            configured = true;
            var truth = match.order.Value;
            foreach (var z in HazardZone.All) z.Configure(truth);
            if (GameplayBootstrap.LocalRole != PlayerRole.Village) return;

            var rng = new System.Random(truth.GetHashCode());
            foreach (var gp in FindObjectsByType<GatherPoint>(FindObjectsSortMode.None))
            {
                if (gp.optionIndex != truth.gather) continue;
                for (int i = 0; i < match.Level.villagerCount; i++)
                {
                    var v = Instantiate(villagerPrefab, gp.RandomSpot(rng), Quaternion.Euler(0, rng.Next(360), 0));
                    v.name = "Villager_" + i;
                    villagers.Add(v);
                }
            }
        }

        public void CallVillagersNear(Transform player)
        {
            LastCallTime = Time.time;
            foreach (var v in villagers)
                if (v.Current == Villager.State.Waiting && Vector3.Distance(v.transform.position, player.position) <= callRadius)
                    v.Follow(player);
        }

        void CountAndReport(MatchManager match, Transform player)
        {
            int following = 0, saved = 0, lost = 0;
            foreach (var v in villagers)
            {
                if (v.Current == Villager.State.Following) following++;
                else if (v.Current == Villager.State.Safe) saved++;
                else if (v.Current == Villager.State.Swept) lost++;
            }
            Following = following;
            Saved = saved;
            Lost = lost;

            // The round ends when the player stands on the hill with nobody still on the way.
            bool atHill = SafeZone.Instance != null && SafeZone.Instance.Contains(player.position);
            bool done = atHill && following == 0;
            if (saved != reportedSaved || lost != reportedLost || (done && !finished))
            {
                reportedSaved = saved;
                reportedLost = lost;
                finished |= done;
                // Villagers left behind count as lost when the round finishes.
                int leftBehind = done ? villagers.Count - saved - lost : 0;
                match.ReportVillagersRpc(saved, lost + leftBehind, done);
            }
        }

        void OnRadio(RadioMessage msg)
        {
            if (msg.kind != RadioKind.Rumor || GameplayBootstrap.LocalRig == null || shouters.Length == 0) return;
            // The closest shouter yells it, so the player hears it "in the street".
            var player = GameplayBootstrap.LocalRig.transform.position;
            WorldLabel best = shouters[0];
            foreach (var s in shouters)
                if (Vector3.Distance(s.transform.position, player) < Vector3.Distance(best.transform.position, player)) best = s;
            best.Say(RadioFeedText.Rumor(msg), 7f);
        }

        void PanicChatter(MatchManager match)
        {
            if (match.Level.rumorsEnabled || Time.time < nextPanicLine || shouters.Length == 0) return;
            nextPanicLine = Time.time + Random.Range(6f, 12f);
            shouters[Random.Range(0, shouters.Length)].Say(Loc.Get("panic_" + Random.Range(0, 3)), 4f);
        }
    }
}
