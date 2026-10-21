namespace WordDrop.Core
{
    /// <summary>
    /// Scene names, in the order they appear in Build Settings.
    /// Keep in sync with the scenes listed by the editor ProjectSetup.
    /// </summary>
    public static class SceneNames
    {
        public const string Splash = "Splash";
        public const string Consent = "Consent";
        public const string Loading = "Loading";
        public const string MainMenu = "MainMenu";
        public const string Game = "Game";

        public static readonly string[] All = { Splash, Consent, Loading, MainMenu, Game };
    }
}
