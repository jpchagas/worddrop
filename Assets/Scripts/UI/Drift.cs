using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>Slowly bobs and sways a decoration (e.g. the floating letter tiles on the menus) around where it was placed.</summary>
    public class Drift : MonoBehaviour
    {
        [SerializeField] private Vector2 amplitude = new Vector2(8f, 18f);
        [SerializeField] private float period = 4f;
        [Tooltip("Sway in degrees either side of the starting rotation.")]
        [SerializeField] private float sway = 6f;

        private RectTransform rect;
        private Vector2 rest;
        private float restAngle;
        private float phase;

        private void Start()
        {
            rect = (RectTransform)transform;
            rest = rect.anchoredPosition;
            restAngle = rect.localEulerAngles.z;
            // Each decoration starts at a different point of its cycle.
            phase = (rest.x * 0.013f + rest.y * 0.007f) % (Mathf.PI * 2f);
        }

        private void Update()
        {
            float t = Time.unscaledTime * Mathf.PI * 2f / period + phase;
            rect.anchoredPosition = rest + new Vector2(Mathf.Sin(t * 0.7f) * amplitude.x, Mathf.Sin(t) * amplitude.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, restAngle + Mathf.Sin(t * 0.8f + 1f) * sway);
        }
    }
}
