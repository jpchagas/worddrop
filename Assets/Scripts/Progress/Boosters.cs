using UnityEngine;

namespace WordDrop.Progress
{
    public enum BoosterType { Bomb, Shuffle, Star }

    /// <summary>
    /// Booster stock, saved on the device. Boosters are earned by playing
    /// (3-star levels, the Daily Challenge, achievements), never bought (GDD 3.3).
    /// </summary>
    public static class Boosters
    {
        public const int StartingAmount = 3;

        public static readonly BoosterType[] All = { BoosterType.Bomb, BoosterType.Shuffle, BoosterType.Star };

        public static int Count(BoosterType type) => PlayerPrefs.GetInt(Key(type), StartingAmount);

        public static void Add(BoosterType type, int amount)
        {
            PlayerPrefs.SetInt(Key(type), Mathf.Max(0, Count(type) + amount));
            PlayerPrefs.Save();
        }

        public static void AddEach(int amount)
        {
            foreach (BoosterType type in All)
            {
                Add(type, amount);
            }
        }

        /// <summary>Spends one booster. False when there are none left.</summary>
        public static bool TryUse(BoosterType type)
        {
            if (Count(type) <= 0)
            {
                return false;
            }

            Add(type, -1);
            PlayerStats.Data.boostersUsed++;
            PlayerStats.Save();
            return true;
        }

        private static string Key(BoosterType type) => $"booster_{type.ToString().ToLowerInvariant()}";
    }
}
