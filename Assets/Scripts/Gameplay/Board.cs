using System.Collections.Generic;
using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The letter grid. (0, 0) is the bottom-left cell; '\0' means empty.
    /// </summary>
    public class Board
    {
        public const char Empty = '\0';

        private readonly char[,] cells;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new char[width, height];
        }

        public int Width { get; }
        public int Height { get; }

        public char this[int x, int y]
        {
            get => cells[x, y];
            set => cells[x, y] = value;
        }

        public char this[Vector2Int cell]
        {
            get => cells[cell.x, cell.y];
            set => cells[cell.x, cell.y] = value;
        }

        public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>True when the cell is on the board and has no letter.</summary>
        public bool IsFree(int x, int y) => IsInside(x, y) && cells[x, y] == Empty;

        public void Clear(IEnumerable<Vector2Int> toClear)
        {
            foreach (Vector2Int cell in toClear)
            {
                cells[cell.x, cell.y] = Empty;
            }
        }

        /// <summary>Rows with a letter in every cell, bottom first.</summary>
        public List<int> FullRows()
        {
            var rows = new List<int>();
            for (int y = 0; y < Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Width && full; x++)
                {
                    full = cells[x, y] != Empty;
                }

                if (full)
                {
                    rows.Add(y);
                }
            }

            return rows;
        }

        public List<Vector2Int> CellsInRows(IEnumerable<int> rows)
        {
            var result = new List<Vector2Int>();
            foreach (int y in rows)
            {
                for (int x = 0; x < Width; x++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        /// <summary>
        /// After clearing whole rows, moves every row above them down by the number
        /// of cleared rows below it, keeping its holes (classic Tetris line clear).
        /// Returns each letter that moved, as (from, to).
        /// </summary>
        public List<(Vector2Int from, Vector2Int to)> CollapseRows(ICollection<int> clearedRows)
        {
            var moves = new List<(Vector2Int, Vector2Int)>();
            int shift = 0;
            for (int y = 0; y < Height; y++)
            {
                if (clearedRows.Contains(y))
                {
                    shift++;
                    continue;
                }

                for (int x = 0; shift > 0 && x < Width; x++)
                {
                    if (cells[x, y] == Empty)
                    {
                        continue;
                    }

                    cells[x, y - shift] = cells[x, y];
                    cells[x, y] = Empty;
                    moves.Add((new Vector2Int(x, y), new Vector2Int(x, y - shift)));
                }
            }

            return moves;
        }

        /// <summary>
        /// After clearing cells, drops the letters above them into the gaps.
        /// Columns without cleared cells, and gaps below the lowest cleared cell
        /// of a column, are left alone (overhangs stay, like in Tetris).
        /// Returns each letter that moved, as (from, to).
        /// </summary>
        public List<(Vector2Int from, Vector2Int to)> DropAbove(IEnumerable<Vector2Int> cleared)
        {
            var lowestCleared = new Dictionary<int, int>();
            foreach (Vector2Int cell in cleared)
            {
                lowestCleared[cell.x] = lowestCleared.TryGetValue(cell.x, out int y) ? Mathf.Min(y, cell.y) : cell.y;
            }

            var moves = new List<(Vector2Int, Vector2Int)>();
            foreach (var (x, fromRow) in lowestCleared)
            {
                int landing = fromRow;
                for (int y = fromRow; y < Height; y++)
                {
                    if (cells[x, y] == Empty)
                    {
                        continue;
                    }

                    if (y != landing)
                    {
                        cells[x, landing] = cells[x, y];
                        cells[x, y] = Empty;
                        moves.Add((new Vector2Int(x, y), new Vector2Int(x, landing)));
                    }

                    landing++;
                }
            }

            return moves;
        }
    }
}
