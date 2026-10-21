using System.Collections.Generic;
using UnityEngine;

namespace WordDrop.Gameplay.View
{
    /// <summary>The small "Próxima" box in the HUD showing the next piece, centered in the box.</summary>
    public class NextPiecePreview : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private RectTransform tilesRoot;

        private readonly List<TileView> tiles = new List<TileView>();

        /// <summary>Shows the piece as it will spawn, or clears the box when null.</summary>
        public void Show(Piece piece)
        {
            foreach (TileView tile in tiles)
            {
                Destroy(tile.gameObject);
            }

            tiles.Clear();
            if (piece == null)
            {
                return;
            }

            var cells = new Vector2Int[Piece.BlockCount];
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = piece.CellOf(i);
                min = Vector2.Min(min, cells[i]);
                max = Vector2.Max(max, cells[i]);
            }

            Rect rect = tilesRoot.rect;
            // As big as the piece allows (an O gets bigger tiles than an I), up to a cap.
            Vector2 span = max - min + Vector2.one;
            float cellSize = Mathf.Min(rect.width / span.x, rect.height / span.y, rect.width / 2.6f);
            Vector2 center = (min + max) / 2f;

            for (int i = 0; i < cells.Length; i++)
            {
                TileView tile = Instantiate(tilePrefab, tilesRoot);
                tile.Setup(piece.Letters[i], cellSize, ((Vector2)cells[i] - center) * cellSize);
                tiles.Add(tile);
            }
        }
    }
}
