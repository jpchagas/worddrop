using TMPro;
using UnityEngine;

namespace WordDrop.Gameplay.View
{
    /// <summary>Shown before a level starts: title, objective and a short tutorial tip (GDD 4.1, 7.1).</summary>
    public class LevelBriefingPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private TMP_Text tipLabel;

        public void Show(string title, string objective, string tip)
        {
            titleLabel.text = title;
            objectiveLabel.text = objective;
            tipLabel.text = tip;
            tipLabel.gameObject.SetActive(!string.IsNullOrEmpty(tip));
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
