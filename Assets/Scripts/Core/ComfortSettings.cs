using System;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>Player comfort options persisted in PlayerPrefs. Read by the controller, binoculars and effects.</summary>
    public static class ComfortSettings
    {
        public static event Action Changed;

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat("set_sens", 1f);
            set { PlayerPrefs.SetFloat("set_sens", Mathf.Clamp(value, 0.1f, 4f)); Changed?.Invoke(); }
        }

        public static float FieldOfView
        {
            get => PlayerPrefs.GetFloat("set_fov", 70f);
            set { PlayerPrefs.SetFloat("set_fov", Mathf.Clamp(value, 55f, 95f)); Changed?.Invoke(); }
        }

        /// <summary>Off by default for motion comfort.</summary>
        public static bool HeadBob
        {
            get => PlayerPrefs.GetInt("set_headbob", 0) == 1;
            set { PlayerPrefs.SetInt("set_headbob", value ? 1 : 0); Changed?.Invoke(); }
        }

        /// <summary>Instant zoom, no camera shake, gentler lightning.</summary>
        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt("set_reducemotion", 0) == 1;
            set { PlayerPrefs.SetInt("set_reducemotion", value ? 1 : 0); Changed?.Invoke(); }
        }
    }
}
