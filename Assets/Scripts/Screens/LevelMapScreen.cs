using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WordDrop.Core;
using WordDrop.Gameplay;

namespace WordDrop.Screens
{
    /// <summary>
    /// The campaign map (GDD 7.1, item 7): one node per level along a winding
    /// path, with the stars earned. Unlocked levels can be replayed; locked ones
    /// show a lock. The path is lit up to the last unlocked level, and the next
    /// level to play glows under a bobbing pin and is scrolled into view.
    /// </summary>
    public class LevelMapScreen : MonoBehaviour
    {
        [Tooltip("One per level, in order.")]
        [SerializeField] private Button[] nodes;
        [Tooltip("Three per level, in order.")]
        [SerializeField] private Image[] stars;
        [Tooltip("Per level: the number shown when unlocked, and the lock shown otherwise.")]
        [SerializeField] private GameObject[] numberLabels;
        [SerializeField] private GameObject[] lockIcons;
        [Tooltip("The path's dots, and for each the 1-based level it leads to; a dot is lit once that level is unlocked.")]
        [SerializeField] private Image[] pathDots;
        [SerializeField] private int[] pathDotLevels;
        [SerializeField] private RectTransform marker;
        [SerializeField] private Image currentGlow;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Color completedColor = new Color32(0x3D, 0xB5, 0x4A, 0xFF);
        [SerializeField] private Color currentColor = new Color32(0xF3, 0x9A, 0x1E, 0xFF);
        [SerializeField] private Color lockedColor = new Color32(0x3A, 0x4C, 0x7A, 0xFF);
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color pathOn = new Color32(0xFF, 0xE0, 0x8A, 0xFF);
        [SerializeField] private Color pathOff = new Color32(0x2A, 0x3E, 0x6E, 0xFF);

        private RectTransform currentNode;
        private Vector2 markerRest;
        private float pulse;

        private void Start()
        {
            int unlocked = PlayerData.UnlockedLevel;
            for (int i = 0; i < nodes.Length; i++)
            {
                int level = i + 1;
                int earned = PlayerData.Stars(level);
                bool isUnlocked = level <= unlocked;
                bool isCurrent = level == Mathf.Min(unlocked, nodes.Length);

                nodes[i].interactable = isUnlocked;
                nodes[i].targetGraphic.color = isCurrent ? currentColor : earned > 0 ? completedColor : isUnlocked ? currentColor : lockedColor;
                numberLabels[i].SetActive(isUnlocked);
                lockIcons[i].SetActive(!isUnlocked);
                for (int s = 0; s < 3; s++)
                {
                    stars[i * 3 + s].color = s < earned ? starOn : starOff;
                    stars[i * 3 + s].gameObject.SetActive(isUnlocked);
                }

                if (isCurrent)
                {
                    currentNode = (RectTransform)nodes[i].transform;
                }
            }

            for (int i = 0; i < pathDots.Length; i++)
            {
                pathDots[i].color = pathDotLevels[i] <= unlocked ? pathOn : pathOff;
            }

            if (currentNode != null)
            {
                currentGlow.rectTransform.anchoredPosition = currentNode.anchoredPosition;
                markerRest = currentNode.anchoredPosition + new Vector2(0f, currentNode.rect.height * 0.5f + marker.rect.height * 0.5f + 10f);
                marker.anchoredPosition = markerRest;
            }

            currentGlow.gameObject.SetActive(currentNode != null);
            marker.gameObject.SetActive(currentNode != null);

            Canvas.ForceUpdateCanvases();
            if (currentNode != null)
            {
                // The map starts at the bottom (level 1); scroll so the current level is in view.
                RectTransform content = scrollRect.content;
                float y = Mathf.Abs(currentNode.anchoredPosition.y);
                float scrollable = content.rect.height - scrollRect.viewport.rect.height;
                scrollRect.verticalNormalizedPosition = scrollable > 0f
                    ? Mathf.Clamp01(1f - (y - scrollRect.viewport.rect.height / 2f) / scrollable)
                    : 1f;
            }
        }

        private void Update()
        {
            if (currentNode == null)
            {
                return;
            }

            pulse += Time.unscaledDeltaTime * 3f;
            currentNode.localScale = Vector3.one * (1f + Mathf.Sin(pulse) * 0.04f);
            currentGlow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(pulse) * 0.12f);
            // The pin bounces: quick up, slower down.
            marker.anchoredPosition = markerRest + Vector2.up * (Mathf.Abs(Mathf.Sin(pulse * 0.8f)) * 22f);
        }

        /// <summary>Wired to each level node with its 0-based index.</summary>
        public void PlayLevel(int index)
        {
            GameSession.Mode = GameMode.Campaign;
            GameSession.LevelIndex = index;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        public void Back() => SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
