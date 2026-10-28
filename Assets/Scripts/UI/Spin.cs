using UnityEngine;

namespace WordDrop.UI
{
    /// <summary>Keeps a decoration turning, e.g. the sunburst behind earned stars.</summary>
    public class Spin : MonoBehaviour
    {
        [Tooltip("Degrees per second; negative turns clockwise.")]
        [SerializeField] private float speed = -20f;

        private void Update() => transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
    }
}
