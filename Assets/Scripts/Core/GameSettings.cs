using System;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>Audio, voice and quality options persisted in PlayerPrefs. Comfort options live in ComfortSettings.</summary>
    public static class GameSettings
    {
        public static event Action Changed;

        public static float MasterVolume { get => Get("set_master", 0.9f); set { Set("set_master", value); AudioListener.volume = value; } }
        public static float VoiceVolume { get => Get("set_voice", 1f); set => Set("set_voice", value); }
        public static float SfxVolume { get => Get("set_sfx", 0.8f); set => Set("set_sfx", value); }

        /// <summary>On: hold V to talk. Off: open mic, Vivox voice activity detection decides.</summary>
        public static bool PushToTalk
        {
            get => PlayerPrefs.GetInt("set_ptt", 1) == 1;
            set { PlayerPrefs.SetInt("set_ptt", value ? 1 : 0); Changed?.Invoke(); }
        }

        /// <summary>Open-mic sensitivity 0..1 (higher = picks up quieter speech).</summary>
        public static float VoiceThreshold { get => Get("set_vad", 0.45f); set => Set("set_vad", value); }

        /// <summary>Microphone name; empty = system default.</summary>
        public static string MicDevice
        {
            get => PlayerPrefs.GetString("set_mic", string.Empty);
            set { PlayerPrefs.SetString("set_mic", value ?? string.Empty); Changed?.Invoke(); }
        }

        /// <summary>0 = low, 1 = medium, 2 = high. Drives rain particles, fog distance and shadows.</summary>
        public static int Quality
        {
            get => PlayerPrefs.GetInt("set_quality", 1);
            set { PlayerPrefs.SetInt("set_quality", Mathf.Clamp(value, 0, 2)); Changed?.Invoke(); }
        }

        public static void ApplyOnBoot() => AudioListener.volume = MasterVolume;

        static float Get(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);

        static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            Changed?.Invoke();
        }
    }
}
