using System.Collections;
using SayItAgain.Voice;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// The storm look and sound, driven by the same StormState / WeatherConfig as the radio:
    /// camera-following rain, distance fog, dim cold light, photosensitivity-safe lightning with
    /// delayed thunder, rain/wind loops. Quality setting scales particles and fog.
    /// </summary>
    public sealed class StormWeather : MonoBehaviour
    {
        public Light sun;
        public Material rainMaterial;
        public AudioSource rainLoop, windLoop;
        public AudioClip[] thunderClips;
        [Tooltip("Never flash more often than this (photosensitivity: well under 3 per second).")]
        public float minFlashGap = 4f;

        ParticleSystem rain;
        AudioSource thunderSource;
        float baseSun, nextFlash, flash;
        Color baseAmbient;
        static readonly int[] RainByQuality = { 1200, 3000, 6000 };

        /// <summary>Cutscene sets this to drive intensity directly; negative = use StormState.</summary>
        public static float IntensityOverride = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { IntensityOverride = -1f; FloodRise.ProgressOverride = -1f; }
        public static StormWeather Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            baseSun = sun != null ? sun.intensity : 1f;
            baseAmbient = RenderSettings.ambientLight;
            thunderSource = gameObject.AddComponent<AudioSource>();
            thunderSource.spatialBlend = 0f;
            BuildRain();
            nextFlash = Time.time + 6f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        float Intensity => IntensityOverride >= 0f ? IntensityOverride : StormState.Intensity;

        void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 24f;
            main.startSize = 0.025f;
            main.startColor = new Color(0.75f, 0.8f, 0.85f, 0.45f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = RainByQuality[2];
            main.gravityModifier = 0.4f;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 36f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = rain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-3f, -1.5f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 4f;
            r.velocityScale = 0.06f;
            r.sharedMaterial = rainMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            rain.Play();
        }

        void LateUpdate()
        {
            float k = Intensity;
            var cam = Camera.main;
            if (cam != null)
            {
                rain.transform.position = cam.transform.position + Vector3.up * 12f + cam.transform.forward * 6f;
                // Overcast: the sky is the fog colour, so distant shapes fade into the storm.
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = RenderSettings.fogColor;
            }

            int q = GameSettings.Quality;
            var emission = rain.emission;
            emission.rateOverTime = RainByQuality[q] * Mathf.Lerp(0.3f, 1f, k) / 1.1f;

            var cfg = StormState.Config;
            float fogMin = cfg != null ? cfg.fogDensityMin : 0.004f, fogMax = cfg != null ? cfg.fogDensityMax : 0.018f;
            // Low quality pulls fog in a little to hide the short draw distance.
            // Binoculars cut through some of the haze so far landmarks stay readable.
            var rig = Net.GameplayBootstrap.LocalRig;
            float zoom = rig != null ? rig.GetComponent<Binoculars>().ZoomAmount : 0f;
            RenderSettings.fogDensity = Mathf.Lerp(fogMin, fogMax, k) * (q == 0 ? 1.25f : 1f) * Mathf.Lerp(1f, 0.35f, zoom);
            // Low still reaches the hill from the van (about 400 m) so the binoculars stay useful.
            if (cam != null) cam.farClipPlane = q == 0 ? 450f : q == 1 ? 650f : 1000f;

            float sfx = GameSettings.SfxVolume;
            if (rainLoop != null) rainLoop.volume = sfx * Mathf.Lerp(0.35f, 1f, k);
            if (windLoop != null) windLoop.volume = sfx * Mathf.Lerp(0.15f, 0.7f, k);

            UpdateLightning(k, cfg);
        }

        void UpdateLightning(float k, WeatherConfig cfg)
        {
            if (Time.time >= nextFlash)
            {
                var range = cfg != null ? cfg.lightningIntervalRange : new Vector2(18f, 7f);
                nextFlash = Time.time + Mathf.Max(minFlashGap, Mathf.Lerp(range.x, range.y, k) * Random.Range(0.7f, 1.3f));
                Strike(Random.Range(0.3f, 1f));
            }
            // One soft pulse that fades; reduce motion keeps it dimmer.
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * (ComfortSettings.ReduceMotion ? 1.2f : 2.5f));
            float peak = ComfortSettings.ReduceMotion ? 1.2f : 3f;
            if (sun != null) sun.intensity = baseSun * Mathf.Lerp(1f, 0.55f, k) + flash * peak;
            RenderSettings.ambientLight = baseAmbient * Mathf.Lerp(1f, 0.6f, k) + Color.white * flash * 0.35f;
        }

        /// <summary>Flash now, thunder after a distance delay. Also buries radio voice in static.</summary>
        public void Strike(float closeness)
        {
            flash = Mathf.Max(flash, Mathf.Lerp(0.4f, 1f, closeness));
            StartCoroutine(Thunder(closeness));
        }

        IEnumerator Thunder(float closeness)
        {
            yield return new WaitForSeconds(Mathf.Lerp(3.5f, 0.6f, closeness));
            StormState.RaiseThunder(closeness);
            if (thunderClips != null && thunderClips.Length > 0)
                thunderSource.PlayOneShot(thunderClips[Random.Range(0, thunderClips.Length)], GameSettings.SfxVolume * Mathf.Lerp(0.5f, 1f, closeness));
        }
    }
}
