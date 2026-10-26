using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Progress;

namespace WordDrop.Gameplay.View
{
    /// <summary>The bottom bar with the Bomb, Shuffle and Star boosters and how many of each are left.</summary>
    public class BoosterBar : MonoBehaviour
    {
        [Tooltip("In BoosterType order: Bomb, Shuffle, Star.")]
        [SerializeField] private Button[] buttons;
        [SerializeField] private TMP_Text[] countLabels;
        [SerializeField] private CanvasGroup group;

        /// <summary>Shows the current stock; buttons only work when <paramref name="usable"/> and the booster is in stock.</summary>
        public void Refresh(bool usable)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int count = Boosters.Count((BoosterType)i);
                countLabels[i].text = count.ToString();
                buttons[i].interactable = usable && count > 0;
            }
        }

        /// <summary>Hides the bar where boosters are not allowed (the Daily Challenge).</summary>
        public void SetAvailable(bool available)
        {
            group.alpha = available ? 1f : 0f;
            group.interactable = available;
            group.blocksRaycasts = available;
        }
    }
}
