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

        [Tooltip("Spring that makes the banner overshoot as it pops in.")]
        [SerializeField] private float stiffness = 320f;
        [SerializeField] private float damping = 16f;

        private float targetAlpha;
        private float scale = 1f;
        private float scaleVelocity;

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
            scale = 0.6f;
            scaleVelocity = 0f;
        }

        public void Hide() => targetAlpha = 0f;

        private void Update()
        {
            // Leaves faster than it arrives, so it is gone before the next piece spawns.
            float sharpness = targetAlpha > 0f ? 14f : 30f;
            float blend = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
            group.alpha = Mathf.Lerp(group.alpha, targetAlpha, blend);

            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            scaleVelocity += ((1f - scale) * stiffness - scaleVelocity * damping) * dt;
            scale += scaleVelocity * dt;
            transform.localScale = Vector3.one * scale;
        }
    }
}
