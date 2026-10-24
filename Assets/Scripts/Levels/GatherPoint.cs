using UnityEngine;

namespace SayItAgain
{
    /// <summary>A place villagers may be waiting (market, school, rice mill). Index matches OrderData.gatherKeys.</summary>
    public sealed class GatherPoint : MonoBehaviour
    {
        public int optionIndex;
        public float radius = 5f;

        public Vector3 RandomSpot(System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = (float)rng.NextDouble() * radius;
            return transform.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
