using UnityEngine;

namespace SayItAgain
{
    /// <summary>The top of the high hill. Villagers inside are safe.</summary>
    public sealed class SafeZone : MonoBehaviour
    {
        public static SafeZone Instance { get; private set; }
        public float radius = 22f;
        public float minHeight = 3f;

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        public bool Contains(Vector3 p)
        {
            var d = p - transform.position;
            d.y = 0f;
            return d.magnitude <= radius && p.y >= minHeight;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
