using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordDrop.Progress
{
    /// <summary>Lifetime statistics (GDD 7.1, item 12), saved as JSON in PlayerPrefs.</summary>
    [Serializable]
    public class StatsData
    {
        public int wordsFormed;
        public int rowsCleared;
        public int longestWord;
        public string longestWordText = "";
        public int bestChain;
        public int piecesPlaced;
        public int accentedWords;
        public int dailyCompleted;
        public int boostersUsed;
        /// <summary>Every different word the player has ever formed.</summary>
        public List<string> discovered = new List<string>();
    }

    public static class PlayerStats
    {
        private const string Key = "player_stats";

        private static StatsData data;

        public static StatsData Data => data ??= Load();

        public static void RecordWord(string word)
        {
            StatsData d = Data;
            d.wordsFormed++;
            if (word.Length > d.longestWord)
            {
                d.longestWord = word.Length;
                d.longestWordText = word;
            }

            if (HasAccent(word))
            {
                d.accentedWords++;
            }

            if (!d.discovered.Contains(word))
            {
                d.discovered.Add(word);
            }
        }

        public static void RecordPlacement(int rows, int chains)
        {
            Data.piecesPlaced++;
            Data.rowsCleared += rows;
            Data.bestChain = Mathf.Max(Data.bestChain, chains);
        }

        public static void Save() => PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));

        /// <summary>Forgets the cached copy so the next read comes from PlayerPrefs (tests use it).</summary>
        public static void Reload() => data = null;

        /// <summary>True for words with a letter beyond plain A–Z (accents or Ç).</summary>
        public static bool HasAccent(string word)
        {
            foreach (char c in word)
            {
                if (c < 'A' || c > 'Z')
                {
                    return true;
                }
            }

            return false;
        }

        private static StatsData Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            return json.Length > 0 ? JsonUtility.FromJson<StatsData>(json) ?? new StatsData() : new StatsData();
        }
    }
}
