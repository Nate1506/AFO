using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SayItAgain.Net;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain.Voice
{
    public enum VoiceState { Off, Connecting, Ready, Failed }

    /// <summary>
    /// Room voice over Vivox. One channel per room; every client only hears its neighbours in the relay chain,
    /// each through a RadioDistortion filter. Push-to-talk on V. Voice is never recorded or stored.
    /// Lives on the NetworkManager object.
    /// </summary>
    public sealed class VoiceService : MonoBehaviour
    {
        public static VoiceService Instance { get; private set; }

        public WeatherConfig defaultWeather;
        [Tooltip("Optional CC0 radio click played on press / release of push-to-talk.")]
        public AudioClip pttOnClip, pttOffClip;
        [Tooltip("Crackle when thunder hits the line.")]
        public AudioClip glitchClip;

        public VoiceState State { get; private set; } = VoiceState.Off;
        public bool IsTransmitting { get; private set; }
        public event Action Changed;

        readonly Dictionary<string, VivoxParticipant> participants = new Dictionary<string, VivoxParticipant>();
        readonly Dictionary<string, RadioDistortion> taps = new Dictionary<string, RadioDistortion>();
        readonly Dictionary<PlayerRole, bool> speaking = new Dictionary<PlayerRole, bool>();
        VivoxParticipant self;
        InputAction ptt;
        AudioSource sfx;
        string channelName;
        bool vivoxReady;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            ptt = new InputAction("PushToTalk", InputActionType.Button, "<Keyboard>/v");
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            if (StormState.Config == null) StormState.Config = defaultWeather;
        }

        void OnEnable() => ptt.Enable();
        void OnDisable() => ptt.Disable();

        void Start()
        {
            var session = SessionService.Instance;
            session.RoomEntered += OnRoomEntered;
            session.RoomExited += OnRoomExited;
            RoleManager.RolesChanged += ApplyRouting;
            GameSettings.Changed += ApplyDeviceSettings;
            StormState.Thunder += OnThunder;
        }

        void OnThunder(float loudness)
        {
            bool inRoom = SessionService.Instance != null && SessionService.Instance.InRoom;
            if (inRoom && glitchClip != null) sfx.PlayOneShot(glitchClip, 0.35f * loudness * GameSettings.SfxVolume);
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            ptt.Dispose();
            if (SessionService.Instance != null)
            {
                SessionService.Instance.RoomEntered -= OnRoomEntered;
                SessionService.Instance.RoomExited -= OnRoomExited;
            }
            RoleManager.RolesChanged -= ApplyRouting;
            GameSettings.Changed -= ApplyDeviceSettings;
            StormState.Thunder -= OnThunder;
            UnhookVivox();
        }

        void Update()
        {
            bool held = GameSettings.PushToTalk ? ptt.IsPressed() && !TextInputFocused() : true;
            bool want = State == VoiceState.Ready && held;
            if (want != IsTransmitting) SetTransmitting(want);
            foreach (var t in taps.Values)
                if (t != null) t.GetComponent<AudioSource>().volume = GameSettings.VoiceVolume;
        }

        /// <summary>Re-apply mic choice and open-mic sensitivity (called when settings change).</summary>
        async void ApplyDeviceSettings()
        {
            if (!vivoxReady || !VivoxService.Instance.IsLoggedIn) return;
            try
            {
                var vivox = VivoxService.Instance;
                string wanted = GameSettings.MicDevice;
                if (!string.IsNullOrEmpty(wanted))
                    foreach (var d in vivox.AvailableInputDevices)
                        if (d.DeviceName.Contains(wanted)) { await vivox.SetActiveInputDeviceAsync(d); break; }
                int sensitivity = Mathf.RoundToInt(GameSettings.VoiceThreshold * 100f);
                await vivox.SetVoiceActivityDetectionPropertiesAsync(sensitivity: sensitivity);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Voice] Device settings failed: {e.Message}");
            }
        }

        public bool IsSpeaking(PlayerRole role) => speaking.TryGetValue(role, out var s) && s;

        /// <summary>Worst signal among the links this player can hear, for the HUD meter.</summary>
        public float SignalQuality
        {
            get
            {
                float q = 1f;
                foreach (var t in taps.Values) if (t != null) q = Mathf.Min(q, t.SignalQuality);
                return taps.Count == 0 ? 0f : q;
            }
        }

        async void OnRoomEntered()
        {
            if (SessionService.Instance.IsSolo) return;
            channelName = "sia_" + SessionService.Instance.RoomCode;
            await ConnectAsync();
        }

        async void OnRoomExited()
        {
            SetTransmitting(false);
            ClearTaps();
            if (!vivoxReady) { SetState(VoiceState.Off); return; }
            try { await VivoxService.Instance.LeaveAllChannelsAsync(); }
            catch (Exception e) { Debug.LogWarning($"[Voice] Leave failed: {e.Message}"); }
            SetState(VoiceState.Off);
        }

        async Task ConnectAsync()
        {
            SetState(VoiceState.Connecting);
            try
            {
                var vivox = VivoxService.Instance;
                if (!vivoxReady)
                {
                    if (vivox.InitializationState != VivoxInitializationState.Initialized)
                        await vivox.InitializeAsync();
                    vivox.ChannelJoined += OnChannelJoined;
                    vivox.ParticipantAddedToChannel += OnParticipantAdded;
                    vivox.ParticipantRemovedFromChannel += OnParticipantRemoved;
                    vivox.LoggedOut += OnLoggedOut;
                    vivoxReady = true;
                }
                // Our Vivox name is the Unity Authentication player id; RoleManager maps it to a role.
                // (Netcode client ids are not assigned yet at this point on a joining client.)
                if (!vivox.IsLoggedIn)
                    await vivox.LoginAsync(new LoginOptions { DisplayName = Unity.Services.Authentication.AuthenticationService.Instance.PlayerId });
                // Mic stays muted except while push-to-talk is held.
                vivox.MuteInputDevice();
                await vivox.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            }
            catch (Exception e)
            {
                // Mic denied, no network, or service down: the game stays playable with quick-chat.
                Debug.LogWarning($"[Voice] Connect failed: {e}");
                SetState(VoiceState.Failed);
            }
        }

        void OnChannelJoined(string name)
        {
            if (name != channelName) return;
            SetState(VoiceState.Ready);
            ApplyRouting();
            ApplyDeviceSettings();
        }

        void OnLoggedOut()
        {
            ClearTaps();
            SetState(VoiceState.Failed);
        }

        void OnParticipantAdded(VivoxParticipant p)
        {
            if (p.ChannelName != channelName) return;
            if (p.IsSelf) { self = p; return; }
            participants[p.PlayerId] = p;
            p.ParticipantSpeechDetected += () => OnSpeech(p);
            ApplyRouting();
        }

        void OnParticipantRemoved(VivoxParticipant p)
        {
            participants.Remove(p.PlayerId);
            RemoveTap(p.PlayerId);
            speaking[RoleOf(p)] = false;
            Changed?.Invoke();
        }

        void OnSpeech(VivoxParticipant p)
        {
            var role = RoleOf(p);
            speaking[role] = p.SpeechDetected && (!InMatch || taps.ContainsKey(p.PlayerId));
            Changed?.Invoke();
        }

        /// <summary>True while a round is on screen; in the lobby everyone talks freely.</summary>
        public static bool InMatch => GameplayBootstrap.Active;

        /// <summary>Own microphone level 0..1 (Vivox audio energy), for the HUD meter.</summary>
        public float LocalMicLevel => self != null ? (float)self.AudioEnergy : 0f;

        /// <summary>Called when the scene or roles change.</summary>
        public void Refresh() => ApplyRouting();

        /// <summary>
        /// Lobby: everyone hears everyone, clear. In a round: hear only relay neighbours,
        /// each through its own radio filter.
        /// </summary>
        void ApplyRouting()
        {
            if (State != VoiceState.Ready || RoleManager.Instance == null) return;
            var me = RoleManager.Instance.LocalRole;
            if (!InMatch)
            {
                foreach (var p in participants.Values)
                {
                    RemoveTap(p.PlayerId);
                    p.UnmutePlayerLocally();
                }
                Changed?.Invoke();
                return;
            }
            foreach (var p in participants.Values)
            {
                var role = RoleOf(p);
                bool hear = RelayChain.CanHear(me, role);
                if (hear && !taps.ContainsKey(p.PlayerId))
                {
                    p.UnmutePlayerLocally();
                    var go = p.CreateVivoxParticipantTap("Radio_" + role);
                    go.transform.SetParent(transform, false);
                    var radio = go.AddComponent<RadioDistortion>();
                    radio.distanceKm = RelayChain.DistanceKm(me, role);
                    taps[p.PlayerId] = radio;
                }
                else if (!hear)
                {
                    RemoveTap(p.PlayerId);
                    p.MutePlayerLocally();
                }
                else
                {
                    taps[p.PlayerId].distanceKm = RelayChain.DistanceKm(me, role);
                }
            }
            Changed?.Invoke();
        }

        static PlayerRole RoleOf(VivoxParticipant p)
        {
            var rm = RoleManager.Instance;
            if (rm == null) return PlayerRole.None;
            var role = rm.RoleOfPlayerId(p.DisplayName);
            return role != PlayerRole.None ? role : rm.RoleOfPlayerId(p.PlayerId);
        }

        void SetTransmitting(bool on)
        {
            IsTransmitting = on;
            if (vivoxReady && VivoxService.Instance.IsLoggedIn)
            {
                if (on) VivoxService.Instance.UnmuteInputDevice();
                else VivoxService.Instance.MuteInputDevice();
            }
            var clip = on ? pttOnClip : pttOffClip;
            if (clip != null && GameSettings.PushToTalk) sfx.PlayOneShot(clip, GameSettings.SfxVolume);
            Changed?.Invoke();
        }

        void RemoveTap(string playerId)
        {
            if (!taps.TryGetValue(playerId, out var radio)) return;
            if (radio != null) Destroy(radio.gameObject);
            taps.Remove(playerId);
        }

        void ClearTaps()
        {
            foreach (var r in taps.Values) if (r != null) Destroy(r.gameObject);
            taps.Clear();
            participants.Clear();
            self = null;
            speaking.Clear();
        }

        void UnhookVivox()
        {
            if (!vivoxReady) return;
            var vivox = VivoxService.Instance;
            vivox.ChannelJoined -= OnChannelJoined;
            vivox.ParticipantAddedToChannel -= OnParticipantAdded;
            vivox.ParticipantRemovedFromChannel -= OnParticipantRemoved;
            vivox.LoggedOut -= OnLoggedOut;
            vivoxReady = false;
        }

        void SetState(VoiceState s)
        {
            State = s;
            Changed?.Invoke();
        }

        static bool TextInputFocused()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.currentSelectedGameObject != null
                && es.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;
        }
    }
}
