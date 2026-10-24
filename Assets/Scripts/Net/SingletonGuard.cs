using Unity.Netcode;
using UnityEngine;

namespace SayItAgain.Net
{
    /// <summary>
    /// The lobby scene carries the NetworkManager. When players return to the lobby, the running one
    /// (kept across scenes) wins and the scene copy removes itself before NetworkManager wakes up.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SingletonGuard : MonoBehaviour
    {
        void Awake()
        {
            var running = NetworkManager.Singleton;
            if (running != null && running.gameObject != gameObject) DestroyImmediate(gameObject);
        }
    }
}
