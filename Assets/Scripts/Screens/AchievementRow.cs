using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Screens
{
    /// <summary>One line of the achievements list.</summary>
    public class AchievementRow : MonoBehaviour
    {
        [SerializeField] internal Image background;
        [SerializeField] internal TMP_Text titleLabel;
        [SerializeField] internal TMP_Text descriptionLabel;
        [SerializeField] internal RectTransform progressFill;
        [SerializeField] internal TMP_Text progressLabel;
        [SerializeField] internal TMP_Text rewardLabel;
    }
}
