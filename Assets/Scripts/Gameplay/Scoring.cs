using System.Collections.Generic;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// Points (GDD section 3), all in integer arithmetic so results are the
    /// same on every device. Order of operations, per resolution pass:
    ///   pass points = (Σ word points + full rows × row bonus) × chain multiplier
    /// and once per placement, after all passes, without multiplier:
    ///   + multiword bonus (2 words: +100, 3 or more: +250).
    /// </summary>
    public static class Scoring
    {
        public const int RowBonus = 150;

        // Chain multipliers in tenths: ×1.0, ×1.5, ×2.0, then ×2.5 at most.
        private static readonly int[] ChainMultiplierTenths = { 10, 15, 20, 25 };

        /// <summary>10 × L²: 3 letters = 90, 4 = 160, 5 = 250, 6 = 360...</summary>
        public static int WordPoints(int length) => 10 * length * length;

        /// <summary>Multiplier in tenths for a clearing pass (0 = first clear, 1 = first chain...).</summary>
        public static int MultiplierTenths(int chainIndex) =>
            ChainMultiplierTenths[System.Math.Min(chainIndex, ChainMultiplierTenths.Length - 1)];

        public static int PassPoints(IEnumerable<int> wordLengths, int rows, int chainIndex)
        {
            int basePoints = rows * RowBonus;
            foreach (int length in wordLengths)
            {
                basePoints += WordPoints(length);
            }

            return basePoints * MultiplierTenths(chainIndex) / 10;
        }

        public static int MultiwordBonus(int wordsInPlacement) =>
            wordsInPlacement >= 3 ? 250 : wordsInPlacement == 2 ? 100 : 0;
    }
}
