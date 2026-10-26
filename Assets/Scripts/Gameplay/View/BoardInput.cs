using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// Turns touches (or mouse) on the play area into game gestures, all
    /// reachable with one thumb (GDD 7.3):
    /// tap = rotate, drag sideways = move, drag down = soft drop,
    /// quick flick down = hard drop.
    /// Needs a Graphic (e.g. a transparent Image) on the same object to receive touches.
    /// </summary>
    public class BoardInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private BoardView boardView;
        [Tooltip("A press shorter than this (seconds) that barely moved counts as a tap.")]
        [SerializeField] private float tapMaxSeconds = 0.3f;
        [Tooltip("A downward drag of this many cells, released within flickMaxSeconds, is a hard drop.")]
        [SerializeField] private float flickCells = 2f;
        [SerializeField] private float flickMaxSeconds = 0.25f;

        public event Action Tapped;
        public event Action DragStarted;
        /// <summary>Whole columns moved since the drag started (negative = left).</summary>
        public event Action<int> DraggedColumns;
        /// <summary>Whole rows dragged downward since the drag started.</summary>
        public event Action<int> DraggedRows;
        public event Action Flicked;

        private Vector2 pressPosition;
        private float pressTime;
        private bool moved;
        private bool vertical;
        private bool axisChosen;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressPosition = LocalPoint(eventData);
            pressTime = Time.unscaledTime;
            moved = false;
            vertical = false;
            axisChosen = false;
            DragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 delta = LocalPoint(eventData) - pressPosition;
            float cell = boardView.CellSize;
            if (delta.magnitude > cell * 0.3f)
            {
                moved = true;
            }

            // Lock the drag to one axis once it is clear, so a sideways drag
            // doesn't drop the piece and a downward drag doesn't shift it.
            if (!axisChosen && delta.magnitude > cell * 0.5f)
            {
                axisChosen = true;
                vertical = -delta.y > Mathf.Abs(delta.x);
            }

            if (!axisChosen)
            {
                return;
            }

            if (vertical)
            {
                DraggedRows?.Invoke(Mathf.Max(0, Mathf.FloorToInt(-delta.y / cell)));
            }
            else
            {
                DraggedColumns?.Invoke(Mathf.RoundToInt(delta.x / cell));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float seconds = Time.unscaledTime - pressTime;
            Vector2 delta = LocalPoint(eventData) - pressPosition;

            if (!moved && seconds <= tapMaxSeconds)
            {
                Tapped?.Invoke();
            }
            else if (vertical && -delta.y > boardView.CellSize * flickCells && seconds <= flickMaxSeconds)
            {
                Flicked?.Invoke();
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
