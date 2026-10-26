using System;
using System.Collections.Generic;

namespace WordDrop.Gameplay
{
    /// <summary>How often a shape is dealt, relative to the other entries.</summary>
    [Serializable]
    public struct PieceWeight
    {
        public PieceType type;
        public int weight;

        public PieceWeight(PieceType type, int weight)
        {
            this.type = type;
            this.weight = weight;
        }
    }

    /// <summary>
    /// Deals the pieces (GDD 1.2, 12.1). A level may start with a curated
    /// sequence; after it, shapes are weighted-random for the difficulty.
    ///
    /// Color hints: a hinted piece gets part of a word on its long row, and the
    /// rest of the word goes on the next piece(s), all in one hint color. Two
    /// tiles of the same color placed side by side, in order, spell the word.
    /// How many pieces are hinted depends on <see cref="Difficulty"/>.
    ///
    /// Pieces without a hint may still carry a whole word (wordPieceChance),
    /// turned away from it, with no color. Other letters are random, weighted by
    /// Portuguese frequency, accented letters included.
    /// </summary>
    public class PieceGenerator
    {
        public const int HintColorCount = 5;
        private const int MaxHintWordLength = 6;

        // Approximate Portuguese letter frequencies (per mil), with accents kept
        // separate (GDD 1.3). Q, X, K, W and Y are left out because they almost
        // never help to form short words.
        private static readonly (char letter, int weight)[] LetterWeights =
        {
            ('A', 140), ('E', 122), ('O', 103), ('S', 78), ('R', 65), ('I', 60), ('N', 50),
            ('D', 50), ('M', 47), ('U', 45), ('T', 43), ('C', 37), ('L', 28), ('P', 25),
            ('V', 17), ('G', 13), ('H', 13), ('B', 10), ('F', 10), ('Z', 5), ('J', 4),
            ('Ã', 7), ('Ç', 5), ('É', 4), ('Á', 4), ('Í', 4), ('Ê', 4), ('Ó', 3), ('Ô', 3),
            ('Â', 3), ('Ú', 2), ('Õ', 1),
        };

        private static readonly int TotalLetterWeight;

        private readonly WordDictionary dictionary;
        private readonly float wordPieceChance;
        private readonly IReadOnlyList<string> sequence;
        private readonly Random random;
        private PieceWeight[] shapeWeights;
        private int totalShapeWeight;
        private Difficulty difficulty;
        private int sequenceIndex;
        private string pendingRest = "";
        private int pendingColor;
        private int lastColor;

        static PieceGenerator()
        {
            foreach (var (_, weight) in LetterWeights)
            {
                TotalLetterWeight += weight;
            }
        }

        public PieceGenerator(WordDictionary dictionary, float wordPieceChance, Difficulty difficulty,
            IReadOnlyList<string> sequence, Random random)
        {
            this.dictionary = dictionary;
            this.wordPieceChance = wordPieceChance;
            this.sequence = sequence ?? Array.Empty<string>();
            this.random = random;
            Difficulty = difficulty;
        }

        /// <summary>Changes which shapes are dealt and how many pieces get hints (Endless raises it over time).</summary>
        public Difficulty Difficulty
        {
            get => difficulty;
            set
            {
                difficulty = value;
                shapeWeights = DifficultyRules.Weights(value);
                totalShapeWeight = 0;
                foreach (PieceWeight entry in shapeWeights)
                {
                    totalShapeWeight += Math.Max(0, entry.weight);
                }
            }
        }

        /// <summary>The length of a whole word a piece type can carry, or 0.</summary>
        public static int WordLengthFor(PieceType type)
        {
            switch (type)
            {
                case PieceType.I: return 4;
                case PieceType.T:
                case PieceType.L:
                case PieceType.J:
                case PieceType.I3: return 3;
                default: return 0;
            }
        }

        /// <summary>
        /// Parses a curated sequence entry: "SHAPE LETTERS [ROTATION]", e.g.
        /// "I3 SOL 2" or "T LUAR". Letters go to the blocks in order (the long
        /// row first, left to right).
        /// </summary>
        public static Piece ParseEntry(string entry)
        {
            string[] parts = entry.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !Enum.TryParse(parts[0], true, out PieceType type))
            {
                throw new FormatException($"Invalid piece entry '{entry}'. Expected 'SHAPE LETTERS [ROTATION]'.");
            }

            char[] letters = WordDictionary.Normalize(parts[1]).ToCharArray();
            int rotation = parts.Length > 2 ? int.Parse(parts[2]) : 0;
            return new Piece(type, letters, rotation);
        }

