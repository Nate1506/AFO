using Unity.Netcode;

namespace SayItAgain.Net
{
    public enum RadioKind : byte
    {
        /// <summary>A relayed order (bot speech or a card sent to a bot).</summary>
        Order,
        QuickChat,
        Rumor,
        HqReply,
        FakeHq,
    }

    public enum QuickPhrase : byte { SayAgain, Copy, ReadBack, Correct, Wrong, Wait }

    public enum RumorType : byte { RouteFlooded, RouteDry, GatherWrong, AvoidSafe }

    /// <summary>
    /// Language-neutral radio line. Each client builds the text in its own language,
    /// then drops words with the same seed so everyone sees the same gaps.
    /// </summary>
    public struct RadioMessage : INetworkSerializeByMemcpy
    {
        public RadioKind kind;
        public PlayerRole from;
        public RelayCard card;
        public QuickPhrase phrase;
        public RumorType rumor;
        public OrderSlot slot;
        public sbyte option;
        public int seed;
        public float drop;
    }
}
