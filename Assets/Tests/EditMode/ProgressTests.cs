using System;
using NUnit.Framework;
using UnityEngine;
using WordDrop.Progress;

namespace WordDrop.Tests
{
    /// <summary>
    /// Boosters, leaderboards and the Daily Challenge. These use the real
    /// PlayerPrefs, so each test works on throwaway keys (a test board, a date
    /// in 2000) or restores what it changed.
    /// </summary>
    public class ProgressTests
    {
        private static readonly DateTime TestDay = new DateTime(2000, 1, 2);
        private const string TestBoard = "unit_test_board";

        private int[] savedBoosters;
        private string savedStats;

        [SetUp]
        public void Remember()
        {
            savedBoosters = Array.ConvertAll(Boosters.All, Boosters.Count);
            savedStats = PlayerPrefs.GetString("player_stats", "");
        }

        [TearDown]
        public void Restore()
        {
            for (int i = 0; i < Boosters.All.Length; i++)
            {
                Boosters.Add(Boosters.All[i], savedBoosters[i] - Boosters.Count(Boosters.All[i]));
            }

            PlayerPrefs.SetString("player_stats", savedStats);
            PlayerStats.Reload();
            PlayerPrefs.DeleteKey("leaderboard_" + TestBoard);
            foreach (string field in new[] { "words", "seconds", "completed", "claimed", "content" })
            {
                PlayerPrefs.DeleteKey($"daily_{DailyChallenge.DateKey(TestDay)}_{field}");
            }

            PlayerPrefs.Save();
        }

        [Test]
        public void Boosters_CannotGoBelowZero()
        {
            Boosters.Add(BoosterType.Bomb, -Boosters.Count(BoosterType.Bomb));
            Assert.IsFalse(Boosters.TryUse(BoosterType.Bomb));

            Boosters.Add(BoosterType.Bomb, 1);
            Assert.IsTrue(Boosters.TryUse(BoosterType.Bomb));
            Assert.AreEqual(0, Boosters.Count(BoosterType.Bomb));
        }

        [Test]
        public void Leaderboard_KeepsTheTenBestScoresInOrder()
        {
            var service = new LocalLeaderboardService();
            for (int score = 1; score <= 15; score++)
            {
                service.Submit(TestBoard, score * 100);
            }

            var top = service.Top(TestBoard, 20);
            Assert.AreEqual(LocalLeaderboardService.Size, top.Count);
            Assert.AreEqual(1500, top[0].score);
            Assert.AreEqual(600, top[top.Count - 1].score);
        }

        [Test]
        public void Daily_SeedIsTheDate()
        {
            Assert.AreEqual(20261022, DailyChallenge.Seed(new DateTime(2026, 10, 22)));
        }

        [Test]
        public void Daily_KeepsTheBestRunAndRewardsOnlyOnce()
        {
            DailyChallenge.RecordRun(TestDay, words: 5, seconds: 90, completed: false, contentVersion: "t");
            DailyChallenge.RecordRun(TestDay, words: 3, seconds: 60, completed: false, contentVersion: "t");
            Assert.AreEqual(5, DailyChallenge.BestWords(TestDay));
            Assert.IsFalse(DailyChallenge.ClaimReward(TestDay), "Not completed yet.");

            int stars = Boosters.Count(BoosterType.Star);
            DailyChallenge.RecordRun(TestDay, words: 12, seconds: 80, completed: true, contentVersion: "t");
            Assert.IsTrue(DailyChallenge.Completed(TestDay));
            Assert.IsTrue(DailyChallenge.ClaimReward(TestDay));
            Assert.IsFalse(DailyChallenge.ClaimReward(TestDay), "Only once per day.");
            Assert.AreEqual(stars + DailyChallenge.RewardPerBooster, Boosters.Count(BoosterType.Star));
        }

        [Test]
        public void Stats_TrackLongestAndAccentedWords()
        {
            PlayerPrefs.DeleteKey("player_stats");
            PlayerStats.Reload();

            PlayerStats.RecordWord("SOL");
            PlayerStats.RecordWord("MÃE");
            PlayerStats.RecordWord("JANELA");
            PlayerStats.RecordWord("SOL");

            Assert.AreEqual(4, PlayerStats.Data.wordsFormed);
            Assert.AreEqual(3, PlayerStats.Data.discovered.Count);
            Assert.AreEqual("JANELA", PlayerStats.Data.longestWordText);
            Assert.AreEqual(1, PlayerStats.Data.accentedWords);
        }
    }
}
