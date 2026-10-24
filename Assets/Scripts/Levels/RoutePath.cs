using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// The centre line of one road from the village to the hill. Index matches OrderData.routeKeys.
    /// Used to place flood zones on wrong roads and by the debug autopilot.
    /// </summary>
    public sealed class RoutePath : MonoBehaviour
    {
        public int optionIndex;
        public Vector3[] points;

        void OnDrawGizmos()
        {
            if (points == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 1; i < points.Length; i++) Gizmos.DrawLine(points[i - 1], points[i]);
        }
    }
}
