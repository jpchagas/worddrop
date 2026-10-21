using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// Turns touches (or mouse) on the play area into game gestures:
    /// tap = rotate, drag sideways = move columns, swipe down = drop.
    /// Needs a Graphic (e.g. a transparent Image) on the same object to receive touches.
    /// </summary>
    public class BoardInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private BoardView boardView;
        [Tooltip("A press shorter than this (seconds) that barely moved counts as a tap.")]
        [SerializeField] private float tapMaxSeconds = 0.3f;
        [Tooltip("Downward drag, in cells, that triggers a drop.")]
        [SerializeField] private float swipeDownCells = 1.5f;

        public event Action Tapped;
        public event Action DragStarted;
        /// <summary>Whole columns moved since the drag started (negative = left).</summary>
        public event Action<int> DraggedColumns;
        public event Action SwipedDown;

        private Vector2 pressPosition;
        private float pressTime;
        private bool moved;
        private bool dropped;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressPosition = LocalPoint(eventData);
            pressTime = Time.unscaledTime;
            moved = false;
            dropped = false;
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dropped)
            {
                return;
            }

            Vector2 delta = LocalPoint(eventData) - pressPosition;
            float cell = boardView.CellSize;
            if (delta.magnitude > cell * 0.3f)
            {
                moved = true;
            }

            if (-delta.y > cell * swipeDownCells && -delta.y > Mathf.Abs(delta.x) * 1.5f)
            {
                dropped = true;
                SwipedDown?.Invoke();
                return;
            }

            DraggedColumns?.Invoke(Mathf.RoundToInt(delta.x / cell));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!moved && !dropped && Time.unscaledTime - pressTime <= tapMaxSeconds)
            {
                Tapped?.Invoke();
            }
        }

        private Vector2 LocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)boardView.transform, eventData.position, eventData.pressEventCamera, out Vector2 point);
            return point;
        }
    }
}
