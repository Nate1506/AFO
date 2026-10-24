using SayItAgain.Net;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>Raises the flood water with the storm clock, so the water level is a visual timer.</summary>
    public sealed class FloodRise : MonoBehaviour
    {
        public float startY = -0.6f;
        public float endY = 0.9f;
        [Tooltip("This water is the real flood: deep water under it is deadly (see HazardZone.WaterLevel).")]
        public bool drivesWaterLevel;

        void OnDisable() { if (drivesWaterLevel) HazardZone.WaterLevel = float.NegativeInfinity; }

        /// <summary>The cutscene drives the water itself; negative = follow the storm clock.</summary>
        public static float ProgressOverride = -1f;

        void Update()
        {
            var match = MatchManager.Instance;
            float t = ProgressOverride >= 0f ? ProgressOverride : match != null ? match.Progress : 0f;
            var p = transform.position;
            p.y = Mathf.Lerp(startY, endY, t * t);
            transform.position = p;
            if (drivesWaterLevel) HazardZone.WaterLevel = p.y;
        }
    }
}
