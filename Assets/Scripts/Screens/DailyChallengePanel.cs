using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;
using WordDrop.Gameplay;
using WordDrop.Progress;
using WordDrop.UI;

namespace WordDrop.Screens
{
    /// <summary>The Daily Challenge screen in the menu: today's date, best result, play and claim the reward.</summary>
    public class DailyChallengePanel : MonoBehaviour
    {
        [SerializeField] private LevelDefinition dailyRules;
        [SerializeField] private TMP_Text dateLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private GameObject doneBadge;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Toast toast;

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        public void Play()
        {
            GameSession.Mode = GameMode.Daily;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        public void Claim()
        {
            if (DailyChallenge.ClaimReward(DailyChallenge.Today))
            {
                toast.Show(Strings.DailyRewardToast);
            }

            Refresh();
        }

        private void Refresh()
        {
            var today = DailyChallenge.Today;
            int target = WordTarget();
            dateLabel.text = Strings.FormatDate(today);
            statusLabel.text = DailyChallenge.Played(today)
                ? $"{string.Format(Strings.DailyWordsFormat, DailyChallenge.BestWords(today), target)}   ·   " +
                  string.Format(Strings.DailyTimeFormat, Strings.FormatTime(DailyChallenge.BestSeconds(today)))
                : Strings.DailyNotPlayed;

            bool completed = DailyChallenge.Completed(today);
            bool claimed = DailyChallenge.Claimed(today);
            doneBadge.SetActive(completed);
            claimButton.interactable = completed && !claimed;
            claimLabel.text = claimed ? Strings.RewardClaimed : Strings.ClaimReward;
        }

        private int WordTarget()
        {
            foreach (LevelObjective objective in dailyRules.Objectives)
            {
                if (objective.type == ObjectiveType.Words)
                {
                    return objective.target;
                }
            }

            return 0;
        }
    }
}
