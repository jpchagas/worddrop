using TMPro;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Gameplay.View
{
    /// <summary>The banner shown over the board when words or rows are cleared.</summary>
    public class WordFoundOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text wordsLabel;
        [SerializeField] private TMP_Text pointsLabel;
        [SerializeField] private TMP_Text definitionLabel;

        private float targetAlpha;

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        /// <param name="definition">Meaning of the word, shown in small text; null for none.</param>
        public void Show(string title, string body, int points, string definition = null)
        {
            definitionLabel.text = definition ?? "";
            titleLabel.text = title;
            wordsLabel.text = body;
            pointsLabel.text = "+" + Strings.FormatNumber(points);

            targetAlpha = 1f;
            transform.localScale = Vector3.one * 0.7f;
        }

        public void Hide() => targetAlpha = 0f;

        private void Update()
        {
            float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            group.alpha = Mathf.Lerp(group.alpha, targetAlpha, blend);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, blend);
        }
    }
}
