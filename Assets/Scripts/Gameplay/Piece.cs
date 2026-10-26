using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// Piece shapes: the seven Tetris tetrominoes plus smaller groups of 2–3
    /// tiles (GDD 1.2), used mostly in early levels.
    /// </summary>
    public enum PieceType
    {
        I, O, T, S, Z, L, J,
        /// <summary>Two tiles in a line.</summary>
        I2,
        /// <summary>Three tiles in a line.</summary>
        I3,
        /// <summary>Three tiles in a corner (an L of three).</summary>
        V3,
        /// <summary>One tile; only used by the Bomb and Star boosters, never dealt.</summary>
        Single,
    }

    /// <summary>
    /// The falling piece: 2–4 blocks, each with a letter. The shape lives in a
    /// small square box whose bottom-left corner sits at (X, Y) on the board.
    /// Rotating turns the box 90° clockwise and the letters travel with their blocks.
    /// </summary>
    public class Piece
    {
        // Spawn shapes, y up, indexed by PieceType. For pieces that can carry a
        // word (see PieceGenerator.WordLengthFor), the first blocks are the long
        // row, left to right.
        private static readonly Vector2Int[][] Shapes =
        {
            /* I  */ new[] { V(0, 2), V(1, 2), V(2, 2), V(3, 2) },
            /* O  */ new[] { V(0, 1), V(1, 1), V(1, 0), V(0, 0) },
            /* T  */ new[] { V(0, 1), V(1, 1), V(2, 1), V(1, 2) },
            /* S  */ new[] { V(0, 1), V(1, 1), V(1, 2), V(2, 2) },
            /* Z  */ new[] { V(0, 2), V(1, 2), V(1, 1), V(2, 1) },
            /* L  */ new[] { V(0, 1), V(1, 1), V(2, 1), V(2, 2) },
            /* J  */ new[] { V(0, 1), V(1, 1), V(2, 1), V(0, 2) },
            /* I2 */ new[] { V(0, 1), V(1, 1) },
            /* I3 */ new[] { V(0, 1), V(1, 1), V(2, 1) },
            /* V3 */ new[] { V(0, 1), V(0, 0), V(1, 0) },
            /* 1  */ new[] { V(0, 0) },
        };

        // The blocks of each shape's longest straight row, left to right: where
        // a word or part of a word is written (see PieceGenerator).
        private static readonly int[][] RowBlocks =
        {
            /* I  */ new[] { 0, 1, 2, 3 },
            /* O  */ new[] { 0, 1 },
            /* T  */ new[] { 0, 1, 2 },
            /* S  */ new[] { 0, 1 },
            /* Z  */ new[] { 0, 1 },
            /* L  */ new[] { 0, 1, 2 },
            /* J  */ new[] { 0, 1, 2 },
            /* I2 */ new[] { 0, 1 },
            /* I3 */ new[] { 0, 1, 2 },
            /* V3 */ new[] { 1, 2 },
            /* 1  */ new[] { 0 },
        };

        /// <summary>The Bomb booster's tile: clears the 3×3 area around where it lands.</summary>
        public const char Bomb = '!';

        /// <summary>The Star booster's tile: a wildcard that counts as any letter.</summary>
        public const char Wildcard = '*';

        /// <summary>Hint color of a tile with no hint.</summary>
        public const int NoHint = 0;

        // Where to try moving the piece when a rotation is blocked (simple wall kicks).
        private static readonly Vector2Int[] Kicks = { V(0, 0), V(-1, 0), V(1, 0), V(-2, 0), V(2, 0), V(0, 1) };

        public Piece(PieceType type, char[] letters, int rotation = 0, int[] hints = null)
        {
            if (letters.Length != BlockCountOf(type))
            {
                throw new System.ArgumentException($"{type} needs {BlockCountOf(type)} letters, got {letters.Length}.");
            }

            Type = type;
            Letters = letters;
            Hints = hints ?? new int[letters.Length];
            Rotation = rotation % 4;
        }

        public PieceType Type { get; }
        public char[] Letters { get; }

        /// <summary>
        /// Hint color per block: tiles with the same non-zero value belong to the
        /// same word, possibly split over several pieces (0 = no hint).
        /// </summary>
        public int[] Hints { get; }
        /// <summary>Quarter turns clockwise from the spawn shape (0–3).</summary>
        public int Rotation { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }

        public int BlockCount => Letters.Length;
        public int BoxSize => BoxSizeOf(Type);

        public static int BlockCountOf(PieceType type) => Shapes[(int)type].Length;

        /// <summary>Block indices of the shape's longest straight row, left to right at rotation 0.</summary>
        public static int[] RowBlocksOf(PieceType type) => RowBlocks[(int)type];

        public static int BoxSizeOf(PieceType type)
        {
            switch (type)
            {
                case PieceType.I: return 4;
                case PieceType.Single: return 1;
                case PieceType.O:
                case PieceType.I2:
                case PieceType.V3: return 2;
                default: return 3;
            }
        }

        public Vector2Int CellOf(int block) => CellOf(block, X, Y, Rotation);

        public bool FitsAt(Board board, int x, int y) => FitsAt(board, x, y, Rotation);

        /// <summary>How many rows the piece can still fall before it lands.</summary>
        public int DropDistance(Board board)
        {
            int distance = 0;
            while (FitsAt(board, X, Y - distance - 1))
            {
                distance++;
            }

            return distance;
        }

        /// <summary>Rotates 90° clockwise, nudging the piece if needed. False if it can't turn.</summary>
        public bool TryRotate(Board board)
        {
            int next = (Rotation + 1) % 4;
            foreach (Vector2Int kick in Kicks)
            {
                if (FitsAt(board, X + kick.x, Y + kick.y, next))
                {
                    X += kick.x;
                    Y += kick.y;
                    Rotation = next;
                    return true;
                }
            }

            return false;
        }

        public void PlaceOn(Board board)
        {
            for (int i = 0; i < BlockCount; i++)
            {
                board[CellOf(i)] = Letters[i];
            }
        }

        private bool FitsAt(Board board, int x, int y, int rotation)
        {
            for (int i = 0; i < BlockCount; i++)
            {
                Vector2Int cell = CellOf(i, x, y, rotation);
                if (!board.IsFree(cell.x, cell.y))
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2Int CellOf(int block, int x, int y, int rotation)
        {
            Vector2Int p = Shapes[(int)Type][block];
            int last = BoxSize - 1;
            for (int r = 0; r < rotation; r++)
            {
                p = new Vector2Int(p.y, last - p.x); // 90° clockwise inside the box.
            }

            return new Vector2Int(x + p.x, y + p.y);
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);
    }
}
