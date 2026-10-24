using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// One storm profile. The same values drive the visuals (rain, fog, wind, lightning)
    /// and the radio distortion, so what players see matches what they hear.
    /// </summary>
    [CreateAssetMenu(menuName = "Say It Again/Weather Config", fileName = "Weather_")]
    public sealed class WeatherConfig : ScriptableObject
    {
        [Header("Storm curve")]
        [Tooltip("Storm intensity 0..1 over the level's normalized time (0 = start, 1 = storm clock ends).")]
        public AnimationCurve intensityOverTime = AnimationCurve.Linear(0f, 0.35f, 1f, 1f);

        [Header("Visuals")]
        [Range(0f, 1f)] public float rainAtFullIntensity = 1f;
        public float fogDensityMin = 0.004f;
        public float fogDensityMax = 0.018f;
        [Range(0f, 1f)] public float windAtFullIntensity = 0.9f;
        [Tooltip("Seconds between lightning flashes at low / full intensity. Never below 4 s (photosensitivity).")]
        public Vector2 lightningIntervalRange = new Vector2(18f, 7f);

        [Header("Radio")]
        [Tooltip("Static added per kilometre of link distance.")]
        [Range(0f, 0.2f)] public float noisePerKm = 0.03f;
        [Range(0f, 0.5f)] public float baseNoise = 0.03f;
        [Tooltip("Extra static at full storm intensity (rain on the antenna).")]
        [Range(0f, 0.5f)] public float rainNoise = 0.12f;
        [Tooltip("Voice gain lost per kilometre.")]
        [Range(0f, 0.3f)] public float gainLossPerKm = 0.05f;
        [Tooltip("Dropouts per minute at full intensity.")]
        public float dropoutsPerMinute = 10f;
        public Vector2 dropoutSeconds = new Vector2(0.25f, 0.9f);
        [Tooltip("How long a thunder clap buries the voice in static.")]
        public float thunderMaskSeconds = 1.4f;
        public float bandLowHz = 320f;
        public float bandHighHz = 2800f;
        [Range(1f, 6f)] public float drive = 2.2f;

        public float IntensityAt(float normalizedTime) =>
            Mathf.Clamp01(intensityOverTime.Evaluate(Mathf.Clamp01(normalizedTime)));
    }
}
