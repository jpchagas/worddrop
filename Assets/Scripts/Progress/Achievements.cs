using System;
using System.Collections.Generic;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Progress
{
    /// <summary>One achievement: a goal on a stat, with a booster reward (GDD 7.1, item 12).</summary>
    public class Achievement
    {
        public Achievement(string id, string title, string description, int target, Func<int> progress,
            BoosterType reward, int rewardAmount)
        {
            Id = id;
            Title = title;
            Description = description;
            Target = target;
            Progress = progress;
            Reward = reward;
            RewardAmount = rewardAmount;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public int Target { get; }
        public Func<int> Progress { get; }
        public BoosterType Reward { get; }
        public int RewardAmount { get; }

        public bool IsUnlocked => PlayerPrefs.GetInt("achievement_" + Id, 0) == 1;

        public int CurrentProgress => Mathf.Min(Progress(), Target);
    }

    /// <summary>
    /// The achievement list (pt-BR content). <see cref="CheckNewlyUnlocked"/>
    /// runs after each piece and at the end of a run; it saves new unlocks and
    /// grants their rewards.
    /// </summary>
    public static class Achievements
    {
        public static readonly Achievement[] All =
        {
            new Achievement("first_word", "Primeira palavra", "Forme sua primeira palavra.", 1,
                () => PlayerStats.Data.wordsFormed, BoosterType.Star, 1),
            new Achievement("words_50", "Vocabulário", "Forme 50 palavras.", 50,
                () => PlayerStats.Data.wordsFormed, BoosterType.Shuffle, 2),
            new Achievement("words_250", "Dicionário vivo", "Forme 250 palavras.", 250,
                () => PlayerStats.Data.wordsFormed, BoosterType.Star, 3),
            new Achievement("discover_100", "Explorador", "Descubra 100 palavras diferentes.", 100,
                () => PlayerStats.Data.discovered.Count, BoosterType.Bomb, 2),
            new Achievement("long_6", "Palavra longa", "Forme uma palavra com 6 letras ou mais.", 6,
                () => PlayerStats.Data.longestWord, BoosterType.Shuffle, 1),
            new Achievement("long_8", "Palavra gigante", "Forme uma palavra com 8 letras ou mais.", 8,
                () => PlayerStats.Data.longestWord, BoosterType.Star, 2),
            new Achievement("accents_10", "Com acento", "Forme 10 palavras com acento ou Ç.", 10,
                () => PlayerStats.Data.accentedWords, BoosterType.Shuffle, 1),
            new Achievement("rows_10", "Faxina", "Complete 10 linhas.", 10,
                () => PlayerStats.Data.rowsCleared, BoosterType.Bomb, 1),
            new Achievement("chain_1", "Combo!", "Faça um combo.", 1,
                () => PlayerStats.Data.bestChain, BoosterType.Bomb, 1),
            new Achievement("chain_2", "Reação em cadeia", "Faça dois combos seguidos com uma só peça.", 2,
                () => PlayerStats.Data.bestChain, BoosterType.Star, 2),
            new Achievement("world_1", "Primeiras Palavras", "Complete os 10 níveis do mundo 1.", GameplayLevels,
                CompletedLevels, BoosterType.Bomb, 3),
            new Achievement("stars_5", "Perfeição", "Ganhe 3 estrelas em 5 níveis.", 5,
                ThreeStarLevels, BoosterType.Star, 2),
            new Achievement("endless_5000", "Infinito", "Faça 5.000 pontos no Modo Infinito.", 5000,
                () => PlayerData.EndlessBest, BoosterType.Bomb, 2),
            new Achievement("daily_1", "Desafio aceito", "Complete um Desafio Diário.", 1,
                () => PlayerStats.Data.dailyCompleted, BoosterType.Shuffle, 1),
            new Achievement("daily_7", "Fiel", "Complete 7 Desafios Diários.", 7,
                () => PlayerStats.Data.dailyCompleted, BoosterType.Star, 3),
        };

        private const int GameplayLevels = 10;

        /// <summary>Unlocks achievements whose goal is now met, grants their rewards and returns them.</summary>
        public static List<Achievement> CheckNewlyUnlocked()
        {
            var unlocked = new List<Achievement>();
            foreach (Achievement achievement in All)
            {
                if (achievement.IsUnlocked || achievement.Progress() < achievement.Target)
                {
                    continue;
                }

                PlayerPrefs.SetInt("achievement_" + achievement.Id, 1);
                Boosters.Add(achievement.Reward, achievement.RewardAmount);
                unlocked.Add(achievement);
            }

            PlayerPrefs.Save();
            return unlocked;
        }

        public static int UnlockedCount()
        {
            int count = 0;
            foreach (Achievement achievement in All)
            {
                if (achievement.IsUnlocked)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CompletedLevels()
        {
            int count = 0;
            for (int n = 1; n <= GameplayLevels; n++)
            {
                if (PlayerData.Stars(n) > 0)
                {
                    count++;
                }
            }

            return count;
        }

        private static int ThreeStarLevels()
        {
            int count = 0;
            for (int n = 1; n <= GameplayLevels; n++)
            {
                if (PlayerData.Stars(n) >= 3)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
