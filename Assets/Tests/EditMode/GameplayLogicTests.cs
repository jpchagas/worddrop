using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WordDrop.Gameplay;

namespace WordDrop.Tests
{
    public class GameplayLogicTests
    {
        private static readonly WordDictionary Words =
            new WordDictionary(new[] { "SOL", "MÃE", "SOLA", "MAR", "MEL", "AVÓ", "ÁRVORE" });

        private static Board BoardWithRows(params string[] rowsBottomUp) => BoardOf(10, 20, rowsBottomUp);

        private static Board BoardOf(int width, int height, params string[] rowsBottomUp)
        {
            var board = new Board(width, height);
            board.LoadRows(rowsBottomUp);
            return board;
        }

        private static Vector2Int V(int x, int y) => new Vector2Int(x, y);

        private static Vector2Int[] Cells(Piece piece) =>
            Enumerable.Range(0, piece.BlockCount).Select(piece.CellOf).ToArray();

        // ---------------------------------------------------------------- Dictionary (GDD 1.3, 8.6)

        [Test]
        public void Normalize_UsesNfcUpperCaseAndKeepsAccents()
        {
            Assert.AreEqual("MAÇÃ", WordDictionary.Normalize("maçã"));
            // "A" + combining tilde (decomposed) becomes the single character "Ã".
            Assert.AreEqual("MÃE", WordDictionary.Normalize("MA\u0303E"));
            Assert.AreEqual(3, WordDictionary.Normalize("ma\u0303e").Length);
        }

        [Test]
        public void Dictionary_TreatsAccentedWordsAsDifferent()
        {
            Assert.IsTrue(Words.Contains("AVÓ"));
            Assert.IsFalse(Words.Contains("AVO"));
            Assert.IsFalse(Words.Contains("MAE"));
        }

        [Test]
        public void Dictionary_ReadsVersionAndSkipsCommentsAndShortWords()
        {
            WordDictionary dictionary = WordDictionary.FromText("#! versao: 2.3.1\n# comentário\n\nSOL\r\nPÉ\n");
            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual("2.3.1", dictionary.Version);
        }

        [Test]
        public void WordList_IsVersionedFitsTheBoardAndUsesPortugueseLetters()
        {
            string text = System.IO.File.ReadAllText("Assets/Content/Words/palavras.txt");
            WordDictionary dictionary = WordDictionary.FromText(text);

            Assert.Greater(dictionary.Count, 400);
            Assert.AreNotEqual("desconhecida", dictionary.Version);
            Assert.LessOrEqual(dictionary.MaxLength, 10);
            foreach (string line in text.Split('\n'))
            {
                string word = line.Trim();
                if (word.Length == 0 || word.StartsWith("#"))
                {
                    continue;
                }

                string entry = word.Split('|')[0];
                StringAssert.IsMatch("^[A-ZÁÂÃÀÉÊÍÓÔÕÚÇ]{3,10}$", WordDictionary.Normalize(entry), word);
                Assert.IsNotNull(dictionary.Definition(WordDictionary.Normalize(entry)), "Every word needs a meaning: " + word);
            }
        }

        // ---------------------------------------------------------------- Word finder

        [Test]
        public void Finder_FindsWordInsideARowAndPrefersTheLongest()
        {
            var found = WordFinder.Find(BoardWithRows("XSOLAX"), Words);
            Assert.AreEqual(new[] { "SOLA" }, found.Select(w => w.Word).ToArray());
        }

        [Test]
        public void Finder_ReadsColumnsTopToBottomButNotBackwards()
        {
            Assert.AreEqual(new[] { "SOL" }, WordFinder.Find(BoardWithRows("L", "O", "S"), Words).Select(w => w.Word));
            Assert.IsEmpty(WordFinder.Find(BoardWithRows("LOS"), Words));
            Assert.IsEmpty(WordFinder.Find(BoardWithRows("SO.L"), Words));
        }

