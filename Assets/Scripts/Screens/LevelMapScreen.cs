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
    /// path, with the stars earned. Unlocked levels can be replayed; the next
    /// level to play is highlighted and scrolled into view.
    /// </summary>
    public class LevelMapScreen : MonoBehaviour
    {
        [Tooltip("One per level, in order.")]
        [SerializeField] private Button[] nodes;
        [Tooltip("Three per level, in order.")]
        [SerializeField] private Image[] stars;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Color completedColor = new Color32(0x3D, 0xB5, 0x4A, 0xFF);
        [SerializeField] private Color currentColor = new Color32(0xF3, 0x9A, 0x1E, 0xFF);
        [SerializeField] private Color lockedColor = new Color32(0x24, 0x33, 0x52, 0xFF);
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.15f);

        private RectTransform currentNode;
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
                for (int s = 0; s < 3; s++)
                {
                    stars[i * 3 + s].color = s < earned ? starOn : starOff;
                }

                if (isCurrent)
                {
                    currentNode = (RectTransform)nodes[i].transform;
                }
            }

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
            currentNode.localScale = Vector3.one * (1f + Mathf.Sin(pulse) * 0.06f);
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
