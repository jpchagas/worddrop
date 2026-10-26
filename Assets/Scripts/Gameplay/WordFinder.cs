using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WordDrop.Gameplay
{
    public class FoundWord
    {
        public FoundWord(string word, List<Vector2Int> cells)
        {
            Word = word;
            Cells = cells;
        }

        /// <summary>The dictionary word (NFC, upper case, with accents); a wildcard tile shows as the letter it stood for.</summary>
        public string Word { get; }
        public List<Vector2Int> Cells { get; }
    }

    /// <summary>
    /// Finds dictionary words on the board, read left→right in rows and
    /// top→bottom in columns. Within one line, longer words win and words
    /// don't overlap; a cell can still be shared by a row word and a column word.
    /// </summary>
    public static class WordFinder
    {
        public static List<FoundWord> Find(Board board, WordDictionary dictionary)
        {
            var found = new List<FoundWord>();

            for (int y = 0; y < board.Height; y++)
            {
                var line = new List<Vector2Int>();
                for (int x = 0; x < board.Width; x++)
                {
                    line.Add(new Vector2Int(x, y));
                }

                FindInLine(board, dictionary, line, found);
            }

            for (int x = 0; x < board.Width; x++)
            {
                var line = new List<Vector2Int>();
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    line.Add(new Vector2Int(x, y));
                }

                FindInLine(board, dictionary, line, found);
            }

            return found;
        }

        /// <summary>Splits a line into runs of consecutive letters and searches each run.</summary>
        private static void FindInLine(Board board, WordDictionary dictionary, List<Vector2Int> line, List<FoundWord> found)
        {
            var run = new List<Vector2Int>();
            foreach (Vector2Int cell in line)
            {
                if (board[cell] != Board.Empty)
                {
                    run.Add(cell);
                    continue;
                }

                FindInRun(board, dictionary, run, found);
                run.Clear();
            }

            FindInRun(board, dictionary, run, found);
        }

        private static void FindInRun(Board board, WordDictionary dictionary, List<Vector2Int> run, List<FoundWord> found)
        {
            if (run.Count < WordDictionary.MinLength)
            {
                return;
            }

            var letters = new StringBuilder(run.Count);
            foreach (Vector2Int cell in run)
            {
                letters.Append(board[cell]);
            }

            string text = letters.ToString();
            var used = new bool[run.Count];

            for (int length = Mathf.Min(run.Count, dictionary.MaxLength); length >= WordDictionary.MinLength; length--)
            {
                for (int start = 0; start + length <= run.Count; start++)
                {
                    if (IsAnyUsed(used, start, length))
                    {
                        continue;
                    }

                    // A Star booster tile (wildcard) stands for any letter.
                    string word = dictionary.Match(text.Substring(start, length), Piece.Wildcard);
                    if (word == null)
                    {
                        continue;
                    }

                    for (int i = start; i < start + length; i++)
                    {
                        used[i] = true;
                    }

                    found.Add(new FoundWord(word, run.GetRange(start, length)));
                }
            }
        }

        private static bool IsAnyUsed(bool[] used, int start, int length)
        {
            for (int i = start; i < start + length; i++)
            {
                if (used[i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