        public Piece Next()
        {
            if (sequenceIndex < sequence.Count)
            {
                return CuratedPiece(ParseEntry(sequence[sequenceIndex++]));
            }

            PieceType type = RandomShape();
            var letters = new char[Piece.BlockCountOf(type)];
            for (int i = 0; i < letters.Length; i++)
            {
                letters[i] = RandomLetter();
            }

            bool hinted = pendingRest.Length > 0 || random.NextDouble() < DifficultyRules.HintChance(difficulty);
            if (hinted && TryWriteHint(type, letters, out int[] hints))
            {
                // Easy shows the word ready to read; Medium makes the player turn it.
                int rotation = difficulty == Difficulty.Easy ? 0 : random.Next(4);
                return new Piece(type, letters, rotation, hints);
            }

            int wordLength = WordLengthFor(type);
            string word = wordLength > 0 && random.NextDouble() < wordPieceChance
                ? dictionary.RandomWord(wordLength, random)
                : null;
            if (word == null)
            {
                return new Piece(type, letters);
            }

            for (int i = 0; i < word.Length; i++)
            {
                letters[i] = word[i];
            }

            // Rotation 0 reads the word left→right and rotation 1 top→bottom, so
            // start at 2 or 3: one to three taps away from a readable word.
            return new Piece(type, letters, rotation: 2 + random.Next(2));
        }

        /// <summary>
        /// The Shuffle booster: new letters for the piece, keeping its shape. Its
        /// row gets a whole word when it fits, in a hint color on Easy/Medium.
        /// </summary>
        public Piece Reroll(Piece piece)
        {
            var letters = new char[piece.BlockCount];
            for (int i = 0; i < letters.Length; i++)
            {
                letters[i] = RandomLetter();
            }

            var hints = new int[letters.Length];
            int[] row = Piece.RowBlocksOf(piece.Type);
            string word = row.Length >= WordDictionary.MinLength ? dictionary.RandomWord(row.Length, random) : null;
            if (word != null)
            {
                int color = difficulty == Difficulty.Advanced ? Piece.NoHint : NextColor();
                for (int i = 0; i < row.Length; i++)
                {
                    letters[row[i]] = word[i];
                    hints[row[i]] = color;
                }
            }

            return new Piece(piece.Type, letters, word != null ? 0 : piece.Rotation, hints)
            {
                X = piece.X,
                Y = piece.Y,
            };
        }

        /// <summary>A random letter, weighted by Portuguese frequency (accents included).</summary>
        public char RandomLetter()
        {
            int roll = random.Next(TotalLetterWeight);
            foreach (var (letter, weight) in LetterWeights)
            {
                if (roll < weight)
                {
                    return letter;
                }

                roll -= weight;
            }

            return 'A';
        }

        /// <summary>Colors a curated piece's word when its long row spells one and hints are on.</summary>
        private Piece CuratedPiece(Piece piece)
        {
            int length = WordLengthFor(piece.Type);
            if (length == 0 || DifficultyRules.HintChance(difficulty) <= 0f ||
                !dictionary.Contains(new string(piece.Letters, 0, length)))
            {
                return piece;
            }

            int color = NextColor();
            for (int i = 0; i < length; i++)
            {
                piece.Hints[i] = color;
            }

            return piece;
        }

        /// <summary>
        /// Writes the next letters of the current hint word on the piece's long
        /// row (left to right), starting a new word when none is pending.
        /// </summary>
        private bool TryWriteHint(PieceType type, char[] letters, out int[] hints)
        {
            hints = new int[letters.Length];
            int[] row = Piece.RowBlocksOf(type);

            string segment;
            if (pendingRest.Length > 0)
            {
                int take = Math.Min(row.Length, pendingRest.Length);
                segment = pendingRest.Substring(0, take);
                pendingRest = pendingRest.Substring(take);
            }
            else if (!StartHintWord(row.Length, out segment))
            {
                return false;
            }

            for (int i = 0; i < segment.Length; i++)
            {
                letters[row[i]] = segment[i];
                hints[row[i]] = pendingColor;
            }

            return true;
        }

        /// <summary>
        /// Picks a hint word and returns the part for this piece. A word that fits
        /// the row may go whole on this piece; otherwise it is split, leaving at
        /// least one letter for the next piece(s).
        /// </summary>
        private bool StartHintWord(int rowLength, out string firstSegment)
        {
            firstSegment = null;
            int length = WordDictionary.MinLength + random.Next(MaxHintWordLength - WordDictionary.MinLength + 1);
            string word = null;
            for (int l = length; l >= WordDictionary.MinLength && word == null; l--)
            {
                word = dictionary.RandomWord(l, random);
            }

            if (word == null)
            {
                return false;
            }

            int firstPart = word.Length <= rowLength && random.Next(2) == 0
                ? word.Length
                : 1 + random.Next(Math.Min(rowLength, word.Length - 1));

            pendingColor = NextColor();
            firstSegment = word.Substring(0, firstPart);
            pendingRest = word.Substring(firstPart);
            return true;
        }

        private int NextColor()
        {
            lastColor = lastColor % HintColorCount + 1;
            return lastColor;
        }

        private PieceType RandomShape()
        {
            int roll = random.Next(totalShapeWeight);
            foreach (PieceWeight entry in shapeWeights)
            {
                if (roll < entry.weight)
                {
                    return entry.type;
                }

                roll -= Math.Max(0, entry.weight);
            }

            return shapeWeights[0].type;
        }
    }
}
