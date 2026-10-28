using System.Collections;
using TMPro;
using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>Text that pops up, rises and fades, like "+90" over cleared tiles. Clones a hidden template label.</summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text template;
        [Tooltip("How far it rises at scale 1.")]
        [SerializeField] private float rise = 140f;
        [SerializeField] private float seconds = 0.9f;

        /// <summary>Shows <paramref name="text"/> at <paramref name="position"/> (this object's local space).</summary>
        /// <param name="scale">Size (and rise) relative to the template.</param>
        public void Show(string text, Vector2 position, Color color, float scale = 1f)
        {
            TMP_Text label = Instantiate(template, transform);
            label.gameObject.SetActive(true);
            label.text = text;
            label.color = color;
            label.rectTransform.anchoredPosition = position;
            StartCoroutine(Animate(label, position, scale));
        }

        private IEnumerator Animate(TMP_Text label, Vector2 start, float scale)
        {
            Color color = label.color;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float progress = t / seconds;
                float pop = PopIn.EaseOutBack(Mathf.Clamp01(progress / 0.25f));
                label.rectTransform.localScale = Vector3.one * (scale * Mathf.Lerp(0.4f, 1f, pop));
                label.rectTransform.anchoredPosition = start + Vector2.up * (rise * scale * (1f - (1f - progress) * (1f - progress)));
                color.a = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, progress));
                label.color = color;
                yield return null;
            }

            Destroy(label.gameObject);
        }
    }
}
