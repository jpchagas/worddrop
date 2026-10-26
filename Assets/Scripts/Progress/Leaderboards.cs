using System;
using System.Collections.Generic;
using UnityEngine;

namespace WordDrop.Progress
{
    [Serializable]
    public struct LeaderboardEntry
    {
        public string player;
        public int score;
        /// <summary>yyyy-MM-dd</summary>
        public string date;
    }

    /// <summary>
    /// Where scores go. Today only <see cref="LocalLeaderboardService"/> exists;
    /// an online service (Google Play Games, Firebase...) can implement this
    /// later and be set in <see cref="Leaderboards.Service"/>, once anti-cheat and
    /// the privacy policy are ready (GDD 4.3).
    /// </summary>
    public interface ILeaderboardService
    {
        void Submit(string board, int score);
        IReadOnlyList<LeaderboardEntry> Top(string board, int count);
    }

    public static class Leaderboards
    {
        public const string Endless = "endless";

        public static ILeaderboardService Service { get; set; } = new LocalLeaderboardService();

        public static string Daily(DateTime date) => "daily_" + DailyChallenge.DateKey(date);

        public static string Level(int number) => "level_" + number;
    }

    /// <summary>Keeps the 10 best scores of each board on this device.</summary>
    public class LocalLeaderboardService : ILeaderboardService
    {
        public const int Size = 10;

        [Serializable]
        private class Table
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        }

        public void Submit(string board, int score)
        {
            if (score <= 0)
            {
                return;
            }

            Table table = Load(board);
            table.entries.Add(new LeaderboardEntry
            {
                player = "Você",
                score = score,
                date = DateTime.Now.ToString("yyyy-MM-dd"),
            });
            table.entries.Sort((a, b) => b.score.CompareTo(a.score));
            if (table.entries.Count > Size)
            {
                table.entries.RemoveRange(Size, table.entries.Count - Size);
            }

            PlayerPrefs.SetString(Key(board), JsonUtility.ToJson(table));
            PlayerPrefs.Save();
        }

        public IReadOnlyList<LeaderboardEntry> Top(string board, int count)
        {
            List<LeaderboardEntry> entries = Load(board).entries;
            return entries.GetRange(0, Mathf.Min(count, entries.Count));
        }

        private static Table Load(string board)
        {
            string json = PlayerPrefs.GetString(Key(board), "");
            return json.Length > 0 ? JsonUtility.FromJson<Table>(json) ?? new Table() : new Table();
        }

        private static string Key(string board) => "leaderboard_" + board;
    }
}
