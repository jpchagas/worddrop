using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>The seven Tetris pieces, named after the letter they look like.</summary>
    public enum PieceType { I, O, T, S, Z, L, J }

    /// <summary>
    /// The falling tetromino: four blocks, each with a letter. The shape lives in
    /// a small square box (4×4 for I, 2×2 for O, 3×3 for the rest) whose
    /// bottom-left corner sits at (X, Y) on the board. Rotating turns the box
    /// 90° clockwise and the letters travel with their blocks.
    /// </summary>
    public class Piece
    {
        public const int BlockCount = 4;

        // Spawn shapes, y up. For I, T, L and J the first blocks are the long row,
        // left to right, which is where a word is written (see PieceGenerator).
        private static readonly Vector2Int[][] Shapes =
        {
            /* I */ new[] { V(0, 2), V(1, 2), V(2, 2), V(3, 2) },
            /* O */ new[] { V(0, 1), V(1, 1), V(1, 0), V(0, 0) },
            /* T */ new[] { V(0, 1), V(1, 1), V(2, 1), V(1, 2) },
            /* S */ new[] { V(0, 1), V(1, 1), V(1, 2), V(2, 2) },
            /* Z */ new[] { V(0, 2), V(1, 2), V(1, 1), V(2, 1) },
            /* L */ new[] { V(0, 1), V(1, 1), V(2, 1), V(2, 2) },
            /* J */ new[] { V(0, 1), V(1, 1), V(2, 1), V(0, 2) },
        };

        // Where to try moving the piece when a rotation is blocked (simple wall kicks).
        private static readonly Vector2Int[] Kicks = { V(0, 0), V(-1, 0), V(1, 0), V(-2, 0), V(2, 0), V(0, 1) };

        public Piece(PieceType type, char[] letters, int rotation = 0)
        {
            Type = type;
            Letters = letters;
            Rotation = rotation;
        }

        public PieceType Type { get; }
        public char[] Letters { get; }
        /// <summary>Quarter turns clockwise from the spawn shape (0–3).</summary>
        public int Rotation { get; private set; }
        public int X { get; set; }
        public int Y { get; set; }

        public int BoxSize => BoxSizeOf(Type);

        public static int BoxSizeOf(PieceType type) => type == PieceType.I ? 4 : type == PieceType.O ? 2 : 3;

        public Vector2Int CellOf(int block) => CellOf(block, X, Y, Rotation);

        public bool FitsAt(Board board, int x, int y) => FitsAt(board, x, y, Rotation);

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
