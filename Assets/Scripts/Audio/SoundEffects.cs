using System.Collections.Generic;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Audio
{
    public enum Sfx { Rotate, Lock, Word, Row, Combo, Bonus, Win, Lose }

    /// <summary>
    /// Short game sounds, synthesized at startup from simple tones so the game
    /// needs no sound files. Respects the "Sons" setting. Replace with real
    /// audio clips later by changing <see cref="Build"/>.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundEffects : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [SerializeField] private float volume = 0.5f;

        private static Dictionary<Sfx, AudioClip> clips;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            clips ??= Build();
        }

        public void Play(Sfx effect)
        {
            if (PlayerData.SoundOn && clips.TryGetValue(effect, out AudioClip clip))
            {
                source.PlayOneShot(clip, volume);
            }
        }

        private static Dictionary<Sfx, AudioClip> Build()
        {
            return new Dictionary<Sfx, AudioClip>
            {
                [Sfx.Rotate] = Notes("rotate", 0.035f, 0.25f, 1200f),
                [Sfx.Lock] = Notes("lock", 0.07f, 0.6f, 140f),
                [Sfx.Word] = Notes("word", 0.09f, 0.45f, 660f, 880f),
                [Sfx.Row] = Notes("row", 0.07f, 0.45f, 392f, 523f, 659f),
                [Sfx.Combo] = Notes("combo", 0.08f, 0.45f, 784f, 988f, 1175f, 1568f),
                [Sfx.Bonus] = Notes("bonus", 0.06f, 0.4f, 1046f, 1318f),
                [Sfx.Win] = Notes("win", 0.13f, 0.5f, 523f, 659f, 784f, 1046f),
                [Sfx.Lose] = Notes("lose", 0.18f, 0.5f, 392f, 330f, 262f),
            };
        }

        /// <summary>A clip that plays the given frequencies one after another, each with a quick fade.</summary>
        private static AudioClip Notes(string name, float noteSeconds, float gain, params float[] frequencies)
        {
            int noteSamples = Mathf.RoundToInt(noteSeconds * SampleRate);
            var data = new float[noteSamples * frequencies.Length];
            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float attack = Mathf.Clamp01(i / (0.004f * SampleRate));
                    float decay = Mathf.Exp(-5f * i / noteSamples);
                    // Sine plus a little of the octave for a brighter, "chime" sound.
                    float wave = Mathf.Sin(2f * Mathf.PI * frequencies[n] * t)
                                 + 0.3f * Mathf.Sin(4f * Mathf.PI * frequencies[n] * t);
                    data[n * noteSamples + i] = wave * attack * decay * gain * 0.75f;
                }
            }

            AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>Short vibration on mobile, if the player has it on (GDD 6.1).</summary>
    public static class Haptics
    {
        public static void Pulse()
        {
            if (!PlayerData.VibrationOn)
            {
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
