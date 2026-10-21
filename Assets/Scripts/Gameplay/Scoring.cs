namespace WordDrop.Gameplay
{
    public static class Scoring
    {
        /// <summary>
        /// 3 letters = 60, 4 = 120, 5 = 200, 6 = 300, multiplied by the combo
        /// step (1 for the first clear, 2 when the falling letters form another word...).
        /// </summary>
        public static int PointsFor(int wordLength, int combo) => 10 * wordLength * (wordLength - 1) * combo;

        private static readonly int[] RowPoints = { 0, 100, 300, 500, 800 };

        /// <summary>
        /// Full rows cleared at once, as in Tetris: 1 = 100, 2 = 300, 3 = 500,
        /// 4 = 800 (+200 for each row beyond 4), multiplied by the combo step.
        /// </summary>
        public static int PointsForRows(int rows, int combo)
        {
            int points = rows < RowPoints.Length ? RowPoints[rows] : RowPoints[RowPoints.Length - 1] + 200 * (rows - 4);
            return points * combo;
        }
    }
}
