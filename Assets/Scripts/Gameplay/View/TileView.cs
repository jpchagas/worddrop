using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// One letter tile. It glides toward its target position, so the board only
    /// has to say where a tile should be, and movement is animated for free.
    /// Its color is a hint: tiles of the same bright color spell a word together;
    /// tiles with no hint are a neutral slate. Booster tiles show an icon.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        /// <summary>Share of the tile's height taken by the darker lip at its bottom (from the generated tile sprite).</summary>
        public const float LipFraction = 13f / 160f;

        // The five hint colors, from screens_draft.png; index = hint - 1. These
        // are the gloss shade: the tile sprite darkens the face below it.
        private static readonly Color[] HintColors =
        {
            new Color32(0xB0, 0x6C, 0xFF, 0xFF), // purple
            new Color32(0x55, 0xE0, 0x6A, 0xFF), // green
            new Color32(0xFF, 0xB0, 0x38, 0xFF), // orange
            new Color32(0x4A, 0xA6, 0xFF, 0xFF), // blue
            new Color32(0xFF, 0x62, 0x68, 0xFF), // red
        };

        private static readonly Color NeutralColor = new Color32(0x72, 0x84, 0xAE, 0xFF);
        private static readonly Color HighlightColor = new Color32(0xA6, 0xFF, 0x86, 0xFF);
        private static readonly Color BombColor = new Color32(0x4A, 0x3A, 0x58, 0xFF);
        private static readonly Color WildcardColor = new Color32(0xFF, 0xD8, 0x4A, 0xFF);

        [SerializeField] private Image background;
        [SerializeField] private TMP_Text letterLabel;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite bombSprite;
        [SerializeField] private Sprite starSprite;
        [SerializeField] private float followSharpness = 18f;

        private RectTransform rectTransform;
        private Vector2 target;
        private float targetScale = 1f;

        public static Color ColorForHint(int hint) =>
            hint <= 0 ? NeutralColor : HintColors[(hint - 1) % HintColors.Length];

        public void Setup(char letter, int hint, float size, Vector2 position)
        {
            rectTransform = (RectTransform)transform;

            bool bomb = letter == Piece.Bomb;
            bool wildcard = letter == Piece.Wildcard;
            letterLabel.gameObject.SetActive(!bomb && !wildcard);
            letterLabel.text = letter.ToString();
            icon.gameObject.SetActive(bomb || wildcard);
            icon.sprite = bomb ? bombSprite : starSprite;
            background.color = bomb ? BombColor : wildcard ? WildcardColor : ColorForHint(hint);

            SetSize(size);
            Snap(position);
            transform.localScale = Vector3.one * 0.3f; // Pops in.
        }

        public void SetSize(float cellSize)
        {
            rectTransform.sizeDelta = Vector2.one * (cellSize * 0.94f);
            letterLabel.fontSize = cellSize * 0.56f;
            SetCornerRadius(background, cellSize * 0.2f);
        }

        /// <summary>Keeps a sliced rounded sprite's corners proportional to the tile, at any board size.</summary>
        public static void SetCornerRadius(Image image, float radius)
        {
            if (image.type == Image.Type.Sliced)
            {
                image.pixelsPerUnitMultiplier = image.sprite.border.x / Mathf.Max(radius, 1f);
            }
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
