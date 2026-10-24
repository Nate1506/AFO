using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace SayItAgain.Net
{
    /// <summary>
    /// Creates and joins 3-player rooms by code (Multiplayer Services session over Relay),
    /// or starts an offline solo host for bot play. Lives on the NetworkManager object.
    /// </summary>
    public sealed class SessionService : MonoBehaviour
    {
        public const int MaxPlayers = 3;

        public static SessionService Instance { get; private set; }

        [Tooltip("Spawned by the host once the network starts. Must be in the NetworkManager prefab list.")]
        public NetworkObject roleManagerPrefab;

        public ISession Session { get; private set; }
        public bool IsSolo { get; private set; }
        public bool IsBusy { get; private set; }
        public bool InRoom => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public string RoomCode => Session?.Code ?? string.Empty;

        /// <summary>Localization key describing the latest connection state.</summary>
        public string StatusKey { get; private set; } = string.Empty;
        public event Action StatusChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            var nm = NetworkManager.Singleton;
            nm.OnServerStarted += OnServerStarted;
            nm.OnClientDisconnectCallback += OnClientDisconnect;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnServerStarted -= OnServerStarted;
            nm.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        public async Task<bool> CreateRoomAsync()
        {
            if (IsBusy || InRoom) return false;
            IsBusy = true;
            try
            {
                if (!await EnsureSignedInAsync()) return false;
                SetStatus("status_creating");
                var options = new SessionOptions { MaxPlayers = MaxPlayers, IsPrivate = true }.WithRelayNetwork();
                Session = await MultiplayerService.Instance.CreateSessionAsync(options);
                HookSession();
                SetStatus("status_in_room");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Create failed: {e}");
                SetStatus(IsNetworkError(e) ? "status_offline" : "status_create_failed");
                return false;
            }
            finally { IsBusy = false; }
        }

        public async Task<bool> JoinRoomAsync(string code)
        {
            if (IsBusy || InRoom) return false;
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length == 0)
            {
                SetStatus("status_code_empty");
                return false;
            }

            IsBusy = true;
            try
            {
                if (!await EnsureSignedInAsync()) return false;
                SetStatus("status_joining");
                Session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
                HookSession();
                SetStatus("status_in_room");
                return true;
            }
            catch (SessionException e) when (e.Message.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetStatus("status_room_full");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Join failed: {e}");
                SetStatus(IsNetworkError(e) ? "status_offline" : "status_join_failed");
                return false;
            }
            finally { IsBusy = false; }
        }

        /// <summary>Offline host on loopback. Works with no internet; missing roles are played by bots.</summary>
        public bool StartSolo()
        {
            if (IsBusy || InRoom) return false;
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            IsSolo = NetworkManager.Singleton.StartHost();
            SetStatus(IsSolo ? "status_in_room" : "status_create_failed");
            return IsSolo;
        }

        public async Task LeaveAsync()
        {
            var session = Session;
            UnhookSession();
            try
            {
                if (session != null) await session.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Leave failed: {e.Message}");
            }
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
            IsSolo = false;
            SetStatus("status_left");
        }

        async Task<bool> EnsureSignedInAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    SetStatus("status_signing_in");
                    await UnityServices.InitializeAsync();
                }
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    SetStatus("status_signing_in");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Sign-in failed: {e.Message}");
                SetStatus("status_offline");
                return false;
            }
        }

        void HookSession()
        {
            Session.RemovedFromSession += OnRemovedFromSession;
            Session.Deleted += OnRemovedFromSession;
        }

        void UnhookSession()
        {
            if (Session == null) return;
            Session.RemovedFromSession -= OnRemovedFromSession;
            Session.Deleted -= OnRemovedFromSession;
            Session = null;
        }

        void OnRemovedFromSession()
        {
            UnhookSession();
            SetStatus("status_host_left");
        }

        void OnServerStarted()
        {
            if (roleManagerPrefab == null)
            {
                Debug.LogError("[Session] RoleManager prefab not assigned");
                return;
            }
            Instantiate(roleManagerPrefab).Spawn(destroyWithScene: false);
        }

        void OnClientDisconnect(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            // A client sees its own id when the host goes away or the connection drops.
            if (!nm.IsServer && clientId == nm.LocalClientId)
            {
                UnhookSession();
                SetStatus("status_host_left");
            }
        }

        static bool IsNetworkError(Exception e)
        {
            for (var x = e; x != null; x = x.InnerException)
            {
                if (x is RequestFailedException rf && rf.ErrorCode == CommonErrorCodes.TransportError) return true;
                if (x is System.Net.Http.HttpRequestException || x is System.Net.Sockets.SocketException) return true;
            }
            return Application.internetReachability == NetworkReachability.NotReachable;
        }

        void SetStatus(string key)
        {
            StatusKey = key;
            StatusChanged?.Invoke();
        }
    }
}
