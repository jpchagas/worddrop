using TMPro;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Gameplay.View
{
    /// <summary>"Fim de jogo" panel: reason, final score and personal best (GDD 7.1).</summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text reasonLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private GameObject newRecordLabel;

        public void Show(string reason, int score, int best, bool newRecord)
        {
            reasonLabel.text = reason;
            scoreLabel.text = Strings.FormatNumber(score);
            bestLabel.text = Strings.FormatNumber(best);
            newRecordLabel.SetActive(newRecord);
            gameObject.SetActive(true);
        }
    }
}
