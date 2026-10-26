using System;
using UnityEngine;

namespace WordDrop.Progress
{
    /// <summary>
    /// The Daily Challenge (GDD 4.3): one puzzle per calendar day, the same for
    /// every player. The day's date is the random seed, the rules come from
    /// Daily.asset and the dictionary version is saved with the result, so a
    /// day can be replayed exactly. Boosters are off for fairness. Completing it
    /// lets the player claim a reward once per day.
    /// </summary>
    public static class DailyChallenge
    {
        public const int RewardPerBooster = 1;

        public static DateTime Today => DateTime.Now.Date;

        /// <summary>Same number for everyone on the same date, e.g. 20261022.</summary>
        public static int Seed(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;

        public static string DateKey(DateTime date) => date.ToString("yyyyMMdd");

        public static bool Played(DateTime date) => PlayerPrefs.HasKey(Key(date, "words"));
        public static int BestWords(DateTime date) => PlayerPrefs.GetInt(Key(date, "words"), 0);
        public static int BestSeconds(DateTime date) => PlayerPrefs.GetInt(Key(date, "seconds"), 0);
        public static bool Completed(DateTime date) => PlayerPrefs.GetInt(Key(date, "completed"), 0) == 1;
        public static bool Claimed(DateTime date) => PlayerPrefs.GetInt(Key(date, "claimed"), 0) == 1;

        /// <summary>Keeps the best result of the day (completed beats not completed, then more words, then less time).</summary>
        public static void RecordRun(DateTime date, int words, int seconds, bool completed, string contentVersion)
        {
            bool wasCompleted = Completed(date);
            bool better = !Played(date)
                          || (completed && !wasCompleted)
                          || (completed == wasCompleted && words > BestWords(date))
                          || (completed && wasCompleted && words == BestWords(date) && seconds < BestSeconds(date));
            if (better)
            {
                PlayerPrefs.SetInt(Key(date, "words"), words);
                PlayerPrefs.SetInt(Key(date, "seconds"), seconds);
                PlayerPrefs.SetString(Key(date, "content"), contentVersion);
            }

            if (completed && !wasCompleted)
            {
                PlayerPrefs.SetInt(Key(date, "completed"), 1);
                PlayerStats.Data.dailyCompleted++;
                PlayerStats.Save();
            }

            PlayerPrefs.Save();
        }

        /// <summary>Gives the day's reward (one of each booster). False if not completed or already claimed.</summary>
        public static bool ClaimReward(DateTime date)
        {
            if (!Completed(date) || Claimed(date))
            {
                return false;
            }

            PlayerPrefs.SetInt(Key(date, "claimed"), 1);
            Boosters.AddEach(RewardPerBooster);
            PlayerPrefs.Save();
            return true;
        }

        private static string Key(DateTime date, string field) => $"daily_{DateKey(date)}_{field}";
    }
}
