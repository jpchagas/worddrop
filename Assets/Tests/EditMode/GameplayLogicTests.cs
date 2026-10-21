using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WordDrop.Gameplay;

namespace WordDrop.Tests
{
    public class GameplayLogicTests
    {
        private static readonly WordDictionary Words = new WordDictionary(new[] { "SOL", "MÃE", "SOLA", "MAR", "ÁRVORE" });

        private static Board BoardWithRow(int y, string letters, int width = 6, int height = 9)
        {
            var board = new Board(width, height);
            for (int x = 0; x < letters.Length; x++)
            {
                board[x, y] = letters[x] == '.' ? Board.Empty : letters[x];
            }

            return board;
        }

        [Test]
        public void Normalize_RemovesAccentsAndCedilla()
        {
            Assert.AreEqual("MACA", WordDictionary.Normalize("Maçã"));
            Assert.AreEqual("ARVORE", WordDictionary.Normalize("árvore"));
        }

        [Test]
        public void Dictionary_MatchesPlainLettersAndKeepsAccentsForDisplay()
        {
            Assert.IsTrue(Words.Contains("MAE"));
            Assert.AreEqual("MÃE", Words.Display("MAE"));
            Assert.AreEqual(6, Words.MaxLength);
        }

        [Test]
        public void Dictionary_SkipsCommentsBlankLinesAndShortWords()
        {
            WordDictionary dictionary = WordDictionary.FromText("# comentário\n\nSOL\r\nPÉ\n");
            Assert.AreEqual(1, dictionary.Count);
        }

        [Test]
        public void Finder_FindsWordInsideARow()
        {
            Board board = BoardWithRow(0, "XMARX");
            var found = WordFinder.Find(board, Words);

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("MAR", found[0].Key);
            CollectionAssert.AreEqual(new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, found[0].Cells);
        }

        [Test]
        public void Finder_PrefersTheLongestWord()
        {
            var found = WordFinder.Find(BoardWithRow(0, "SOLA"), Words);
            Assert.AreEqual(new[] { "SOLA" }, found.Select(w => w.Key).ToArray());
        }

        [Test]
        public void Finder_ReadsColumnsTopToBottom()
        {
            var board = new Board(6, 9);
            board[2, 2] = 'S';
            board[2, 1] = 'O';
            board[2, 0] = 'L';

            var found = WordFinder.Find(board, Words);
            Assert.AreEqual(new[] { "SOL" }, found.Select(w => w.Key).ToArray());
        }

        [Test]
        public void Finder_DoesNotReadBackwardsOrAcrossGaps()
        {
            Assert.IsEmpty(WordFinder.Find(BoardWithRow(0, "LOS"), Words));
            Assert.IsEmpty(WordFinder.Find(BoardWithRow(0, "SO.L"), Words));
        }

        [Test]
        public void Finder_ReportsRowAndColumnWordsSharingACell()
        {
            // Row 2 reads M-A-R and column 0 reads M-A-R top to bottom; they share the top-left M.
            Board board = BoardWithRow(2, "MAR");
            board[0, 1] = 'A';
            board[0, 0] = 'R';

            var found = WordFinder.Find(board, Words);

            Assert.AreEqual(2, found.Count);
            Assert.IsTrue(found.All(w => w.Key == "MAR" && w.Cells.Contains(new Vector2Int(0, 2))));
        }

        [Test]
        public void DropAbove_FillsTheGapButKeepsOtherOverhangs()
        {
            var board = new Board(2, 5);
            board[0, 0] = 'A';
            board[0, 2] = 'B'; // Cleared below.
            board[0, 4] = 'C';
            board[1, 3] = 'D'; // Overhang in a column with nothing cleared.

            board[0, 2] = Board.Empty;
            var moves = board.DropAbove(new[] { new Vector2Int(0, 2) });

            Assert.AreEqual('A', board[0, 0]);
            Assert.AreEqual(Board.Empty, board[0, 1], "The gap below the cleared cell stays.");
            Assert.AreEqual('C', board[0, 2]);
            Assert.AreEqual('D', board[1, 3]);
            Assert.AreEqual(1, moves.Count);
        }

        [Test]
        public void Piece_TRotatesClockwise()
        {
            var piece = new Piece(PieceType.T, "ABCD".ToCharArray()) { X = 0, Y = 0 };
            // Spawn: row A-B-C with D on top of B.
            CollectionAssert.AreEqual(new[] { V(0, 1), V(1, 1), V(2, 1), V(1, 2) }, Cells(piece));

            Assert.IsTrue(piece.TryRotate(new Board(10, 20)));
            // Now a column A-B-C read top to bottom, with D sticking out to the right.
            CollectionAssert.AreEqual(new[] { V(1, 2), V(1, 1), V(1, 0), V(2, 1) }, Cells(piece));
        }

        [Test]
        public void Piece_FourRotationsComeBackToTheStart()
        {
            var board = new Board(10, 20);
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                var piece = new Piece(type, "ABCD".ToCharArray()) { X = 3, Y = 10 };
                Vector2Int[] start = Cells(piece);
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsTrue(piece.TryRotate(board), type.ToString());
                }

