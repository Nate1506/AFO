using UnityEngine;

namespace SayItAgain.UI
{
    /// <summary>One opening shot: a Cinemachine camera that drifts while it is live.</summary>
    public sealed class CutsceneShot : MonoBehaviour
    {
        public string captionKey;
        public float seconds = 4f;
        public Vector3 drift = new Vector3(0, 0, 6);
        [Tooltip("Lightning strike this many seconds into the shot; negative = none.")]
        public float strikeAt = -1f;
    }
}
