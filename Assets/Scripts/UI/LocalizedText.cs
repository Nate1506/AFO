using TMPro;
using UnityEngine;

namespace SayItAgain.UI
{
    /// <summary>Keeps a TMP label in sync with the current language.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        public string key;
        TMP_Text text;

        void Awake() => text = GetComponent<TMP_Text>();

        void OnEnable()
        {
            Loc.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.LanguageChanged -= Refresh;

        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        void Refresh()
        {
            if (text != null && !string.IsNullOrEmpty(key)) text.text = Loc.Get(key);
        }
    }
}
