using SayItAgain.UI;
using Unity.Netcode;
using UnityEngine;

namespace SayItAgain.Net
{
    /// <summary>
    /// Spawns the local first-person rig at the spawn point for this client's role, the HUD, and
    /// (on the host) the round's MatchManager.
    /// Roles are far apart and never see each other, so rigs are local only (no network transform).
    /// </summary>
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        public GameObject playerRigPrefab;
        public NetworkObject matchManagerPrefab;
        [Tooltip("Used when the scene is played directly in the Editor without a room.")]
        public PlayerRole editorFallbackRole = PlayerRole.Village;

        public static PlayerRole LocalRole { get; private set; }
        /// <summary>A round scene is loaded (voice switches from open lobby chat to relay radio).</summary>
        public static bool Active { get; private set; }
        public static GameObject LocalRig { get; private set; }

        public string lobbyScene = "Lobby";

        void Start()
        {
            // Menu replay of the opening: scenery only, no player.
            if (CutscenePlayer.ReplayOnly) return;

            if (SessionService.Instance != null) SessionService.Instance.RoomExited += BackToLobby;
            Active = true;
            SayItAgain.Voice.VoiceService.Instance?.Refresh();
            var rm = RoleManager.Instance;
            LocalRole = rm != null && rm.LocalRole != PlayerRole.None ? rm.LocalRole : editorFallbackRole;
            SpawnRig(LocalRole);

            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer) Instantiate(matchManagerPrefab).Spawn(destroyWithScene: true);

            var hud = new GameObject("Hud");
            hud.AddComponent<GameHud>();
            hud.AddComponent<CardPanel>();
            hud.AddComponent<ResultPanel>();
            hud.AddComponent<PauseMenu>();
            hud.AddComponent<MapOverlay>();
            hud.AddComponent<TutorialOverlay>();
        }

        void OnDestroy()
        {
            Active = false;
            SayItAgain.Voice.VoiceService.Instance?.Refresh();
            if (SessionService.Instance != null) SessionService.Instance.RoomExited -= BackToLobby;
        }

        /// <summary>The host left or the connection dropped: go back to the menu instead of standing in a dead match.</summary>
        void BackToLobby() => UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene);

        void SpawnRig(PlayerRole role)
        {
            RoleSpawnPoint point = null;
            foreach (var p in FindObjectsByType<RoleSpawnPoint>(FindObjectsSortMode.None))
                if (p.role == role) { point = p; break; }

            if (point == null)
            {
                Debug.LogError($"[Gameplay] No RoleSpawnPoint for {role}");
                return;
            }

            LocalRig = Instantiate(playerRigPrefab, point.transform.position, point.transform.rotation);
            LocalRig.name = $"PlayerRig_{role}";
            var controller = LocalRig.GetComponent<FirstPersonController>();
            controller.mode = point.moveMode;
            controller.anchor = point.transform.position;
            controller.confineRadius = point.confineRadius;
        }
    }
}
