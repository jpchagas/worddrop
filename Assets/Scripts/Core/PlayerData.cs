using System;
using UnityEngine;

namespace WordDrop.Core
{
    /// <summary>
    /// The local player profile (GDD 6.1), saved on the device with PlayerPrefs:
    /// player id, legal consent, campaign progress, best scores, Endless records,
    /// sound/vibration preferences and the content (dictionary) version.
    /// No account or network needed.
    /// </summary>
    public static class PlayerData
    {
        /// <summary>
        /// Bump this when the Terms of Use or Privacy Policy change, so every
        /// player is asked to accept the new version on their next launch.
        /// </summary>
        public const int CurrentLegalVersion = 1;

        private const string PlayerIdKey = "player_id";
        private const string LegalVersionKey = "legal_accepted_version";
        private const string MusicOnKey = "settings_music_on";
        private const string SoundOnKey = "settings_sound_on";
        private const string VibrationOnKey = "settings_vibration_on";
        private const string UnlockedLevelKey = "campaign_unlocked_level";
        private const string EndlessBestKey = "endless_best";
        private const string EndlessLastKey = "endless_last";
        private const string ContentVersionKey = "content_version";

        /// <summary>A random id created on first use; identifies this local profile.</summary>
        public static string PlayerId
        {
            get
            {
                string id = PlayerPrefs.GetString(PlayerIdKey, "");
                if (id.Length == 0)
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(PlayerIdKey, id);
                    PlayerPrefs.Save();
                }

                return id;
            }
        }

        public static bool HasAcceptedLegal =>
            PlayerPrefs.GetInt(LegalVersionKey, 0) >= CurrentLegalVersion;

        public static void AcceptLegal()
        {
            PlayerPrefs.SetInt(LegalVersionKey, CurrentLegalVersion);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- Campaign

        /// <summary>Highest campaign level the player may play (1-based).</summary>
        public static int UnlockedLevel => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedLevelKey, 1));

        public static void UnlockLevel(int level)
        {
            if (level > UnlockedLevel)
            {
                PlayerPrefs.SetInt(UnlockedLevelKey, level);
                PlayerPrefs.Save();
            }
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

        // ---------------------------------------------------------------- Endless

        public static int EndlessBest => PlayerPrefs.GetInt(EndlessBestKey, 0);
        public static int EndlessLast => PlayerPrefs.GetInt(EndlessLastKey, 0);

        /// <summary>Saves an Endless run. Returns true if it is a new personal best.</summary>
        public static bool RecordEndlessRun(int score)
        {
            bool record = score > EndlessBest;
            PlayerPrefs.SetInt(EndlessLastKey, score);
            if (record)
            {
                PlayerPrefs.SetInt(EndlessBestKey, score);
            }

            PlayerPrefs.Save();
            return record;
        }

        // ---------------------------------------------------------------- Settings

        public static bool MusicOn
        {
            get => GetBool(MusicOnKey);
            set => SetBool(MusicOnKey, value);
        }

        public static bool SoundOn
        {
            get => GetBool(SoundOnKey);
            set => SetBool(SoundOnKey, value);
        }

        public static bool VibrationOn
        {
            get => GetBool(VibrationOnKey);
            set => SetBool(VibrationOnKey, value);
        }

        // ---------------------------------------------------------------- Content

        /// <summary>Dictionary/content version the saved progress was played with (GDD 6.1, 8.6).</summary>
        public static string ContentVersion
        {
            get => PlayerPrefs.GetString(ContentVersionKey, "");
            set
            {
                PlayerPrefs.SetString(ContentVersionKey, value);
                PlayerPrefs.Save();
            }
        }

        private static bool GetBool(string key) => PlayerPrefs.GetInt(key, 1) == 1;

        private static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
