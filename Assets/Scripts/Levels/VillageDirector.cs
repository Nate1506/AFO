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
        [Tooltip("Real recorded voices (CC0): a villager answering the call, a cheer on the hill.")]
        public AudioClip followClip, cheerClip;
        public AudioClip[] sweptClips;
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
        readonly HashSet<Villager> screamed = new HashSet<Villager>();
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
            RecordTrail(player.position);
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
                    var v = Instantiate(villagerPrefab, ReachableSpot(gp, rng), Quaternion.Euler(0, rng.Next(360), 0));
                    v.name = "Villager_" + i;
                    villagers.Add(v);
                }
            }
        }

        /// <summary>
        /// A spot near the gathering point with a walkable path to the hill, so nobody starts inside a
        /// building or on a roof that the NavMesh cannot leave.
        /// </summary>
        static Vector3 ReachableSpot(GatherPoint gp, System.Random rng)
        {
            var goal = SafeZone.Instance != null ? SafeZone.Instance.transform.position : gp.transform.position;
            UnityEngine.AI.NavMesh.SamplePosition(goal, out var goalHit, 8f, UnityEngine.AI.NavMesh.AllAreas);
            var path = new UnityEngine.AI.NavMeshPath();
            for (int tries = 0; tries < 30; tries++)
            {
                var spot = gp.RandomSpot(rng);
                if (UnityEngine.AI.NavMesh.SamplePosition(spot, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)
                    && UnityEngine.AI.NavMesh.CalculatePath(hit.position, goalHit.position, UnityEngine.AI.NavMesh.AllAreas, path)
                    && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    return hit.position;
            }
            return gp.transform.position;
        }

        public void CallVillagersNear(Transform player)
        {
            LastCallTime = Time.time;
            if (trail.Count == 0) trail.Add(player.position);
            Villager first = null;
            foreach (var v in villagers)
                if (v.Current == Villager.State.Waiting && Vector3.Distance(v.transform.position, player.position) <= callRadius)
                {
                    v.Follow(this, followSlots++);
                    if (first == null) first = v;
                }
            if (first != null && followClip != null) AudioSource.PlayClipAtPoint(followClip, first.transform.position + Vector3.up * 1.6f, GameSettings.SfxVolume);
        }

        /// <summary>The player's recent path, newest last, about one point per metre.</summary>
        readonly List<Vector3> trail = new List<Vector3>();
        /// <summary>How many old points were dropped from the front, so trail indices stay stable.</summary>
        int trailBase;
        int followSlots;

        void RecordTrail(Vector3 p)
        {
            if (trail.Count == 0 || (trail[trail.Count - 1] - p).sqrMagnitude > 1f) trail.Add(p);
            if (trail.Count > 3000) { trail.RemoveAt(0); trailBase++; }
        }

        /// <summary>Index one past the newest trail point.</summary>
        public int TrailEnd => trailBase + trail.Count;

        /// <summary>Trail point by stable index (clamped to what is still kept).</summary>
        public Vector3 TrailAt(int index) =>
            trail.Count == 0 ? GameplayBootstrap.LocalRig.transform.position : trail[Mathf.Clamp(index - trailBase, 0, trail.Count - 1)];

        /// <summary>A point on the player's path, this many metres behind the player.</summary>
        public Vector3 TrailPoint(float metresBehind)
        {
            if (trail.Count == 0) return GameplayBootstrap.LocalRig.transform.position;
            var current = GameplayBootstrap.LocalRig.transform.position;
            float left = metresBehind;
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                float step = Vector3.Distance(current, trail[i]);
                if (step >= left) return Vector3.Lerp(current, trail[i], left / Mathf.Max(step, 0.001f));
                left -= step;
                current = trail[i];
            }
            return trail[0];
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
            // Voices: a cheer for the first villager safe on the hill, a scream for each one the water takes.
            if (saved > 0 && Saved == 0 && cheerClip != null) AudioSource.PlayClipAtPoint(cheerClip, player.position + player.forward, GameSettings.SfxVolume);
            if (lost > Lost && sweptClips != null && sweptClips.Length > 0)
                foreach (var v in villagers)
                    if (v.Current == Villager.State.Swept && !screamed.Contains(v))
                    {
                        screamed.Add(v);
                        AudioSource.PlayClipAtPoint(sweptClips[Random.Range(0, sweptClips.Length)], v.transform.position, GameSettings.SfxVolume);
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
