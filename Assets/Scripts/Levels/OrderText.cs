using System.Text;

namespace SayItAgain
{
    /// <summary>Turns cards into radio sentences, and drops words the way a bad link does.</summary>
    public static class OrderText
    {
        public static string Value(OrderData data, OrderSlot slot, int index)
        {
            var options = data.Options(slot);
            return index >= 0 && index < options.Length ? Loc.Get(options[index]) : "?";
        }

        public static string Sentence(OrderData data, RelayCard card) => Loc.Format("order_sentence",
            Value(data, OrderSlot.Route, card.route),
            Value(data, OrderSlot.Avoid, card.avoid),
            Value(data, OrderSlot.Gather, card.gather),
            Value(data, OrderSlot.Code, card.code));

        /// <summary>
        /// Replaces whole chunks with "..." with the given chance. Chunks split on spaces and
        /// on the Thai/English sentence separators used in the table, so Thai text (no spaces) still breaks up.
        /// </summary>
        public static string Garble(string text, float dropChance, System.Random rng)
        {
            if (dropChance <= 0f) return text;
            var sb = new StringBuilder();
            var chunk = new StringBuilder();
            bool lastDropped = false;
            void Flush(char sep)
            {
                if (chunk.Length > 0)
                {
                    bool drop = rng.NextDouble() < dropChance;
                    if (drop) { if (!lastDropped) sb.Append("..."); }
                    else sb.Append(chunk);
                    lastDropped = drop;
                    chunk.Clear();
                }
                if (sep != '\0') sb.Append(sep);
            }
            foreach (char c in text)
            {
                if (c == ' ' || c == '/' || c == '.' || c == ':' || c == ',') Flush(c);
                else chunk.Append(c);
            }
            Flush('\0');
            return sb.ToString();
        }
    }
}
