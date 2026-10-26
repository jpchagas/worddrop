using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordDrop.Gameplay
{
    /// <summary>How letters fall after a clear (GDD 12.2; a per-level setting so both can be playtested).</summary>
    public enum GravityMode
    {
        /// <summary>Every unsupported letter falls on its own. Creates more cascades.</summary>
        Independent,
        /// <summary>Only letters above cleared cells fall; other overhangs stay, closer to Tetris.</summary>
        AboveClearedOnly,
    }

    /// <summary>One pass of the resolution loop: what was cleared, how letters fell, and the points.</summary>
    public class ResolutionPass
    {
        public List<FoundWord> Words = new List<FoundWord>();
        public List<int> Rows = new List<int>();
        public List<Vector2Int> ClearedCells = new List<Vector2Int>();
        public List<(Vector2Int from, Vector2Int to)> Moves = new List<(Vector2Int, Vector2Int)>();
        /// <summary>0 for the first clear of the placement, 1 for the first chain, and so on; -1 if nothing cleared.</summary>
        public int ChainIndex = -1;
        public int Points;

        public bool HasClear => ClearedCells.Count > 0;
    }

    /// <summary>Everything that happened after one piece locked.</summary>
    public class PlacementResult
    {
        public List<ResolutionPass> Passes = new List<ResolutionPass>();
        public int WordCount;
        public int RowCount;
        /// <summary>Clearing passes after the first one (each is a chain reaction).</summary>
        public int Chains;
        public int MultiwordBonus;
        public int TotalPoints;
        public bool HitPassLimit;
    }

    /// <summary>
    /// The deterministic resolution sequence from GDD 2.1, run after a piece locks:
    /// find words → clear them (shared tiles once) → clear full rows → gravity →
    /// repeat until nothing changes. Pure logic; the view replays the passes.
    /// </summary>
    public static class Resolver
    {
        /// <summary>Defensive cap (GDD 2.2); never reached in normal play.</summary>
        public const int MaxPasses = 20;

        public static PlacementResult Resolve(Board board, WordDictionary dictionary, GravityMode gravity)
        {
            var result = new PlacementResult();
            int chainIndex = 0;

            for (int i = 0; i < MaxPasses; i++)
            {
                var pass = new ResolutionPass();

                pass.Words = WordFinder.Find(board, dictionary);
                var wordCells = pass.Words.SelectMany(w => w.Cells).Distinct().ToList();
                board.Clear(wordCells);

                // Full rows are checked after the words are gone and before gravity.
                pass.Rows = board.FullRows();
                pass.ClearedCells = wordCells.Union(board.CellsInRows(pass.Rows)).ToList();
                board.Clear(pass.ClearedCells);

                pass.Moves = gravity == GravityMode.Independent
                    ? board.ApplyGravity()
                    : board.DropAbove(pass.ClearedCells);

                if (!pass.HasClear && pass.Moves.Count == 0)
                {
                    break; // Stable.
                }

                if (pass.HasClear)
                {
                    pass.ChainIndex = chainIndex++;
                    pass.Points = Scoring.PassPoints(pass.Words.Select(w => w.Word.Length), pass.Rows.Count, pass.ChainIndex);
                    result.WordCount += pass.Words.Count;
                    result.RowCount += pass.Rows.Count;
                    result.TotalPoints += pass.Points;
                }

                result.Passes.Add(pass);

                if (i == MaxPasses - 1)
                {
                    result.HitPassLimit = true;
                }
            }

            result.Chains = Mathf.Max(0, chainIndex - 1);
            result.MultiwordBonus = Scoring.MultiwordBonus(result.WordCount);
            result.TotalPoints += result.MultiwordBonus;
            return result;
        }
    }
}
