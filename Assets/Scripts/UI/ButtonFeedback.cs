using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordDrop.UI
{
    /// <summary>
    /// Squashes a button while it is held and springs it back with a little
    /// overshoot on release, so every tap feels physical. Ignores presses on
    /// buttons that are not interactable.
    /// </summary>
    public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float stiffness = 700f;
        [SerializeField] private float damping = 20f;

        private Selectable selectable;
        private float target = 1f;
        private float scale = 1f;
        private float velocity;

        private void Awake() => selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable == null || selectable.IsInteractable())
            {
                target = pressedScale;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => target = 1f;

        public void OnPointerExit(PointerEventData eventData) => target = 1f;

        private void OnDisable()
        {
            target = scale = 1f;
            velocity = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (Mathf.Approximately(scale, target) && Mathf.Abs(velocity) < 0.001f)
            {
                return;
            }

            // Damped spring, stepped with a capped frame time so a hitch can't make it explode.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            velocity += ((target - scale) * stiffness - velocity * damping) * dt;
            scale += velocity * dt;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
