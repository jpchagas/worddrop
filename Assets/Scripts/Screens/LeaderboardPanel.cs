using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using WordDrop.Core;
using WordDrop.Gameplay;
using WordDrop.Progress;

namespace WordDrop.Screens
{
    /// <summary>
    /// Records (GDD 7.1, item 10): Endless top 10, today's Daily Challenge and
    /// best campaign scores, read from <see cref="Leaderboards.Service"/>
    /// (local for now; an online service can replace it later).
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyLabel;

        public void Open()
        {
            gameObject.SetActive(true);
            bodyLabel.text = BuildText();
        }

        public void Close() => gameObject.SetActive(false);

        private static string BuildText()
        {
            var text = new StringBuilder();
            AppendBoard(text, Strings.LeaderboardEndless, Leaderboards.Service.Top(Leaderboards.Endless, 10));
            AppendBoard(text, Strings.LeaderboardDaily, Leaderboards.Service.Top(Leaderboards.Daily(DailyChallenge.Today), 5));

            text.Append("<color=#FFC83A><b>").Append(Strings.LeaderboardCampaign).Append("</b></color>\n");
            for (int n = 1; n <= GameSession.CampaignLevelCount; n++)
            {
                int best = PlayerData.BestScore(n);
                int stars = PlayerData.Stars(n);
                text.Append(string.Format(Strings.LevelFormat, n)).Append(": ")
                    .Append(best > 0 ? Strings.FormatNumber(best) : "—")
                    .Append(stars > 0 ? $"   <size=80%>{stars}/3</size>" : "").Append('\n');
            }

            text.Append("\n<size=80%><i>").Append(Strings.LeaderboardOnlineSoon).Append("</i></size>");
            return text.ToString();
        }

        private static void AppendBoard(StringBuilder text, string title, IReadOnlyList<LeaderboardEntry> entries)
        {
            text.Append("<color=#FFC83A><b>").Append(title).Append("</b></color>\n");
            if (entries.Count == 0)
            {
                text.Append(Strings.LeaderboardEmpty).Append('\n');
            }

            for (int i = 0; i < entries.Count; i++)
            {
                text.Append(i + 1).Append(". ").Append(Strings.FormatNumber(entries[i].score))
                    .Append("   <size=80%>").Append(entries[i].date).Append("</size>\n");
            }

            text.Append('\n');
        }
    }
}
