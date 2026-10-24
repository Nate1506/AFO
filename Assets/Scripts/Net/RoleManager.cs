using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace SayItAgain.Net
{
    /// <summary>
    /// Server-owned role table: which client plays Van, Tent and Village.
    /// Spawned by the host, survives scene loads. Empty roles are played by bots.
    /// </summary>
    public sealed class RoleManager : NetworkBehaviour
    {
        public const ulong NoOwner = ulong.MaxValue;

        public static RoleManager Instance { get; private set; }
        public static event Action RolesChanged;

        readonly NetworkVariable<ulong> vanOwner = new NetworkVariable<ulong>(NoOwner);
        readonly NetworkVariable<ulong> tentOwner = new NetworkVariable<ulong>(NoOwner);
        readonly NetworkVariable<ulong> villageOwner = new NetworkVariable<ulong>(NoOwner);
        /// <summary>Level picked by the host in the lobby (index into LevelCatalog).</summary>
        public readonly NetworkVariable<int> levelIndex = new NetworkVariable<int>();

        /// <summary>
        /// Unity Authentication player id of each connected client. Vivox participants are matched to
        /// roles through this id (a client's Netcode id is not known yet when it logs in to Vivox).
        /// </summary>
        public struct ClientAuth : INetworkSerializable, IEquatable<ClientAuth>
        {
            public ulong clientId;
            public FixedString64Bytes playerId;

            public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
            {
                s.SerializeValue(ref clientId);
                s.SerializeValue(ref playerId);
            }

            public bool Equals(ClientAuth o) => clientId == o.clientId && playerId.Equals(o.playerId);
        }

        readonly NetworkList<ClientAuth> auth = new NetworkList<ClientAuth>();

        public override void OnNetworkSpawn()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            vanOwner.OnValueChanged += OnOwnerChanged;
            tentOwner.OnValueChanged += OnOwnerChanged;
            villageOwner.OnValueChanged += OnOwnerChanged;
            levelIndex.OnValueChanged += (_, __) => RolesChanged?.Invoke();
            auth.OnListChanged += _ => RolesChanged?.Invoke();

            // Solo play never signs in; online players register their id for voice routing.
            if (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized
                && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn)
                RegisterAuthRpc(Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);

            if (IsServer)
            {
                levelIndex.Value = SessionService.PendingLevel;
                var preferred = Variable(SessionService.PreferredHostRole);
                if (preferred != null) preferred.Value = NetworkManager.LocalClientId;
                SessionService.PreferredHostRole = PlayerRole.None;
                NetworkManager.OnClientConnectedCallback += AssignFirstFree;
                NetworkManager.OnClientDisconnectCallback += ForgetClient;
                foreach (var id in NetworkManager.ConnectedClientsIds) AssignFirstFree(id);
            }
            RolesChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            vanOwner.OnValueChanged -= OnOwnerChanged;
            tentOwner.OnValueChanged -= OnOwnerChanged;
            villageOwner.OnValueChanged -= OnOwnerChanged;
            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= AssignFirstFree;
                NetworkManager.OnClientDisconnectCallback -= ForgetClient;
            }
            if (Instance == this) Instance = null;
            RolesChanged?.Invoke();
        }

        public ulong OwnerOf(PlayerRole role) => Variable(role)?.Value ?? NoOwner;

        public bool IsBot(PlayerRole role) => OwnerOf(role) == NoOwner;

        public PlayerRole RoleOf(ulong clientId)
        {
            if (vanOwner.Value == clientId) return PlayerRole.Van;
            if (tentOwner.Value == clientId) return PlayerRole.Tent;
            if (villageOwner.Value == clientId) return PlayerRole.Village;
            return PlayerRole.None;
        }

        public PlayerRole LocalRole => RoleOf(NetworkManager.LocalClientId);

        /// <summary>Role of the client signed in with this Unity Authentication player id.</summary>
        public PlayerRole RoleOfPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return PlayerRole.None;
            foreach (var a in auth)
                if (a.playerId.ToString() == playerId) return RoleOf(a.clientId);
            return PlayerRole.None;
        }

        [Rpc(SendTo.Server)]
        void RegisterAuthRpc(FixedString64Bytes playerId, RpcParams p = default)
        {
            ulong sender = p.Receive.SenderClientId;
            for (int i = auth.Count - 1; i >= 0; i--)
                if (auth[i].clientId == sender) auth.RemoveAt(i);
            auth.Add(new ClientAuth { clientId = sender, playerId = playerId });
        }

        /// <summary>Lobby "take role" button. Swaps only into a free (bot) role.</summary>
        [Rpc(SendTo.Server)]
        public void RequestRoleRpc(PlayerRole role, RpcParams rpcParams = default)
        {
            var target = Variable(role);
            if (target == null || target.Value != NoOwner) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            Release(sender);
            target.Value = sender;
        }

        void AssignFirstFree(ulong clientId)
        {
            if (RoleOf(clientId) != PlayerRole.None) return;
            foreach (PlayerRole role in new[] { PlayerRole.Van, PlayerRole.Tent, PlayerRole.Village })
            {
                var v = Variable(role);
                if (v.Value != NoOwner) continue;
                v.Value = clientId;
                return;
            }
            // Sessions cap the room at 3, so this only happens if that limit is bypassed.
            NetworkManager.DisconnectClient(clientId, "room_full");
        }

        void Release(ulong clientId)
        {
            var v = Variable(RoleOf(clientId));
            if (v != null) v.Value = NoOwner;
        }

        void ForgetClient(ulong clientId)
        {
            Release(clientId);
            for (int i = auth.Count - 1; i >= 0; i--)
                if (auth[i].clientId == clientId) auth.RemoveAt(i);
        }

        NetworkVariable<ulong> Variable(PlayerRole role) => role switch
        {
            PlayerRole.Van => vanOwner,
            PlayerRole.Tent => tentOwner,
            PlayerRole.Village => villageOwner,
            _ => null,
        };

        void OnOwnerChanged(ulong _, ulong __) => RolesChanged?.Invoke();
    }
}