        [Test]
        public void Finder_NeedsTheExactAccent()
        {
            Assert.AreEqual(new[] { "MÃE" }, WordFinder.Find(BoardWithRows("MÃE"), Words).Select(w => w.Word));
            Assert.IsEmpty(WordFinder.Find(BoardWithRows("MAE"), Words));
        }

        [Test]
        public void Finder_WildcardTileStandsForAnyLetter()
        {
            Board board = BoardWithRows("S*L");
            var found = WordFinder.Find(board, Words);
            Assert.AreEqual(new[] { "SOL" }, found.Select(w => w.Word));
        }

        [Test]
        public void Finder_BombTileIsNeverPartOfAWord()
        {
            Assert.IsEmpty(WordFinder.Find(BoardWithRows("S!L"), Words));
        }

        [Test]
        public void Dictionary_ReadsDefinitions()
        {
            WordDictionary dictionary = WordDictionary.FromText("SOL | A estrela que ilumina a Terra.\nMAR\n");
            Assert.IsTrue(dictionary.Contains("SOL"));
            Assert.AreEqual("A estrela que ilumina a Terra.", dictionary.Definition("SOL"));
            Assert.IsNull(dictionary.Definition("MAR"));
        }

        // ---------------------------------------------------------------- Scoring (GDD 3)

        [Test]
        public void Scoring_MatchesTheGdd()
        {
            Assert.AreEqual(90, Scoring.WordPoints(3));
            Assert.AreEqual(160, Scoring.WordPoints(4));
            Assert.AreEqual(640, Scoring.WordPoints(8));
            Assert.AreEqual(new[] { 10, 15, 20, 25, 25 }, Enumerable.Range(0, 5).Select(Scoring.MultiplierTenths));
            Assert.AreEqual(0, Scoring.MultiwordBonus(1));
            Assert.AreEqual(100, Scoring.MultiwordBonus(2));
            Assert.AreEqual(250, Scoring.MultiwordBonus(4));
        }

        [Test]
        public void Scoring_AddsRowsThenAppliesTheChainMultiplier()
        {
            // (90 + 150) × 1.5 = 360
            Assert.AreEqual(360, Scoring.PassPoints(new[] { 3 }, rows: 1, chainIndex: 1));
        }

        // ---------------------------------------------------------------- Resolver (GDD 2)

        [Test]
        public void Resolver_ClearsAWordAndScoresIt()
        {
            Board board = BoardWithRows("SOL");
            PlacementResult result = Resolver.Resolve(board, Words, GravityMode.Independent);

            Assert.AreEqual(1, result.WordCount);
            Assert.AreEqual(90, result.TotalPoints);
            Assert.AreEqual(Board.Empty, board[0, 0]);
        }

        [Test]
        public void Resolver_SharedTileIsClearedOnceButBothWordsScore()
        {
            // Row 2 reads M-A-R and column 0 reads M-A-R top to bottom; they share the M.
            Board board = BoardWithRows("R", "A", "MAR");
            PlacementResult result = Resolver.Resolve(board, Words, GravityMode.Independent);

            ResolutionPass pass = result.Passes[0];
            Assert.AreEqual(2, pass.Words.Count);
            Assert.AreEqual(5, pass.ClearedCells.Count);
            Assert.AreEqual(90 + 90 + 100, result.TotalPoints, "Two words plus the 2-word bonus.");
        }

        [Test]
        public void Resolver_ClearsFullRowsInTheSamePassAsWords()
        {
            Board board = BoardOf(4, 6, "ZXKW", "SOL");
            PlacementResult result = Resolver.Resolve(board, Words, GravityMode.Independent);

            ResolutionPass pass = result.Passes[0];
            Assert.AreEqual(1, pass.Words.Count);
            Assert.AreEqual(new[] { 0 }, pass.Rows);
            Assert.AreEqual(90 + Scoring.RowBonus, pass.Points);
        }

