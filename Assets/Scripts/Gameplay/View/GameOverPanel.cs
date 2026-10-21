using TMPro;
using UnityEngine;

namespace WordDrop.Gameplay.View
{
    /// <summary>"Fim de jogo" panel with the reason the level was lost.</summary>
    public class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text reasonLabel;

        public void Show(string reason)
        {
            reasonLabel.text = reason;
            gameObject.SetActive(true);
        }
    }
}
