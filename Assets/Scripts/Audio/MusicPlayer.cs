using System.Collections;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Audio
{
    /// <summary>
    /// Background music that keeps playing across scene changes.
    /// Put one in any scene that should start the music; if a player already
    /// exists (e.g. coming back to the menu), the new copy removes itself.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private float volume = 0.6f;
        [SerializeField] private float fadeInSeconds = 1.5f;

        public static MusicPlayer Instance { get; private set; }

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void Start()
        {
            if (Instance == this && PlayerData.MusicOn)
            {
                StartCoroutine(FadeIn());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Flips the music setting, whether or not a player exists in this scene.</summary>
        public static void ToggleMusic()
        {
            bool on = !PlayerData.MusicOn;
            if (Instance != null)
            {
                Instance.SetMusicOn(on);
            }
            else
            {
                PlayerData.MusicOn = on;
            }
        }

        public void SetMusicOn(bool on)
        {
            PlayerData.MusicOn = on;
            StopAllCoroutines();

            if (on)
            {
                StartCoroutine(FadeIn());
            }
            else
            {
                source.Stop();
            }
        }

        private IEnumerator FadeIn()
        {
            source.volume = 0f;
            if (!source.isPlaying)
            {
                source.Play();
            }

            for (float t = 0f; t < fadeInSeconds; t += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(0f, volume, t / fadeInSeconds);
                yield return null;
            }

            source.volume = volume;
        }
    }
}
