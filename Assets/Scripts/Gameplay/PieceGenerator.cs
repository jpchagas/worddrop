using System;
using System.Collections.Generic;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// Deals the pieces. Shapes come from a "7-bag" (every bag holds each of the
    /// seven pieces once, in random order), like modern Tetris. Some I, T, L and
    /// J pieces carry a word on their long row (4 letters on I, 3 on the others)
    /// and spawn turned away from it, so the player rotates them to read it.
    /// Every other letter is random, weighted by how common it is in Portuguese.
    /// </summary>
    public class PieceGenerator
    {
        // Approximate Portuguese letter frequencies (per mil). Q, X, K, W and Y are
        // left out because they almost never help to form short words.
        private static readonly (char letter, int weight)[] LetterWeights =
        {
            ('A', 146), ('E', 126), ('O', 107), ('S', 78), ('R', 65), ('I', 62), ('N', 50),
            ('D', 50), ('M', 47), ('U', 46), ('T', 43), ('C', 39), ('L', 28), ('P', 25),
            ('V', 17), ('G', 13), ('H', 13), ('B', 10), ('F', 10), ('Z', 5), ('J', 4),
        };

        private static readonly int TotalWeight;

        private readonly WordDictionary dictionary;
        private readonly float wordPieceChance;
        private readonly Random random;
        private readonly List<PieceType> bag = new List<PieceType>();

        static PieceGenerator()
        {
            foreach (var (_, weight) in LetterWeights)
            {
                TotalWeight += weight;
            }
        }

        public PieceGenerator(WordDictionary dictionary, float wordPieceChance, Random random)
        {
            this.dictionary = dictionary;
            this.wordPieceChance = wordPieceChance;
            this.random = random;
        }

        /// <summary>The length of word a piece type can carry, or 0.</summary>
        public static int WordLengthFor(PieceType type)
        {
            switch (type)
            {
                case PieceType.I: return 4;
                case PieceType.T:
                case PieceType.L:
                case PieceType.J: return 3;
                default: return 0;
            }
        }

        public Piece Next()
        {
            PieceType type = NextType();
            var letters = new char[Piece.BlockCount];
            for (int i = 0; i < letters.Length; i++)
            {
                letters[i] = RandomLetter();
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

        private PieceType NextType()
        {
            if (bag.Count == 0)
            {
                bag.AddRange((PieceType[])Enum.GetValues(typeof(PieceType)));
                for (int i = bag.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }
            }

            PieceType next = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return next;
        }

        private char RandomLetter()
        {
            int roll = random.Next(TotalWeight);
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
    }
}
