using System;
using System.Collections.Generic;
using UnityEngine;
using WordDrop.Core;

namespace WordDrop.Gameplay
{
    public enum ObjectiveType
    {
        /// <summary>Reach a score.</summary>
        Score,
        /// <summary>Form a number of words.</summary>
        Words,
        /// <summary>Clear a number of full rows.</summary>
        Rows,
        /// <summary>Trigger a number of chain reactions (combos).</summary>
        Chains,
    }

    [Serializable]
    public struct LevelObjective
    {
        public ObjectiveType type;
        public int target;

        public LevelObjective(ObjectiveType type, int target)
        {
            this.type = type;
            this.target = target;
        }
    }

    /// <summary>Counts what the player has done in a run and checks the level's objectives.</summary>
    public class ObjectiveTracker
    {
        private readonly IReadOnlyList<LevelObjective> objectives;

        public ObjectiveTracker(IReadOnlyList<LevelObjective> objectives)
        {
            this.objectives = objectives ?? Array.Empty<LevelObjective>();
        }

        public IReadOnlyList<LevelObjective> Objectives => objectives;
        public int Score { get; private set; }
        public int Words { get; private set; }
        public int Rows { get; private set; }
        public int Chains { get; private set; }

        public void AddScore(int points) => Score += points;

        /// <summary>Counts words, rows and chains of a placement (points are added separately, as they are shown).</summary>
        public void Record(PlacementResult result)
        {
            Words += result.WordCount;
            Rows += result.RowCount;
            Chains += result.Chains;
        }

        public int Progress(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.Score: return Score;
                case ObjectiveType.Words: return Words;
                case ObjectiveType.Rows: return Rows;
                default: return Chains;
            }
        }

        /// <summary>True when the level has objectives and all of them are met.</summary>
        public bool AllComplete
        {
            get
            {
                if (objectives.Count == 0)
                {
                    return false;
                }

                foreach (LevelObjective objective in objectives)
                {
                    if (Progress(objective.type) < objective.target)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Overall progress from 0 to 1 (average over the objectives).</summary>
        public float Fraction
        {
            get
            {
                if (objectives.Count == 0)
                {
                    return 0f;
                }

                float sum = 0f;
                foreach (LevelObjective objective in objectives)
                {
                    sum += Mathf.Clamp01((float)Progress(objective.type) / Mathf.Max(1, objective.target));
                }

                return sum / objectives.Count;
            }
        }

        /// <summary>A compact progress line for the HUD, e.g. "Palavras 2/3 · Linhas 0/1".</summary>
        public string ProgressLine()
        {
            var parts = new List<string>();
            foreach (LevelObjective objective in objectives)
            {
                int done = Mathf.Min(Progress(objective.type), objective.target);
                parts.Add($"{ObjectiveText.ShortName(objective.type)} {Strings.FormatNumber(done)}/{Strings.FormatNumber(objective.target)}");
            }

            return string.Join(" · ", parts);
        }
    }

    /// <summary>Player-facing descriptions of objectives (pt-BR).</summary>
    public static class ObjectiveText
    {
        public static string Describe(LevelObjective objective)
        {
            int n = objective.target;
            string count = Strings.FormatNumber(n);
            switch (objective.type)
            {
                case ObjectiveType.Score: return string.Format(Strings.ObjectiveScore, count);
                case ObjectiveType.Words: return string.Format(n == 1 ? Strings.ObjectiveWord : Strings.ObjectiveWords, count);
                case ObjectiveType.Rows: return string.Format(n == 1 ? Strings.ObjectiveRow : Strings.ObjectiveRows, count);
                default: return string.Format(n == 1 ? Strings.ObjectiveChain : Strings.ObjectiveChains, count);
            }
        }

        public static string DescribeAll(IReadOnlyList<LevelObjective> objectives)
        {
            var lines = new List<string>();
            foreach (LevelObjective objective in objectives)
            {
                lines.Add(Describe(objective));
            }

            return string.Join("\n", lines);
        }

        public static string ShortName(ObjectiveType type)
        {
            switch (type)
            {
                case ObjectiveType.Score: return Strings.Score;
                case ObjectiveType.Words: return Strings.WordsShort;
                case ObjectiveType.Rows: return Strings.RowsShort;
                default: return Strings.ChainsShort;
            }
        }
    }
}
