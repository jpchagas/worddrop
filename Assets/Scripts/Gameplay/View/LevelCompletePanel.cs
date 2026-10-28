using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// "Nível concluído!" panel: stars, objective status, score, best score and
    /// the words found (GDD 7.1). Earned stars slam down one by one after the panel opens.
    /// </summary>
    public class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Image[] stars;
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text bestLabel;
        [SerializeField] private TMP_Text wordCountLabel;
        [SerializeField] private TMP_Text wordListLabel;
        [SerializeField] private GameObject nextLevelButton;
        [SerializeField] private Color starOn = new Color32(0xFF, 0xC8, 0x3A, 0xFF);
        [SerializeField] private Color starOff = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private SoundEffects sounds;
        [SerializeField] private float firstStarDelay = 0.35f;
        [SerializeField] private float starInterval = 0.45f;

        public void Show(string title, int starCount, string objectiveStatus, int score, int best, IReadOnlyList<string> words, bool hasNextLevel)
        {
            titleLabel.text = title;
            foreach (Image star in stars)
            {
                star.color = starOff;
                star.transform.localScale = Vector3.one;
            }

            objectiveLabel.text = objectiveStatus;
            scoreLabel.text = Strings.FormatNumber(score);
            bestLabel.text = Strings.FormatNumber(best);
            wordCountLabel.text = words.Count.ToString();
            wordListLabel.text = string.Join(" · ", words);
            nextLevelButton.SetActive(hasNextLevel);
            gameObject.SetActive(true);
            StartCoroutine(LandStars(starCount));
        }

        private IEnumerator LandStars(int count)
        {
            yield return new WaitForSecondsRealtime(firstStarDelay);
            for (int i = 0; i < count && i < stars.Length; i++)
            {
                stars[i].color = starOn;
                if (sounds != null)
                {
                    sounds.Play(Sfx.Word);
                }

                const float seconds = 0.28f;
                for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
                {
                    stars[i].transform.localScale = Vector3.one * Mathf.LerpUnclamped(2.4f, 1f, PopIn.EaseOutBack(t / seconds));
                    yield return null;
                }

                stars[i].transform.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(starInterval - seconds);
            }
        }
    }
}
