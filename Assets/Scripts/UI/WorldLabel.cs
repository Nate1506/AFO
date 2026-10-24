using TMPro;
using UnityEngine;

namespace SayItAgain.UI
{
    /// <summary>
    /// Text floating in the world that always faces the camera: signposts and villager speech bubbles.
    /// </summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        public string key;
        public float maxDistance = 60f;
        TextMeshPro text;
        float hideAt = -1f;

        public static WorldLabel Create(Transform parent, Vector3 localPos, string key, float size, Color color)
        {
            var go = new GameObject("Label", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var label = go.AddComponent<WorldLabel>();
            label.key = key;
            var tmp = go.GetComponent<TextMeshPro>();
            tmp.font = UiKit.Font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(12, 3);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        void Awake() => text = GetComponent<TextMeshPro>();

        void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.LanguageChanged -= Refresh;

        void Refresh()
        {
            if (!string.IsNullOrEmpty(key)) text.text = Loc.Get(key);
        }

        /// <summary>Shows raw text for a few seconds (speech bubble).</summary>
        public void Say(string line, float seconds)
        {
            key = null;
            text.text = line;
            text.enabled = true;
            hideAt = Time.time + seconds;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var d = transform.position - cam.transform.position;
            text.enabled = d.magnitude < maxDistance && (hideAt < 0f || Time.time < hideAt);
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
