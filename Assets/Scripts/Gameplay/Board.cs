using System.Collections.Generic;
using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The letter grid. (0, 0) is the bottom-left cell; '\0' means empty.
    /// Movement and collision are pure grid logic, no physics (GDD 1.1).
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

        /// <summary>
        /// Fills the board from text rows, bottom row first; '.' or ' ' is empty.
        /// Used for levels that start with pre-filled cells.
        /// </summary>
        public void LoadRows(IReadOnlyList<string> rowsBottomUp)
        {
            for (int y = 0; y < rowsBottomUp.Count && y < Height; y++)
            {
                string row = WordDictionary.Normalize(rowsBottomUp[y].Replace(' ', '.'));
                for (int x = 0; x < row.Length && x < Width; x++)
                {
                    cells[x, y] = row[x] == '.' ? Empty : row[x];
                }
            }
        }

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
        /// Independent tile gravity: every letter falls straight down until it
        /// rests on the floor or another letter. Returns each move as (from, to),
        /// lowest first.
        /// </summary>
        public List<(Vector2Int from, Vector2Int to)> ApplyGravity()
        {
            var moves = new List<(Vector2Int, Vector2Int)>();
            for (int x = 0; x < Width; x++)
            {
                Compact(x, 0, moves);
            }

            return moves;
        }

        /// <summary>
        /// Only the letters above cleared cells fall; other overhangs stay as in
        /// Tetris. Returns each move as (from, to), lowest first.
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
                Compact(x, fromRow, moves);
            }

            return moves;
        }

        /// <summary>False when the top row has letters, so a new bottom row can't push them up.</summary>
        public bool CanPushRow
        {
            get
            {
                for (int x = 0; x < Width; x++)
                {
                    if (cells[x, Height - 1] != Empty)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Moves everything up one row and fills the bottom row with
        /// <paramref name="row"/> ('\0' for holes). Check <see cref="CanPushRow"/> first.
        /// Returns each move as (from, to), highest first.
        /// </summary>
        public List<(Vector2Int from, Vector2Int to)> PushRowFromBottom(char[] row)
        {
            var moves = new List<(Vector2Int, Vector2Int)>();
            for (int y = Height - 2; y >= 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (cells[x, y] == Empty)
                    {
                        continue;
                    }

                    cells[x, y + 1] = cells[x, y];
                    cells[x, y] = Empty;
                    moves.Add((new Vector2Int(x, y), new Vector2Int(x, y + 1)));
                }
            }

            for (int x = 0; x < Width; x++)
            {
                cells[x, 0] = x < row.Length ? row[x] : Empty;
            }

            return moves;
        }

        private void Compact(int x, int fromRow, List<(Vector2Int, Vector2Int)> moves)
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
    }
}
