using UnityEngine;

namespace SayItAgain
{
    /// <summary>Where a role's first-person rig spawns and how far it may move.</summary>
    public sealed class RoleSpawnPoint : MonoBehaviour
    {
        public PlayerRole role = PlayerRole.Village;
        public MoveMode moveMode = MoveMode.Free;
        public float confineRadius = 4f;

        void OnDrawGizmos()
        {
            Gizmos.color = role switch
            {
                PlayerRole.Van => Color.yellow,
                PlayerRole.Tent => Color.cyan,
                _ => Color.green,
            };
            Gizmos.DrawWireSphere(transform.position, moveMode == MoveMode.Confined ? confineRadius : 0.5f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);
        }
    }
}
