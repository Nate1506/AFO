using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SayItAgain.UI
{
    public enum UiSound { Hover, Select, Back, Confirm, Pause, Error, Tick, Success, Fail, Result }

    /// <summary>Short UI sounds (Kenney Interface Sounds, CC0) from Resources/Audio/UI, at the effects volume.</summary>
    public static class UiSfx
    {
        static AudioSource source;
        static readonly Dictionary<UiSound, AudioClip> clips = new Dictionary<UiSound, AudioClip>();

        public static void Play(UiSound sound, float volume = 1f)
        {
            if (source == null)
            {
                var go = new GameObject("UiSfx");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true;
            }
            if (!clips.TryGetValue(sound, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/UI/ui_" + sound.ToString().ToLowerInvariant());
                clips[sound] = clip;
            }
            if (clip != null) source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }
    }

    /// <summary>Hover and click sounds for a UiKit button.</summary>
    public sealed class ButtonSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public UiSound click = UiSound.Select;

        void Start()
        {
            var b = GetComponent<UnityEngine.UI.Button>();
            if (b != null) b.onClick.AddListener(() => UiSfx.Play(click));
        }

        public void OnPointerEnter(PointerEventData _) => UiSfx.Play(UiSound.Hover, 0.5f);
        public void OnSelect(BaseEventData _) => UiSfx.Play(UiSound.Hover, 0.5f);
    }
}
