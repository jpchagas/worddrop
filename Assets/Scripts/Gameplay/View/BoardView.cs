using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// Draws the board: the empty cell slots, the placed tiles and the falling
    /// piece. The tile size adapts to the space this RectTransform gets, so the
    /// board fits any phone shape.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private RectTransform slotsRoot;
        [SerializeField] private RectTransform tilesRoot;
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(0.03f, 0.06f, 0.14f, 0.85f);

        private int width;
        private int height;
        private TileView[,] tiles;
        private Image[,] slots;
        private readonly TileView[] pieceTiles = new TileView[Piece.BlockCount];
        private Piece piece;
        private Vector2 origin;

        /// <summary>Width of one cell, in the board's local (canvas) units.</summary>
        public float CellSize { get; private set; }

        public void Init(int boardWidth, int boardHeight)
        {
            width = boardWidth;
            height = boardHeight;
            tiles = new TileView[width, height];
            slots = new Image[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var slot = new GameObject($"Slot {x},{y}", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    slot.transform.SetParent(slotsRoot, false);
                    slot.sprite = slotSprite;
                    slot.type = Image.Type.Sliced;
                    slot.color = slotColor;
                    slot.raycastTarget = false;
                    slots[x, y] = slot;
                }
            }

            Relayout();
        }

        public void ShowPiece(Piece newPiece)
        {
            piece = newPiece;
            for (int i = 0; i < Piece.BlockCount; i++)
            {
                pieceTiles[i] = CreateTile(piece.Letters[i], piece.CellOf(i));
            }
        }

        /// <summary>Call after the piece moved or rotated; each tile glides to its block's new cell.</summary>
        public void UpdatePiece()
        {
            for (int i = 0; i < Piece.BlockCount; i++)
            {
                pieceTiles[i].MoveTo(CellCenter(piece.CellOf(i)));
            }
        }

        /// <summary>The landed piece's tiles become regular board tiles.</summary>
        public void LockPiece()
        {
            for (int i = 0; i < Piece.BlockCount; i++)
            {
                Vector2Int cell = piece.CellOf(i);
                tiles[cell.x, cell.y] = pieceTiles[i];
                pieceTiles[i] = null;
            }

            piece = null;
        }

        public void Highlight(IEnumerable<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                tiles[cell.x, cell.y].Highlight();
            }
        }

        public void Remove(IEnumerable<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                tiles[cell.x, cell.y].Vanish();
                tiles[cell.x, cell.y] = null;
            }
        }

        public void ApplyMoves(List<(Vector2Int from, Vector2Int to)> moves)
        {
            foreach (var (from, to) in moves)
            {
                TileView tile = tiles[from.x, from.y];
                tiles[from.x, from.y] = null;
                tiles[to.x, to.y] = tile;
                tile.MoveTo(CellCenter(to));
            }
        }

        internal void AddTile(Vector2Int cell, char letter) => tiles[cell.x, cell.y] = CreateTile(letter, cell);

        private TileView CreateTile(char letter, Vector2Int cell)
        {
            TileView tile = Instantiate(tilePrefab, tilesRoot);
            tile.name = $"Tile {letter}";
            tile.Setup(letter, CellSize, CellCenter(cell));
            return tile;
        }

        private Vector2 CellCenter(Vector2Int cell) =>
            origin + new Vector2((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize);

        private void OnRectTransformDimensionsChange()
        {
            if (tiles != null)
            {
                Relayout();
            }
        }

        private void Relayout()
        {
            Rect rect = tilesRoot.rect;
            CellSize = Mathf.Min(rect.width / width, rect.height / height);
            // Centered horizontally, sitting on the bottom edge.
            origin = new Vector2(rect.xMin + (rect.width - CellSize * width) / 2f, rect.yMin);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var slotRect = (RectTransform)slots[x, y].transform;
                    slotRect.sizeDelta = Vector2.one * (CellSize * 0.92f);
                    TileView.SetCornerRadius(slots[x, y], CellSize * 0.2f);
                    slotRect.anchoredPosition = CellCenter(new Vector2Int(x, y));

                    if (tiles[x, y] != null)
                    {
                        tiles[x, y].SetSize(CellSize);
                        tiles[x, y].Snap(CellCenter(new Vector2Int(x, y)));
                    }
                }
            }

            if (piece != null)
            {
                for (int i = 0; i < Piece.BlockCount; i++)
                {
                    pieceTiles[i].SetSize(CellSize);
                    pieceTiles[i].Snap(CellCenter(piece.CellOf(i)));
                }
            }
        }
    }
}