        [Test]
        public void Resolver_ChainUsesTheNextMultiplier()
        {
            // Level 5's setup: SOL clears, then E and L fall next to M and form MEL.
            Board board = BoardWithRows(".MSOL.....", "..ELI.....", "....U.....");
            PlacementResult result = Resolver.Resolve(board, Words, GravityMode.Independent);

            var clears = result.Passes.Where(p => p.HasClear).ToList();
            Assert.AreEqual(new[] { "SOL" }, clears[0].Words.Select(w => w.Word));
            Assert.AreEqual(new[] { "MEL" }, clears[1].Words.Select(w => w.Word));
            Assert.AreEqual(1, result.Chains);
            Assert.AreEqual(90 + 135 + 100, result.TotalPoints, "×1.5 on the chain, plus the 2-word bonus.");
        }

        [Test]
        public void Resolver_IndependentGravityDropsOverhangsEvenWithoutAClear()
        {
            Board board = BoardOf(3, 4, "X..", "...", ".Y.");
            PlacementResult result = Resolver.Resolve(board, Words, GravityMode.Independent);

            Assert.AreEqual(0, result.TotalPoints);
            Assert.AreEqual('Y', board[1, 0]);
        }

        [Test]
        public void Resolver_AboveClearedOnlyKeepsUnrelatedOverhangs()
        {
            Board board = BoardOf(6, 4, "SOL...", "...Y..", "..X...");
            board[3, 0] = Board.Empty;
            Resolver.Resolve(board, Words, GravityMode.AboveClearedOnly);

            Assert.AreEqual('X', board[2, 0], "X was above a cleared cell, so it falls.");
            Assert.AreEqual('Y', board[3, 1], "Y was not, so it stays in the air.");
        }

        // ---------------------------------------------------------------- Board

        [Test]
        public void Board_PushRowFromBottomMovesEverythingUp()
        {
            Board board = BoardOf(3, 3, "AB.");
            Assert.IsTrue(board.CanPushRow);

            var moves = board.PushRowFromBottom(new[] { 'X', Board.Empty, 'Z' });

            Assert.AreEqual('A', board[0, 1]);
            Assert.AreEqual('X', board[0, 0]);
            Assert.AreEqual(Board.Empty, board[1, 0]);
            Assert.AreEqual(2, moves.Count);
        }

        [Test]
        public void Board_CannotPushWhenTheTopRowIsUsed()
        {
            Assert.IsFalse(BoardOf(2, 2, "..", "A.").CanPushRow);
        }

        // ---------------------------------------------------------------- Pieces

        [Test]
        public void Piece_TRotatesClockwise()
        {
            var piece = new Piece(PieceType.T, "ABCD".ToCharArray()) { X = 0, Y = 0 };
            CollectionAssert.AreEqual(new[] { V(0, 1), V(1, 1), V(2, 1), V(1, 2) }, Cells(piece));

            Assert.IsTrue(piece.TryRotate(new Board(10, 20)));
            CollectionAssert.AreEqual(new[] { V(1, 2), V(1, 1), V(1, 0), V(2, 1) }, Cells(piece));
        }

        [Test]
        public void Piece_EveryShapeComesBackAfterFourRotations()
        {
            var board = new Board(10, 20);
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                var letters = new string('A', Piece.BlockCountOf(type)).ToCharArray();
                var piece = new Piece(type, letters) { X = 3, Y = 10 };
                Vector2Int[] start = Cells(piece);
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsTrue(piece.TryRotate(board), type.ToString());
                }

