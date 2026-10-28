using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>Shakes this object around its resting position, fading out; a stronger shake overrides a weaker one.</summary>
    public class Shaker : MonoBehaviour
    {
        [SerializeField] private float frequency = 28f;

        private Vector3 rest;
        private float strength;
        private float duration;
        private float remaining;
        private float seed;

        /// <param name="amount">Largest offset, in canvas units.</param>
        public void Shake(float amount, float seconds = 0.3f)
        {
            float current = remaining > 0f ? strength * (remaining / duration) : 0f;
            if (amount < current)
            {
                return;
            }

            if (remaining <= 0f)
            {
                rest = transform.localPosition;
            }

            strength = amount;
            duration = remaining = seconds;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                return;
            }

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                transform.localPosition = rest;
                return;
            }

            float fade = remaining / duration;
            float t = (duration - remaining) * frequency + seed;
            var offset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f);
            transform.localPosition = rest + offset * (2f * strength * fade * fade);
        }

        private void OnDisable()
        {
            if (remaining > 0f)
            {
                transform.localPosition = rest;
                remaining = 0f;
            }
        }
    }
}
