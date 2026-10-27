using System.Collections;
using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>
    /// Entrance for a panel each time it is shown: the whole panel fades in
    /// while its card grows from slightly smaller with a small overshoot.
    /// Put it on the object that gets activated.
    /// </summary>
    public class PopIn : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [Tooltip("Scaled from startScale to 1; may be empty for a fade only.")]
        [SerializeField] private RectTransform card;
        [SerializeField] private float startScale = 0.86f;
        [SerializeField] private float seconds = 0.3f;

        private void OnEnable() => StartCoroutine(Play());

        private IEnumerator Play()
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Apply(t / seconds);
                yield return null;
            }

            Apply(1f);
        }

        private void Apply(float progress)
        {
            if (group != null)
            {
                group.alpha = Mathf.Clamp01(progress * 2f);
            }

            if (card != null)
            {
                float s = Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(progress));
                card.localScale = new Vector3(s, s, 1f);
            }
        }

        /// <summary>Goes a little past 1 and settles back, like a spring.</summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
