using System;

namespace SayItAgain.Voice
{
    /// <summary>
    /// Live storm values shared by weather visuals and the radio. The weather system writes,
    /// radio filters read (from the audio thread, so only plain fields).
    /// </summary>
    public static class StormState
    {
        public static volatile float Intensity = 0.35f;
        public static WeatherConfig Config;

        /// <summary>Raised on the main thread when thunder hits; radios bury the voice in static.</summary>
        public static event Action<float> Thunder;

        public static void RaiseThunder(float loudness01) => Thunder?.Invoke(loudness01);
    }
}
