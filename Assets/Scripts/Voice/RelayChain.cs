namespace SayItAgain.Voice
{
    /// <summary>
    /// Who can reach whom by radio. Each place is too far from the next-but-one,
    /// so the message must hop: Van <-> Tent <-> Village.
    /// </summary>
    public static class RelayChain
    {
        public static bool CanHear(PlayerRole listener, PlayerRole speaker)
        {
            if (listener == PlayerRole.None || speaker == PlayerRole.None || listener == speaker) return false;
            return listener == PlayerRole.Tent || speaker == PlayerRole.Tent;
        }

        /// <summary>Link length in km, used for static and volume loss.</summary>
        public static float DistanceKm(PlayerRole a, PlayerRole b)
        {
            bool vanTent = (a == PlayerRole.Van && b == PlayerRole.Tent) || (a == PlayerRole.Tent && b == PlayerRole.Van);
            return vanTent ? 4.5f : 3.2f;
        }
    }
}
