using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SayItAgain
{
    public enum Language { Thai = 0, English = 1 }

    /// <summary>Tiny localization table loaded from Resources/Localization/strings.csv (key,en,th).</summary>
    public static class Loc
    {
        const string PrefKey = "set_lang";
        static Dictionary<string, string[]> table;
        static Language current = (Language)PlayerPrefs.GetInt(PrefKey, (int)Language.Thai);

        public static event Action LanguageChanged;

        public static Language Current
        {
            get => current;
            set
            {
                if (current == value) return;
                current = value;
                PlayerPrefs.SetInt(PrefKey, (int)value);
                LanguageChanged?.Invoke();
            }
        }

        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            EnsureLoaded();
            if (!table.TryGetValue(key, out var row)) return key;
            var text = current == Language.Thai ? row[1] : row[0];
            return string.IsNullOrEmpty(text) ? row[0] : text;
        }

        public static string Format(string key, params object[] args) => string.Format(Get(key), args);

        static void EnsureLoaded()
        {
            if (table != null) return;
            table = new Dictionary<string, string[]>();
            var asset = Resources.Load<TextAsset>("Localization/strings");
            if (asset == null)
            {
                Debug.LogError("[Loc] Resources/Localization/strings.csv missing");
                return;
            }

            var lines = asset.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                var fields = ParseCsvLine(lines[i].TrimEnd('\r'));
                if (fields.Count < 2 || string.IsNullOrEmpty(fields[0])) continue;
                table[fields[0]] = new[] { fields[1], fields.Count > 2 ? fields[2] : string.Empty };
            }
        }

        static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else if (c == '"') quoted = false;
                    else sb.Append(c);
                }
                else if (c == '"') quoted = true;
                else if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            result.Add(sb.ToString());
            return result;
        }
    }
}
