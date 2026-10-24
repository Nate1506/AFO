namespace SayItAgain
{
    /// <summary>The three relay roles. Order matches the relay chain HQ -> Van -> Tent -> Village.</summary>
    public enum PlayerRole : byte
    {
        None = 0,
        Van = 1,
        Tent = 2,
        Village = 3,
    }

    public static class PlayerRoleExtensions
    {
        public static string NameKey(this PlayerRole role) => role switch
        {
            PlayerRole.Van => "role_van",
            PlayerRole.Tent => "role_tent",
            PlayerRole.Village => "role_village",
            _ => "role_none",
        };
    }
}
