using UnityEngine;

namespace SayItAgain.Voice
{
    /// <summary>
    /// Walkie-talkie DSP on one received voice: band-pass, overdrive, static that grows with distance
    /// and storm, random dropouts, and thunder masking. Runs on the audio thread after the Vivox tap.
    /// Nothing is recorded or stored.
    /// </summary>
    public sealed class RadioDistortion : MonoBehaviour
    {
        public float distanceKm = 3f;

        /// <summary>0..1 for the HUD signal meter (main thread).</summary>
        public float SignalQuality { get; private set; } = 1f;
        public bool InDropout => dropoutGain < 0.5f;

        // Copied from WeatherConfig on the main thread; read on the audio thread.
        volatile float noise, gain, dropoutsPerSec, dropMin, dropMax, lowHz = 320f, highHz = 2800f, drive = 2.2f;
        volatile float thunderUntil;
        volatile float dspNow;
        float sampleRate;
        System.Random rng;

        readonly float[] hpPrevIn = new float[2], hpPrevOut = new float[2], lpPrev = new float[2];
        double dropoutEnd, nextDropoutCheck;
        volatile float dropoutGain = 1f;

        void Awake()
        {
            sampleRate = AudioSettings.outputSampleRate;
            rng = new System.Random(GetInstanceID());
            Update();
        }

        void OnEnable() => StormState.Thunder += OnThunder;
        void OnDisable() => StormState.Thunder -= OnThunder;

        void Update()
        {
            var cfg = StormState.Config;
            float intensity = StormState.Intensity;
            if (cfg != null)
            {
                noise = cfg.baseNoise + cfg.noisePerKm * distanceKm + cfg.rainNoise * intensity;
                gain = Mathf.Clamp01(1f - cfg.gainLossPerKm * distanceKm);
                dropoutsPerSec = cfg.dropoutsPerMinute / 60f * intensity;
                dropMin = cfg.dropoutSeconds.x;
                dropMax = cfg.dropoutSeconds.y;
                lowHz = cfg.bandLowHz;
                highHz = cfg.bandHighHz;
                drive = cfg.drive;
            }
            else
            {
                noise = 0.1f + 0.1f * intensity;
                gain = 0.85f;
                dropoutsPerSec = intensity * 0.15f;
                dropMin = 0.25f;
                dropMax = 0.9f;
            }
            dspNow = (float)AudioSettings.dspTime;
            bool thunder = dspNow < thunderUntil;
            SignalQuality = Mathf.Clamp01(gain * dropoutGain * (thunder ? 0.2f : 1f) - noise);
        }

        void OnThunder(float loudness)
        {
            var cfg = StormState.Config;
            float seconds = (cfg != null ? cfg.thunderMaskSeconds : 1.4f) * Mathf.Lerp(0.5f, 1f, loudness);
            thunderUntil = (float)AudioSettings.dspTime + seconds;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            float dt = 1f / sampleRate;
            float hpA = HighPassCoeff(lowHz, dt);
            float lpA = LowPassCoeff(highHz, dt);
            double now = dspNow;
            bool thunder = now < thunderUntil;
            int ch = Mathf.Min(channels, 2);

            for (int i = 0; i < data.Length; i += channels)
            {
                now += dt;
                UpdateDropout(now);
                float voiceGain = gain * dropoutGain * (thunder ? 0.15f : 1f);
                float staticLevel = noise + (1f - dropoutGain) * 0.25f + (thunder ? 0.35f : 0f);

                for (int c = 0; c < ch; c++)
                {
                    float x = data[i + c];
                    // One-pole high-pass then low-pass = telephone band.
                    float hp = hpA * (hpPrevOut[c] + x - hpPrevIn[c]);
                    hpPrevIn[c] = x;
                    hpPrevOut[c] = hp;
                    lpPrev[c] += lpA * (hp - lpPrev[c]);
                    float y = SoftClip(lpPrev[c] * drive) * voiceGain;
                    y += ((float)rng.NextDouble() * 2f - 1f) * staticLevel * 0.3f;
                    data[i + c] = Mathf.Clamp(y, -1f, 1f);
                }
                for (int c = ch; c < channels; c++) data[i + c] = data[i];
            }
        }

        void UpdateDropout(double now)
        {
            if (now < dropoutEnd)
            {
                dropoutGain = Mathf.MoveTowards(dropoutGain, 0f, 0.02f);
                return;
            }
            dropoutGain = Mathf.MoveTowards(dropoutGain, 1f, 0.01f);
            if (now < nextDropoutCheck) return;
            nextDropoutCheck = now + 0.1;
            if (rng.NextDouble() < dropoutsPerSec * 0.1)
                dropoutEnd = now + Mathf.Lerp(dropMin, dropMax, (float)rng.NextDouble());
        }

        static float SoftClip(float x) => x / (1f + Mathf.Abs(x));
        static float HighPassCoeff(float hz, float dt) { float rc = 1f / (2f * Mathf.PI * hz); return rc / (rc + dt); }
        static float LowPassCoeff(float hz, float dt) { float rc = 1f / (2f * Mathf.PI * hz); return dt / (rc + dt); }
    }
}
