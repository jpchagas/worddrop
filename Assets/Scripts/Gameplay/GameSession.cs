namespace WordDrop.Gameplay
{
    public enum GameMode { Campaign, Endless, Daily }

    /// <summary>What the Game scene should start: set by the menu before loading it.</summary>
    public static class GameSession
    {
        /// <summary>Levels in the campaign (the GameController's level list must match).</summary>
        public const int CampaignLevelCount = 10;

        public static GameMode Mode { get; set; } = GameMode.Campaign;

        /// <summary>Campaign level index, 0-based.</summary>
        public static int LevelIndex { get; set; }
    }
}
