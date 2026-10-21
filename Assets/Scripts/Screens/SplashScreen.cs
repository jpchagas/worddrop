using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordDrop.Core;

namespace WordDrop.Screens
{
    /// <summary>
    /// Studio splash: fades the Cruze logo in and out, then moves on to the
    /// consent screen (first launch only) or straight to the main menu.
    /// </summary>
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup logoGroup;
        [SerializeField] private float fadeInSeconds = 0.8f;
        [SerializeField] private float holdSeconds = 1.6f;
        [SerializeField] private float fadeOutSeconds = 0.6f;

        private IEnumerator Start()
        {
            logoGroup.alpha = 0f;
            yield return Fade(0f, 1f, fadeInSeconds);
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Fade(1f, 0f, fadeOutSeconds);

            if (PlayerData.HasAcceptedLegal)
            {
                SceneLoader.LoadWithLoadingScreen(SceneNames.MainMenu);
            }
            else
            {
                SceneManager.LoadScene(SceneNames.Consent);
            }
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                logoGroup.alpha = Mathf.SmoothStep(from, to, t / seconds);
                yield return null;
            }

            logoGroup.alpha = to;
        }
    }
}
