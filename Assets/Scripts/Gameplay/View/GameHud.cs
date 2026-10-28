using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// Top bar (GDD 7.3): mode/level, score, objective with a progress bar,
    /// stars, a counter (moves left or Endless speed) and the pause button.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private GameObject progressTrack;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private GameObject starsRoot;
        [SerializeField] private Image[] stars;
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.18f);

        private float shownProgress;
        private float targetProgress;
        private float shownScore;
        private int targetScore;
        private int earnedStars;

        public void SetTitle(string title) => titleLabel.text = title;

        /// <summary>The score counts up to a higher value with a punch; a lower one (a restart) shows at once.</summary>
        public void SetScore(int score)
        {
            if (score > targetScore)
            {
                Punch(scoreLabel.transform, 1.3f);
            }
            else if (score < targetScore)
            {
                shownScore = score;
                scoreLabel.text = Strings.FormatNumber(score);
            }

            targetScore = score;
        }

        public void SetCounter(string text) => counterLabel.text = text;

        /// <summary>Objective line and its 0–1 progress; a negative progress hides the bar.</summary>
        public void SetObjective(string line, float progress)
        {
            objectiveLabel.text = line;
            progressTrack.SetActive(progress >= 0f);
            targetProgress = Mathf.Clamp01(progress);
        }

        /// <summary>Stars earned so far; hidden when <paramref name="visible"/> is false (Endless).</summary>
        public void SetStars(int earned, bool visible)
        {
            starsRoot.SetActive(visible);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? starOn : starOff;
                if (i >= earnedStars && i < earned)
                {
                    Punch(stars[i].transform, 1.8f);
                }
            }

            earnedStars = earned;
        }

        private static void Punch(Transform target, float scale) => target.localScale = Vector3.one * scale;

        private void Update()
        {
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, Time.unscaledDeltaTime);
            progressFill.anchorMax = new Vector2(shownProgress, 1f);

            float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            if (Mathf.RoundToInt(shownScore) != targetScore)
            {
                // Eases in, then always moves at least a little so it lands on the exact value.
                shownScore = Mathf.MoveTowards(Mathf.Lerp(shownScore, targetScore, blend), targetScore, 60f * Time.unscaledDeltaTime);
                scoreLabel.text = Strings.FormatNumber(Mathf.RoundToInt(shownScore));
            }

            scoreLabel.transform.localScale = Vector3.Lerp(scoreLabel.transform.localScale, Vector3.one, blend);
            foreach (Image star in stars)
            {
                star.transform.localScale = Vector3.Lerp(star.transform.localScale, Vector3.one, blend);
            }
        }
    }
}
