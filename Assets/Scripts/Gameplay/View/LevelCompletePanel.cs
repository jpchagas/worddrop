using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Core;

namespace WordDrop.Gameplay.View
{
    /// <summary>"Nível concluído!" panel: stars, score, best score and the words found.</summary>
    public class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private Image[] stars;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private TMP_Text wordCountLabel;
        [SerializeField] private TMP_Text wordListLabel;
        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private GameObject playAgainButton;
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.15f);

        public void Show(int starCount, int score, int best, IReadOnlyList<string> words, bool hasNextLevel)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].color = i < starCount ? starOn : starOff;
            }

            scoreLabel.text = Strings.FormatNumber(score);
            bestLabel.text = Strings.FormatNumber(best);
            wordCountLabel.text = words.Count.ToString();
            wordListLabel.text = string.Join(" · ", words);
            nextLevelButton.SetActive(hasNextLevel);
            playAgainButton.SetActive(!hasNextLevel);
            gameObject.SetActive(true);
        }
    }
}
