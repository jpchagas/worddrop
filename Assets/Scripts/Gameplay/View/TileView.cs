using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// One letter tile. It glides toward its target position, so the board only
    /// has to say where a tile should be, and movement is animated for free.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        // The five tile colors from screens_draft.png.
        private static readonly Color[] Palette =
        {
            new Color32(0x8E, 0x4D, 0xDB, 0xFF), // purple
            new Color32(0x3D, 0xB5, 0x4A, 0xFF), // green
            new Color32(0xF3, 0x9A, 0x1E, 0xFF), // orange
            new Color32(0x2F, 0x80, 0xED, 0xFF), // blue
            new Color32(0xE5, 0x48, 0x4D, 0xFF), // red
        };

        private static readonly Color HighlightColor = new Color32(0x4C, 0xD9, 0x5B, 0xFF);

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text letterLabel;
        [SerializeField] private float followSharpness = 18f;

        private RectTransform rectTransform;
        private Vector2 target;
        private float targetScale = 1f;

        public void Setup(char letter, float size, Vector2 position)
        {
            rectTransform = (RectTransform)transform;
            letterLabel.text = letter.ToString();
            background.color = Palette[(letter - 'A' + Palette.Length * 10) % Palette.Length];
            SetSize(size);
            Snap(position);
            transform.localScale = Vector3.one * 0.3f; // Pops in.
        }

        public void SetSize(float cellSize)
        {
            rectTransform.sizeDelta = Vector2.one * (cellSize * 0.92f);
            letterLabel.fontSize = cellSize * 0.58f;
            SetCornerRadius(background, cellSize * 0.2f);
        }

        /// <summary>Keeps a sliced rounded sprite's corners proportional to the tile, at any board size.</summary>
        public static void SetCornerRadius(Image image, float radius)
        {
            image.pixelsPerUnitMultiplier = image.sprite.border.x / Mathf.Max(radius, 1f);
        }

        public void MoveTo(Vector2 position) => target = position;

        public void Snap(Vector2 position)
        {
            target = position;
            rectTransform.anchoredPosition = position;
        }

        public void Highlight()
        {
            background.color = HighlightColor;
            StartCoroutine(Pulse());
        }

        /// <summary>Shrinks the tile away and destroys it.</summary>
        public void Vanish()
        {
            StopAllCoroutines();
            targetScale = 0f;
            Destroy(gameObject, 0.3f);
        }

        private void Update()
        {
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, target, blend);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * targetScale, blend);
        }

        private IEnumerator Pulse()
        {
            targetScale = 1.18f;
            yield return new WaitForSeconds(0.18f);
            targetScale = 1.05f;
        }
    }
}
