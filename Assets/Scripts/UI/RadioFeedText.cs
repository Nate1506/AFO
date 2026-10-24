using SayItAgain.Net;

namespace SayItAgain.UI
{
    /// <summary>Builds the localized text for a radio line, with the same dropped words on every client.</summary>
    public static class RadioFeedText
    {
        public static string Format(RadioMessage msg)
        {
            var match = MatchManager.Instance;
            if (match == null) return string.Empty;
            var data = match.Orders;
            var rng = new System.Random(msg.seed);
            string body = msg.kind switch
            {
                RadioKind.Order => OrderText.Garble(OrderText.Sentence(data, msg.card), msg.drop, rng),
                RadioKind.QuickChat => OrderText.Garble(Phrase(data, msg), msg.drop, rng),
                RadioKind.Rumor => Loc.Format("feed_rumor", Rumor(msg)),
                RadioKind.HqReply => Loc.Format("feed_hq_reply", Loc.Get(OrderData.SlotNameKey(msg.slot)), OrderText.Value(data, msg.slot, msg.option)),
                RadioKind.FakeHq => Loc.Format("feed_fake_hq", OrderText.Value(data, OrderSlot.Route, msg.card.route), OrderText.Value(data, OrderSlot.Code, msg.card.code)),
                _ => string.Empty,
            };
            string who = msg.kind switch
            {
                RadioKind.Rumor => Loc.Get("feed_from_crowd"),
                RadioKind.HqReply or RadioKind.FakeHq => "HQ",
                _ => Loc.Get(msg.from.NameKey()),
            };
            return $"<b>{who}:</b> {body}";
        }

        static string Phrase(OrderData data, RadioMessage msg) => msg.phrase switch
        {
            QuickPhrase.ReadBack => Loc.Format("qc_readback_line", OrderText.Sentence(data, msg.card)),
            _ => Loc.Get(PhraseKey(msg.phrase)),
        };

        public static string PhraseKey(QuickPhrase p) => "qc_" + p.ToString().ToLowerInvariant();

        public static string Rumor(RadioMessage msg)
        {
            var data = MatchManager.Instance.Orders;
            return msg.rumor switch
            {
                RumorType.RouteFlooded => Loc.Format("rumor_route_flooded", OrderText.Value(data, OrderSlot.Route, msg.option)),
                RumorType.RouteDry => Loc.Format("rumor_route_dry", OrderText.Value(data, OrderSlot.Route, msg.option)),
                RumorType.GatherWrong => Loc.Format("rumor_gather", OrderText.Value(data, OrderSlot.Gather, msg.option)),
                _ => Loc.Format("rumor_avoid_safe", OrderText.Value(data, OrderSlot.Avoid, msg.option)),
            };
        }
    }
}
