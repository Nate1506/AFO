using UnityEngine;

namespace SayItAgain.Net
{
    /// <summary>
    /// Spawns the local first-person rig at the spawn point for this client's role.
    /// Roles are far apart and never see each other, so rigs are local only (no network transform).
    /// </summary>
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        public GameObject playerRigPrefab;
        [Tooltip("Used when the scene is played directly in the Editor without a room.")]
        public PlayerRole editorFallbackRole = PlayerRole.Village;

        public static PlayerRole LocalRole { get; private set; }
        public static GameObject LocalRig { get; private set; }

        void Start()
        {
            var rm = RoleManager.Instance;
            LocalRole = rm != null && rm.LocalRole != PlayerRole.None ? rm.LocalRole : editorFallbackRole;
            SpawnRig(LocalRole);
        }

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
