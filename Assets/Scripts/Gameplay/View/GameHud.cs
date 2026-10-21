using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;

namespace WordDrop.Gameplay.View
{
    /// <summary>Top bar: score, pieces left, level, progress bar and stars.</summary>
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text piecesLabel;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Image[] stars;
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.18f);

        private float shownProgress;
        private float targetProgress;

        public void SetLevel(int levelNumber) => levelLabel.text = string.Format(Strings.LevelFormat, levelNumber);

        public void SetPiecesLeft(int pieces) => piecesLabel.text = string.Format(Strings.PiecesFormat, pieces);

        public void SetScore(int score, LevelDefinition level)
        {
            scoreLabel.text = Strings.FormatNumber(score);
            targetProgress = Mathf.Clamp01((float)score / level.ThreeStarScore);

            int earned = level.StarsFor(score);
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < earned ? starOn : starOff;
            }
        }

        private void Update()
        {
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, Time.unscaledDeltaTime);
            progressFill.anchorMax = new Vector2(shownProgress, 1f);
        }
    }
}
