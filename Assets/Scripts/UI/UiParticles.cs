using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.UI
{
    /// <summary>
    /// Lightweight uGUI particles: bursts of tinted chunks and soft sparks that
    /// fly out, fall and fade. Images are pooled, so no particle system or
    /// extra camera is needed on the canvas.
    /// </summary>
    public class UiParticles : MonoBehaviour
    {
        [SerializeField] private Sprite chunkSprite;
        [SerializeField] private Sprite sparkSprite;
        [Tooltip("Downward pull, in canvas units per second², at scale 1.")]
        [SerializeField] private float gravity = 2400f;

        private struct Particle
        {
            public Image Image;
            public Vector2 Velocity;
            public Color Color;
            public float Age;
            public float Life;
            public float Spin;
            public float Scale;
        }

        private readonly List<Particle> live = new List<Particle>();
        private readonly Stack<Image> pool = new Stack<Image>();

        /// <summary>Bursts from <paramref name="position"/> (this object's local space).</summary>
        /// <param name="scale">Size and speed of the burst; 1 suits a 70-unit board cell.</param>
        public void Burst(Vector2 position, Color color, float scale, int chunks = 6, int sparks = 3)
        {
            for (int i = 0; i < chunks; i++)
            {
                Spawn(position, chunkSprite, color, scale, Random.Range(20f, 34f), Random.Range(350f, 900f), Random.Range(0.5f, 0.8f));
            }

            Color sparkColor = Color.Lerp(color, Color.white, 0.6f);
            for (int i = 0; i < sparks; i++)
            {
                Spawn(position, sparkSprite, sparkColor, scale, Random.Range(70f, 110f), Random.Range(120f, 420f), Random.Range(0.3f, 0.5f));
            }
        }

        private void Spawn(Vector2 position, Sprite sprite, Color color, float scale, float size, float speed, float life)
        {
            Image image = pool.Count > 0 ? pool.Pop() : CreateImage();
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.color = color;

            RectTransform rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.one * (size * scale);
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            rect.localScale = Vector3.one;

            float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad; // Mostly upwards.
            live.Add(new Particle
            {
                Image = image,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (speed * scale),
                Color = color,
                Life = life,
                Spin = Random.Range(-540f, 540f),
                Scale = scale,
            });
        }

        private Image CreateImage()
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = live.Count - 1; i >= 0; i--)
            {
                Particle p = live[i];
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    p.Image.gameObject.SetActive(false);
                    pool.Push(p.Image);
                    live.RemoveAt(i);
                    continue;
                }

                p.Velocity.y -= gravity * p.Scale * dt;
                float t = p.Age / p.Life;
                RectTransform rect = p.Image.rectTransform;
                rect.anchoredPosition += p.Velocity * dt;
                rect.Rotate(0f, 0f, p.Spin * dt);
                rect.localScale = Vector3.one * (1f - 0.5f * t * t);
                Color color = p.Color;
                color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, t));
                p.Image.color = color;
                live[i] = p;
            }
        }
    }
}