                CollectionAssert.AreEqual(start, Cells(piece), type.ToString());
            }
        }

        [Test]
        public void Piece_GroupsHaveTwoToFourTiles()
        {
            foreach (PieceType type in System.Enum.GetValues(typeof(PieceType)))
            {
                if (type != PieceType.Single) // Booster tile only, never dealt.
                {
                    Assert.That(Piece.BlockCountOf(type), Is.InRange(2, 4), type.ToString());
                }
            }
        }

        [Test]
        public void Piece_KicksAwayFromTheWallToRotate()
        {
            var board = new Board(10, 20);
            var piece = new Piece(PieceType.I, "ABCD".ToCharArray(), rotation: 1) { X = 7, Y = 10 };
            Assert.IsTrue(piece.TryRotate(board));
            Assert.IsTrue(Cells(piece).All(c => board.IsInside(c.x, c.y)));
        }

        [Test]
        public void Piece_DropDistanceStopsOnTheStack()
        {
            Board board = BoardWithRows("XXXXXXXXXX");
            var piece = new Piece(PieceType.O, "ABCD".ToCharArray()) { X = 0, Y = 10 };
            Assert.AreEqual(9, piece.DropDistance(board));
        }

        // ---------------------------------------------------------------- Generator

        [Test]
        public void Generator_DealsTheCuratedSequenceFirst()
        {
            var generator = new PieceGenerator(Words, 0f, Difficulty.Advanced,
                new[] { "I3 SOL 2", "O ABCD" }, new System.Random(1));

            Piece first = generator.Next();
            Assert.AreEqual(PieceType.I3, first.Type);
            Assert.AreEqual("SOL", new string(first.Letters));
            Assert.AreEqual(2, first.Rotation);
            Assert.AreEqual(PieceType.O, generator.Next().Type);
        }

        [Test]
        public void Generator_ShapesDependOnDifficulty()
        {
            var easy = new[] { PieceType.I, PieceType.O, PieceType.I2, PieceType.I3, PieceType.V3 };
            var medium = new[] { PieceType.I, PieceType.O, PieceType.T, PieceType.L, PieceType.J, PieceType.I2, PieceType.I3, PieceType.V3 };
            var advanced = new[] { PieceType.I, PieceType.O, PieceType.T, PieceType.S, PieceType.Z, PieceType.L, PieceType.J };

            AssertShapes(Difficulty.Easy, easy);
            AssertShapes(Difficulty.Medium, medium);
            AssertShapes(Difficulty.Advanced, advanced);
        }

        private static void AssertShapes(Difficulty difficulty, PieceType[] expected)
        {
            var generator = new PieceGenerator(Words, 0f, difficulty, null, new System.Random(3));
            var dealt = Enumerable.Range(0, 400).Select(_ => generator.Next().Type).Distinct().ToList();
            CollectionAssert.AreEquivalent(expected, dealt, difficulty.ToString());
        }

        [Test]
        public void Hints_EasyColorsEveryPieceAndColoredLettersSpellWords()
        {
            var dictionary = new WordDictionary(new[] { "SOL", "CASA", "BOLA", "JANELA", "MÃE", "PATO" });
            var generator = new PieceGenerator(dictionary, 0f, Difficulty.Easy, null, new System.Random(21));

            // Collect the hinted letters in dealing order (row order within a piece);
            // consecutive letters of the same color must add up to dictionary words.
            string current = "";
            int currentColor = 0;
            for (int i = 0; i < 200; i++)
            {
                Piece piece = generator.Next();
                int[] row = Piece.RowBlocksOf(piece.Type);
                Assert.IsTrue(piece.Hints.Any(h => h > 0), "Easy: every piece gets a hint.");

                foreach (int block in row.Where(b => piece.Hints[b] > 0))
                {
                    if (piece.Hints[block] != currentColor && current.Length > 0)
                    {
                        Assert.IsTrue(dictionary.Contains(current), current);
                        current = "";
                    }

                    currentColor = piece.Hints[block];
                    current += piece.Letters[block];
                }
            }
        }

        [Test]
        public void Hints_MediumColorsAboutHalfAndAdvancedNone()
        {
            int Hinted(Difficulty difficulty)
            {
                var generator = new PieceGenerator(Words, 0f, difficulty, null, new System.Random(8));
                return Enumerable.Range(0, 1000).Count(_ => generator.Next().Hints.Any(h => h > 0));
            }

            Assert.That(Hinted(Difficulty.Medium), Is.InRange(450, 850), "Split words also hint the following pieces.");
            Assert.AreEqual(0, Hinted(Difficulty.Advanced));
        }

        [Test]
        public void Generator_ShuffleRerollKeepsShapeAndPosition()
        {
            var generator = new PieceGenerator(Words, 0f, Difficulty.Easy, null, new System.Random(4));
            var piece = new Piece(PieceType.I3, "XYZ".ToCharArray()) { X = 4, Y = 7 };
            Piece shuffled = generator.Reroll(piece);

            Assert.AreEqual(PieceType.I3, shuffled.Type);
            Assert.AreEqual(new Vector2Int(4, 7), new Vector2Int(shuffled.X, shuffled.Y));
            Assert.IsTrue(Words.Contains(new string(shuffled.Letters)), "An I3 row gets a whole word.");
        }

        [Test]
        public void Generator_WordPiecesSpellTheirWordWhenRotatedUpright()
        {
            var dictionary = new WordDictionary(new[] { "SOL", "MÃE", "LUA", "CASA", "BOLA" });
            var generator = new PieceGenerator(dictionary, 1f, Difficulty.Advanced, null, new System.Random(11));

            for (int i = 0; i < 80; i++)
            {
                Piece piece = generator.Next();
                int length = PieceGenerator.WordLengthFor(piece.Type);
                if (length == 0)
                {
                    continue;
                }

                Assert.GreaterOrEqual(piece.Rotation, 2, "Word pieces start turned away from the word.");

                var board = new Board(10, 20);
                piece.X = 3;
                piece.Y = 10;
                while (piece.Rotation != 1)
                {
                    piece.TryRotate(board);
                }

                piece.PlaceOn(board);
                Assert.IsTrue(WordFinder.Find(board, dictionary).Any(w => w.Word.Length == length), new string(piece.Letters));
            }
        }

        [Test]
        public void SeededRandom_DealsTheSamePiecesForTheSameDay()
        {
            string Deal(int seed)
            {
                var generator = new PieceGenerator(Words, 0.5f, Difficulty.Medium, null, new SeededRandom(seed));
                return string.Join(",", Enumerable.Range(0, 30).Select(_ =>
                {
                    Piece p = generator.Next();
                    return $"{p.Type}{new string(p.Letters)}{p.Rotation}";
                }));
            }

            Assert.AreEqual(Deal(20261022), Deal(20261022));
            Assert.AreNotEqual(Deal(20261022), Deal(20261023));
        }

        // ---------------------------------------------------------------- Objectives

        [Test]
        public void Objectives_CompleteOnlyWhenAllTargetsAreMet()
        {
            var tracker = new ObjectiveTracker(new[]
            {
                new LevelObjective(ObjectiveType.Words, 2), new LevelObjective(ObjectiveType.Rows, 1),
            });

            tracker.Record(new PlacementResult { WordCount = 2 });
            Assert.IsFalse(tracker.AllComplete);
            Assert.AreEqual(0.5f, tracker.Fraction, 0.001f);

            tracker.Record(new PlacementResult { RowCount = 1 });
            Assert.IsTrue(tracker.AllComplete);
        }

        [Test]
        public void Objectives_EndlessHasNoneAndNeverCompletes()
        {
            Assert.IsFalse(new ObjectiveTracker(new LevelObjective[0]).AllComplete);
        }

        [Test]
        public void Levels_StarsNeedTheThresholds()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.starScores = new[] { 100, 200, 300 };
            Assert.AreEqual(0, level.StarsFor(99));
            Assert.AreEqual(2, level.StarsFor(250));
            Object.DestroyImmediate(level);
        }
    }
}
