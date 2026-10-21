using UnityEngine;

namespace WordDrop.Core
{
    /// <summary>
    /// Small values saved on the device (PlayerPrefs): legal consent and settings.
    /// </summary>
    public static class PlayerData
    {
        /// <summary>
        /// Bump this when the Terms of Use or Privacy Policy change, so every
        /// player is asked to accept the new version on their next launch.
        /// </summary>
        public const int CurrentLegalVersion = 1;

        private const string LegalVersionKey = "legal_accepted_version";
        private const string MusicOnKey = "settings_music_on";

        public static bool HasAcceptedLegal =>
            PlayerPrefs.GetInt(LegalVersionKey, 0) >= CurrentLegalVersion;

        public static void AcceptLegal()
        {
            PlayerPrefs.SetInt(LegalVersionKey, CurrentLegalVersion);
            PlayerPrefs.Save();
        }

        public static int BestScore(int level) => PlayerPrefs.GetInt($"level_{level}_best", 0);

        public static int Stars(int level) => PlayerPrefs.GetInt($"level_{level}_stars", 0);

        /// <summary>Keeps the best score and the most stars ever reached in a level.</summary>
        public static void RecordLevelResult(int level, int score, int stars)
        {
            PlayerPrefs.SetInt($"level_{level}_best", Mathf.Max(score, BestScore(level)));
            PlayerPrefs.SetInt($"level_{level}_stars", Mathf.Max(stars, Stars(level)));
            PlayerPrefs.Save();
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicOnKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(MusicOnKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
