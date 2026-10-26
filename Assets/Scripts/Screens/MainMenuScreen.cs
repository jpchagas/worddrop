using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using WordDrop.Audio;
using WordDrop.Core;
using WordDrop.Gameplay;

namespace WordDrop.Screens
{
    /// <summary>
    /// Main menu (GDD 7.1): campaign (via the level map), Endless, Daily
    /// Challenge, How to Play, achievements, records and settings.
    /// </summary>
    public class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text playLabel;
        [SerializeField] private TMP_Text endlessBestLabel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private TMP_Text musicToggleLabel;
        [SerializeField] private TMP_Text soundToggleLabel;
        [SerializeField] private TMP_Text vibrationToggleLabel;

        private void Start()
        {
            settingsPanel.SetActive(false);
            howToPlayPanel.SetActive(false);
            int level = Mathf.Min(PlayerData.UnlockedLevel, GameSession.CampaignLevelCount);
            playLabel.text = string.Format(Strings.PlayLevelFormat, level);
            endlessBestLabel.text = PlayerData.EndlessBest > 0
                ? string.Format(Strings.EndlessBestFormat, Strings.FormatNumber(PlayerData.EndlessBest))
                : "";
            RefreshSettingLabels();
        }

        /// <summary>"Jogar" opens the level map, where the next level is highlighted.</summary>
        public void Play() => SceneManager.LoadScene(SceneNames.LevelMap);

        public void PlayEndless()
        {
            GameSession.Mode = GameMode.Endless;
            SceneLoader.LoadWithLoadingScreen(SceneNames.Game);
        }

        public void OpenHowToPlay() => howToPlayPanel.SetActive(true);

        public void CloseHowToPlay() => howToPlayPanel.SetActive(false);

        public void OpenSettings() => settingsPanel.SetActive(true);

        public void CloseSettings() => settingsPanel.SetActive(false);

        public void ToggleMusic()
        {
            MusicPlayer.ToggleMusic();
            RefreshSettingLabels();
        }

        public void ToggleSound()
        {
            PlayerData.SoundOn = !PlayerData.SoundOn;
            RefreshSettingLabels();
        }

        public void ToggleVibration()
        {
            PlayerData.VibrationOn = !PlayerData.VibrationOn;
            if (PlayerData.VibrationOn)
            {
                Haptics.Pulse(); // Lets the player feel what they turned on.
            }

            RefreshSettingLabels();
        }

        private void RefreshSettingLabels()
        {
            musicToggleLabel.text = PlayerData.MusicOn ? Strings.MusicOn : Strings.MusicOff;
            soundToggleLabel.text = PlayerData.SoundOn ? Strings.SoundOn : Strings.SoundOff;
            vibrationToggleLabel.text = PlayerData.VibrationOn ? Strings.VibrationOn : Strings.VibrationOff;
        }
    }
}
