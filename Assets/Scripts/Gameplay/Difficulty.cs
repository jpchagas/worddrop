namespace WordDrop.Gameplay
{
    public enum Difficulty { Easy, Medium, Advanced }

    /// <summary>
    /// What each difficulty changes:
    /// - Pieces: Easy deals I and O plus the small pieces; Medium adds T, L and J;
    ///   Advanced deals all 7 tetrominoes (no small pieces).
    /// - Color hints: share of pieces whose tiles are colored to show which
    ///   letters form a word together: every piece on Easy, half on Medium, none on Advanced.
    /// </summary>
    public static class DifficultyRules
    {
        public static float HintChance(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 1f;
                case Difficulty.Medium: return 0.5f;
                default: return 0f;
            }
        }

        public static PieceWeight[] Weights(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return new[]
                    {
                        W(PieceType.I, 10), W(PieceType.O, 10),
                        W(PieceType.I2, 4), W(PieceType.I3, 8), W(PieceType.V3, 5),
                    };
                case Difficulty.Medium:
                    return new[]
                    {
                        W(PieceType.I, 10), W(PieceType.O, 10), W(PieceType.T, 10), W(PieceType.L, 10), W(PieceType.J, 10),
                        W(PieceType.I2, 2), W(PieceType.I3, 5), W(PieceType.V3, 3),
                    };
                default:
                    return new[]
                    {
                        W(PieceType.I, 10), W(PieceType.O, 10), W(PieceType.T, 10), W(PieceType.S, 10),
                        W(PieceType.Z, 10), W(PieceType.L, 10), W(PieceType.J, 10),
                    };
            }
        }

        private static PieceWeight W(PieceType type, int weight) => new PieceWeight(type, weight);
    }
}
