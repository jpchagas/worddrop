using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WordDrop.Gameplay.View
{
    /// <summary>
    /// Draws the board: the empty cell slots, the placed tiles, the falling
    /// piece and its ghost (where it will land). The tile size adapts to the
    /// space this RectTransform gets, so the board fits any phone shape.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private RectTransform slotsRoot;
        [SerializeField] private RectTransform tilesRoot;
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(0.03f, 0.06f, 0.14f, 0.85f);
        [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.16f);

        private int width;
        private int height;
        private TileView[,] tiles;
        private Image[,] slots;
        private readonly List<TileView> pieceTiles = new List<TileView>();
        private readonly List<Image> ghostCells = new List<Image>();
        private readonly List<Vector2Int> ghostPositions = new List<Vector2Int>();
        private RectTransform ghostRoot;
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
                    slots[x, y] = CreateCellImage($"Slot {x},{y}", slotsRoot, slotColor);
                }
            }

            // Ghost cells sit behind the tiles.
            ghostRoot = new GameObject("Ghost", typeof(RectTransform)).GetComponent<RectTransform>();
            ghostRoot.SetParent(tilesRoot, false);
            ghostRoot.SetAsFirstSibling();

            Relayout();
        }

        /// <summary>Creates tiles for letters already on the board (pre-filled levels).</summary>
        public void ShowBoard(Board board)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y] != Board.Empty)
                    {
                        AddTile(new Vector2Int(x, y), board[x, y]);
                    }
                }
            }
        }

        public void AddTile(Vector2Int cell, char letter, int hint = Piece.NoHint) => tiles[cell.x, cell.y] = CreateTile(letter, hint, cell);

        public void ShowPiece(Piece newPiece)
        {
            piece = newPiece;
            pieceTiles.Clear();
            for (int i = 0; i < piece.BlockCount; i++)
            {
                pieceTiles.Add(CreateTile(piece.Letters[i], piece.Hints[i], piece.CellOf(i)));
            }
        }

        /// <summary>Call after the piece moved or rotated; each tile glides to its block's new cell.</summary>
        public void UpdatePiece()
        {
            for (int i = 0; i < pieceTiles.Count; i++)
            {
                pieceTiles[i].MoveTo(CellCenter(piece.CellOf(i)));
            }
        }

        /// <summary>Shows where the piece would land (GDD 1.2), or hides the ghost when null.</summary>
        public void ShowGhost(IReadOnlyList<Vector2Int> cells)
        {
            ghostPositions.Clear();
            if (cells != null)
            {
                ghostPositions.AddRange(cells);
            }

            while (ghostCells.Count < ghostPositions.Count)
            {
                ghostCells.Add(CreateCellImage("GhostCell", ghostRoot, ghostColor));
            }

            for (int i = 0; i < ghostCells.Count; i++)
            {
                bool visible = i < ghostPositions.Count;
                ghostCells[i].gameObject.SetActive(visible);
                if (visible)
                {
                    PlaceCellImage(ghostCells[i], ghostPositions[i]);
                }
            }
        }

        /// <summary>Swaps the falling piece's tiles, e.g. after a booster replaced the piece.</summary>
        public void ReplacePiece(Piece newPiece)
        {
            foreach (TileView tile in pieceTiles)
            {
                tile.Vanish();
            }

            ShowPiece(newPiece);
        }

        /// <summary>The landed piece's tiles become regular board tiles.</summary>
        public void LockPiece()
        {
            for (int i = 0; i < pieceTiles.Count; i++)
            {
                Vector2Int cell = piece.CellOf(i);
                tiles[cell.x, cell.y] = pieceTiles[i];
            }

            pieceTiles.Clear();
            piece = null;
            ShowGhost(null);
        }

        public void Highlight(IEnumerable<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                tiles[cell.x, cell.y]?.Highlight();
            }
        }

        public void Remove(IEnumerable<Vector2Int> cells)
        {
            foreach (Vector2Int cell in cells)
            {
                if (tiles[cell.x, cell.y] != null)
                {
                    tiles[cell.x, cell.y].Vanish();
                    tiles[cell.x, cell.y] = null;
                }
            }
        }

        /// <summary>Moves tiles as the board did. Moves must come in the order the board made them.</summary>
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

        private TileView CreateTile(char letter, int hint, Vector2Int cell)
        {
            TileView tile = Instantiate(tilePrefab, tilesRoot);
            tile.name = $"Tile {letter}";
            tile.Setup(letter, hint, CellSize, CellCenter(cell));
            return tile;
        }

        private Image CreateCellImage(string objectName, Transform parent, Color color)
        {
            var image = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = slotSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void PlaceCellImage(Image image, Vector2Int cell)
        {
            var rect = (RectTransform)image.transform;
            rect.sizeDelta = Vector2.one * (CellSize * 0.92f);
            rect.anchoredPosition = CellCenter(cell);
            TileView.SetCornerRadius(image, CellSize * 0.2f);
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
                    PlaceCellImage(slots[x, y], new Vector2Int(x, y));

                    if (tiles[x, y] != null)
                    {
                        tiles[x, y].SetSize(CellSize);
                        tiles[x, y].Snap(CellCenter(new Vector2Int(x, y)));
                    }
                }
            }

            for (int i = 0; i < pieceTiles.Count; i++)
            {
                pieceTiles[i].SetSize(CellSize);
                pieceTiles[i].Snap(CellCenter(piece.CellOf(i)));
            }

            ShowGhost(new List<Vector2Int>(ghostPositions));
        }
    }
}
