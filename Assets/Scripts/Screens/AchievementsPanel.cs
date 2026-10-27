using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;
using WordDrop.Progress;

namespace WordDrop.Screens
{
    /// <summary>Achievements and lifetime statistics (GDD 7.1, item 12).</summary>
    public class AchievementsPanel : MonoBehaviour
    {
        [Tooltip("One row per entry of Achievements.All, in the same order.")]
        [SerializeField] private AchievementRow[] rows;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private Color lockedColor = new Color32(0x24, 0x3E, 0x72, 0xFF);
        [SerializeField] private Color unlockedColor = new Color32(0x2F, 0x7D, 0x50, 0xFF);

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void Refresh()
        {
            Achievements.CheckNewlyUnlocked();
            for (int i = 0; i < rows.Length && i < Achievements.All.Length; i++)
            {
                Achievement achievement = Achievements.All[i];
                AchievementRow row = rows[i];
                row.titleLabel.text = achievement.Title;
                row.descriptionLabel.text = achievement.Description;
                row.progressFill.anchorMax = new Vector2((float)achievement.CurrentProgress / achievement.Target, 1f);
                row.progressLabel.text = $"{Strings.FormatNumber(achievement.CurrentProgress)}/{Strings.FormatNumber(achievement.Target)}";
                row.rewardLabel.text = string.Format(Strings.RewardFormat, achievement.RewardAmount, BoosterName(achievement.Reward));
                row.background.color = achievement.IsUnlocked ? unlockedColor : lockedColor;
            }

            countLabel.text = string.Format(Strings.AchievementsCountFormat, Achievements.UnlockedCount(), Achievements.All.Length);
            StatsData stats = PlayerStats.Data;
            statsLabel.text = string.Format(Strings.StatsFormat,
                Strings.FormatNumber(stats.wordsFormed), Strings.FormatNumber(stats.discovered.Count),
                stats.longestWordText.Length > 0 ? stats.longestWordText : "—",
                Strings.FormatNumber(stats.rowsCleared), stats.bestChain, Strings.FormatNumber(stats.piecesPlaced),
                stats.dailyCompleted);
        }

        public static string BoosterName(BoosterType type) =>
            type == BoosterType.Bomb ? Strings.Bomb : type == BoosterType.Shuffle ? Strings.Shuffle : Strings.Star;
    }
}
