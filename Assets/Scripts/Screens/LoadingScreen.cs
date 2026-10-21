using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordDrop.Core;

namespace WordDrop.Screens
{
    /// <summary>
    /// Loads the next scene in the background while filling a progress bar.
    /// Stays up for a minimum time so it never just flashes on fast devices.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private TMP_Text percentLabel;
        [SerializeField] private float minimumSeconds = 1.5f;

        private IEnumerator Start()
        {
            string target = SceneLoader.TakePendingScene(SceneNames.MainMenu);
            SetProgress(0f);

            AsyncOperation load = SceneManager.LoadSceneAsync(target);
            load.allowSceneActivation = false;

            float elapsed = 0f;
            float shown = 0f;
            while (shown < 1f)
            {
                elapsed += Time.unscaledDeltaTime;

                // Unity reports 0.9 when a scene is loaded but not yet activated.
                float loaded = Mathf.Clamp01(load.progress / 0.9f);
                float timed = Mathf.Clamp01(elapsed / minimumSeconds);
                shown = Mathf.MoveTowards(shown, Mathf.Min(loaded, timed), Time.unscaledDeltaTime * 2f);

                SetProgress(shown);
                yield return null;
            }

            load.allowSceneActivation = true;
        }

        private void SetProgress(float value)
        {
            progressFill.anchorMax = new Vector2(value, 1f);
            percentLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }
}
