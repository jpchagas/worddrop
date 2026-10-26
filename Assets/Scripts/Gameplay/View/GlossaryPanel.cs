using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>Lists the words of a run with their meanings (GDD 7.1, item 13).</summary>
    public class GlossaryPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private ScrollRect scrollRect;

        public void Show(string text)
        {
            bodyLabel.text = text;
            gameObject.SetActive(true);
            scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
