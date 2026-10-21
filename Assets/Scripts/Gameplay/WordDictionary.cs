using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WordDrop.Gameplay
{
    /// <summary>
    /// The valid words. Tiles carry plain letters, so words are matched without
    /// accents ("MAE" matches "MÃE"), but the accented form is kept for display.
    /// </summary>
    public class WordDictionary
    {
        public const int MinLength = 3;

        private readonly Dictionary<string, string> displayByKey = new Dictionary<string, string>();
        private readonly Dictionary<int, List<string>> keysByLength = new Dictionary<int, List<string>>();

        public WordDictionary(IEnumerable<string> words)
        {
            foreach (string raw in words)
            {
                string display = raw.Trim().ToUpperInvariant();
                if (display.Length == 0 || display.StartsWith("#"))
                {
                    continue;
                }

                string key = Normalize(display);
                if (key.Length < MinLength || displayByKey.ContainsKey(key))
                {
                    continue;
                }

                displayByKey.Add(key, display);
                if (!keysByLength.TryGetValue(key.Length, out List<string> list))
                {
                    keysByLength[key.Length] = list = new List<string>();
                }

                list.Add(key);
                MaxLength = Math.Max(MaxLength, key.Length);
            }
        }

        public int Count => displayByKey.Count;
        public int MaxLength { get; private set; }

        public static WordDictionary FromText(string text) =>
            new WordDictionary(text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));

        public bool Contains(string key) => displayByKey.ContainsKey(key);

        /// <summary>The accented form of a matched word, e.g. "MAE" → "MÃE".</summary>
        public string Display(string key) => displayByKey.TryGetValue(key, out string display) ? display : key;

        /// <summary>A random word of the given length (plain letters), or null if there is none.</summary>
        public string RandomWord(int length, Random random)
        {
            return keysByLength.TryGetValue(length, out List<string> list) ? list[random.Next(list.Count)] : null;
        }

        /// <summary>Upper case, with accents and the cedilla removed: "Maçã" → "MACA".</summary>
        public static string Normalize(string word)
        {
            string decomposed = word.ToUpperInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (char c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