                CollectionAssert.AreEqual(start, Cells(piece), type.ToString());
            }
        }

        [Test]
        public void Piece_KicksAwayFromTheWallToRotate()
        {
            var board = new Board(10, 20);
            // Vertical I against the right wall: rotating back to flat needs a kick left.
            var piece = new Piece(PieceType.I, "ABCD".ToCharArray(), rotation: 1) { X = 7, Y = 10 };
            Assert.AreEqual(9, piece.CellOf(0).x);

            Assert.IsTrue(piece.TryRotate(board));
            Assert.IsTrue(Cells(piece).All(c => board.IsInside(c.x, c.y)));
        }

        [Test]
        public void Generator_DealsEachShapeOncePerBagOfSeven()
        {
            var generator = new PieceGenerator(Words, wordPieceChance: 0f, new System.Random(3));
            var shapes = Enumerable.Range(0, 7).Select(_ => generator.Next().Type).ToList();
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(PieceType)), shapes);
        }

        [Test]
        public void Generator_WordPiecesSpellTheirWordWhenRotatedUpright()
        {
            var dictionary = new WordDictionary(new[] { "SOL", "MAR", "LUA", "CASA", "BOLA" });
            var generator = new PieceGenerator(dictionary, wordPieceChance: 1f, new System.Random(11));

            for (int i = 0; i < 70; i++)
            {
                Piece piece = generator.Next();
                int length = PieceGenerator.WordLengthFor(piece.Type);
                if (length == 0)
                {
                    continue;
                }

                Assert.GreaterOrEqual(piece.Rotation, 2, "Word pieces start turned away from the word.");

                // Rotate until it reads top to bottom (rotation 1) and look for the word on a board.
                var board = new Board(10, 20);
                piece.X = 3;
                piece.Y = 10;
                while (piece.Rotation != 1)
                {
                    piece.TryRotate(board);
                }

                piece.PlaceOn(board);
                Assert.IsTrue(WordFinder.Find(board, dictionary).Any(w => w.Key.Length == length),
                    new string(piece.Letters));
            }
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        private static Vector2Int[] Cells(Piece piece) =>
            Enumerable.Range(0, Piece.BlockCount).Select(piece.CellOf).ToArray();

        [Test]
        public void FullRows_FindsOnlyCompleteRows()
        {
            Board board = BoardWithRow(0, "ABCD", width: 4, height: 3);
            board[0, 1] = 'E';
            CollectionAssert.AreEqual(new[] { 0 }, board.FullRows());
        }

        [Test]
        public void CollapseRows_ShiftsRowsDownKeepingHoles()
        {
            var board = new Board(3, 4);
            board[0, 2] = 'A'; // Row 2: A . B  (has a hole)
            board[2, 2] = 'B';
            board[1, 3] = 'C'; // Row 3: . C .
            var cleared = new[] { 0, 1 }; // Two full rows, already emptied.

            var moves = board.CollapseRows(cleared);

            Assert.AreEqual('A', board[0, 0]);
            Assert.AreEqual(Board.Empty, board[1, 0], "The hole moves down with its row.");
            Assert.AreEqual('B', board[2, 0]);
            Assert.AreEqual('C', board[1, 1]);
            Assert.AreEqual(Board.Empty, board[1, 3]);
            Assert.AreEqual(3, moves.Count);
        }

        [Test]
        public void Scoring_RowsFollowTetris()
        {
            Assert.AreEqual(100, Scoring.PointsForRows(1, 1));
            Assert.AreEqual(800, Scoring.PointsForRows(4, 1));
            Assert.AreEqual(1000, Scoring.PointsForRows(5, 1));
            Assert.AreEqual(600, Scoring.PointsForRows(2, 2));
        }

        [Test]
        public void Finder_FindsATenLetterWordAcrossTheBoard()
        {
            var dictionary = new WordDictionary(new[] { "DINOSSAURO", "SAURO", "SOL" });
            var found = WordFinder.Find(BoardWithRow(0, "DINOSSAURO", width: 10, height: 20), dictionary);
            Assert.AreEqual(new[] { "DINOSSAURO" }, found.Select(w => w.Key).ToArray());
        }

        [Test]
        public void WordList_FitsTheBoardAndUsesPlainLetters()
        {
            string text = System.IO.File.ReadAllText("Assets/Content/Words/palavras.txt");
            WordDictionary dictionary = WordDictionary.FromText(text);

            Assert.Greater(dictionary.Count, 400);
            Assert.LessOrEqual(dictionary.MaxLength, 10);
            foreach (string line in text.Split('\n'))
            {
                string word = line.Trim();
                if (word.Length == 0 || word.StartsWith("#"))
                {
                    continue;
                }

                StringAssert.IsMatch("^[A-Z]{3,10}$", WordDictionary.Normalize(word), word);
            }
        }

        [Test]
        public void Scoring_MatchesTheDesign()
        {
            Assert.AreEqual(60, Scoring.PointsFor(3, 1));
            Assert.AreEqual(120, Scoring.PointsFor(4, 1));
            Assert.AreEqual(240, Scoring.PointsFor(4, 2));
        }
    }
}
