using System.Collections.Generic;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// Deep water. Villagers who walk in are swept away; the player wades slowly.
    /// A zone can be bound to an order option: it is only deadly when that option matches (or does not match) the HQ order.
    /// </summary>
    public sealed class HazardZone : MonoBehaviour
    {
        public enum Rule
        {
            /// <summary>Always deadly (the flooded field, the river).</summary>
            Always,
            /// <summary>Deadly unless this route is the safe route in the order.</summary>
            UnlessSafeRoute,
            /// <summary>Deadly only if this crossing is the one HQ said to avoid.</summary>
            IfAvoidedCrossing,
        }

        public static readonly List<HazardZone> All = new List<HazardZone>();

        public Rule rule = Rule.Always;
        public int optionIndex;
        public Vector3 size = new Vector3(10, 4, 10);
        [Tooltip("Water surface shown when the zone is deadly.")]
        public GameObject waterVisual;

        public bool Deadly { get; private set; } = true;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Called once the HQ order is known.</summary>
        public void Configure(RelayCard truth)
        {
            Deadly = rule switch
            {
                Rule.UnlessSafeRoute => truth.route != optionIndex,
                Rule.IfAvoidedCrossing => truth.avoid == optionIndex,
                _ => true,
            };
            if (waterVisual != null) waterVisual.SetActive(Deadly || rule == Rule.Always);
        }

        public bool Contains(Vector3 worldPos)
        {
            var local = transform.InverseTransformPoint(worldPos);
            var half = size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z && local.y >= -1f && local.y <= half.y * 2f;
        }

        /// <summary>
        /// Deadly at this point? Bridges and causeways are safe patches on top of always-deadly water,
        /// so a safe bound zone overrides an Always zone it overlaps.
        /// </summary>
        /// <summary>Current flood surface height (set by FloodRise). Deep water below it sweeps villagers away.</summary>
        public static float WaterLevel = float.NegativeInfinity;
        /// <summary>Feet this far under the surface count as deep water.</summary>
        public const float DeepWaterDepth = 0.45f;

        /// <summary>Rule of the zone that last made IsDeadlyAt true (for the right warning text).</summary>
        public static Rule LastDeadlyRule { get; private set; }

        public static bool IsDeadlyAt(Vector3 pos)
        {
            // Deep open water anywhere on the map (bridges and raised paths keep feet above it).
            bool inAlways = pos.y < WaterLevel - DeepWaterDepth;
            // Where bound zones overlap (two hill stairs side by side), the one whose centre line is
            // closest decides, so standing on the safe stairs is never judged by the neighbour.
            HazardZone best = null;
            float bestSide = float.MaxValue;
            foreach (var z in All)
            {
                if (!z.Contains(pos)) continue;
                if (z.rule == Rule.Always) { inAlways = true; continue; }
                float side = Mathf.Abs(z.transform.InverseTransformPoint(pos).x) / Mathf.Max(0.01f, z.size.x * 0.5f);
                if (side < bestSide) { bestSide = side; best = z; }
            }
            if (best != null)
            {
                if (best.Deadly) LastDeadlyRule = best.rule;
                return best.Deadly;
            }
            if (inAlways) LastDeadlyRule = Rule.Always;
            return inAlways;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = rule == Rule.Always ? new Color(0, 0.4f, 1, 0.25f) : new Color(1, 0.5f, 0, 0.35f);
            Gizmos.DrawCube(new Vector3(0, size.y * 0.5f, 0), size);
        }
    }
}
