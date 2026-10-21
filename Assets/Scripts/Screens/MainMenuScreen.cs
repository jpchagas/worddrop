using System.Collections;
using TMPro;
using UnityEngine;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Gameplay;

namespace WordDrop.Screens
{
    /// <summary>
    /// Main menu buttons and the settings panel. Features that don't exist yet
    /// show a short "Em breve!" message.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_Text musicToggleLabel;
        [SerializeField] private CanvasGroup toast;
        [SerializeField] private float toastSeconds = 1.2f;

        private Coroutine toastRoutine;

        private void Start()
        {
            settingsPanel.SetActive(false);
            toast.alpha = 0f;
            RefreshMusicLabel();
        }

        public void Play()
        {
            LevelSelection.CurrentIndex = 0;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        public void DailyChallenge() => ShowComingSoon();

        public void HowToPlay() => ShowComingSoon();

        public void OpenSettings() => settingsPanel.SetActive(true);

        public void CloseSettings() => settingsPanel.SetActive(false);

        public void ToggleMusic()
        {
            MusicPlayer.ToggleMusic();
            RefreshMusicLabel();
        }

        private void RefreshMusicLabel()
        {
            musicToggleLabel.text = PlayerData.MusicOn ? Strings.MusicOn : Strings.MusicOff;
        }

        private void ShowComingSoon()
        {
            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
            }

            toastRoutine = StartCoroutine(ShowToast());
        }

        private IEnumerator ShowToast()
        {
            toast.alpha = 1f;
            yield return new WaitForSecondsRealtime(toastSeconds);

            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                toast.alpha = 1f - t / 0.3f;
                yield return null;
            }

            toast.alpha = 0f;
            toastRoutine = null;
        }
    }
}
