using System;
using System.Collections.Generic;
using System.Text;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The valid words. Following the GDD (1.3, 8.6), words are normalized to
    /// Unicode NFC and upper case, and accents and Ç are kept: "AVÓ" and "AVO"
    /// are different words, and a tile "Ã" is not an "A".
    /// The word file may declare metadata in lines starting with "#!", e.g.
    /// "#! versao: 1.0.0", and each word may have a short explanation after a
    /// bar: "MÃE | Mulher em relação aos seus filhos." (GDD 7.1, item 13).
    /// </summary>
    public class WordDictionary
    {
        public const int MinLength = 3;

        private readonly HashSet<string> words = new HashSet<string>();
        private readonly Dictionary<string, string> definitions = new Dictionary<string, string>();
        private readonly Dictionary<int, List<string>> wordsByLength = new Dictionary<int, List<string>>();

        public WordDictionary(IEnumerable<string> lines)
        {
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (line.StartsWith("#!"))
                {
                    ReadMetadata(line.Substring(2));
                    continue;
                }

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                string definition = null;
                int bar = line.IndexOf('|');
                if (bar >= 0)
                {
                    definition = line.Substring(bar + 1).Trim();
                    line = line.Substring(0, bar);
                }

                string word = Normalize(line);
                if (word.Length < MinLength || !words.Add(word))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(definition))
                {
                    definitions[word] = definition;
                }

                if (!wordsByLength.TryGetValue(word.Length, out List<string> list))
                {
                    wordsByLength[word.Length] = list = new List<string>();
                }

                list.Add(word);
                MaxLength = Math.Max(MaxLength, word.Length);
            }
        }

        public int Count => words.Count;
        public int MaxLength { get; private set; }

        /// <summary>Content version from the file's "#! versao:" line, saved with player data.</summary>
        public string Version { get; private set; } = "desconhecida";

        public static WordDictionary FromText(string text) =>
            new WordDictionary(text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

        public bool Contains(string word) => words.Contains(word);

        /// <summary>The word's short explanation, or null when the list has none.</summary>
        public string Definition(string word) => definitions.TryGetValue(word, out string text) ? text : null;

        /// <summary>
        /// The first word (in list order) matching a pattern where
        /// <paramref name="wildcard"/> stands for any letter, or null.
        /// </summary>
        public string Match(string pattern, char wildcard)
        {
            if (pattern.IndexOf(wildcard) < 0)
            {
                return Contains(pattern) ? pattern : null;
            }

            if (!wordsByLength.TryGetValue(pattern.Length, out List<string> list))
            {
                return null;
            }

            foreach (string word in list)
            {
                bool matches = true;
                for (int i = 0; i < pattern.Length && matches; i++)
                {
                    matches = pattern[i] == wildcard || pattern[i] == word[i];
                }

                if (matches)
                {
                    return word;
                }
            }

            return null;
        }

        /// <summary>A random word of the given length, or null if there is none.</summary>
        public string RandomWord(int length, Random random)
        {
            return wordsByLength.TryGetValue(length, out List<string> list) ? list[random.Next(list.Count)] : null;
        }

        /// <summary>NFC + upper case, keeping accents: "maçã" → "MAÇÃ".</summary>
        public static string Normalize(string word) =>
            word.Trim().Normalize(NormalizationForm.FormC).ToUpperInvariant().Normalize(NormalizationForm.FormC);

        private void ReadMetadata(string line)
        {
            int colon = line.IndexOf(':');
            if (colon < 0)
            {
                return;
            }

            string key = line.Substring(0, colon).Trim().ToLowerInvariant();
            if (key == "versao" || key == "versão" || key == "version")
            {
                Version = line.Substring(colon + 1).Trim();
            }
        }
    }
}
