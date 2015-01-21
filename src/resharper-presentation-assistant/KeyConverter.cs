using System.Collections.Generic;
using System.Windows.Forms;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public static class KeyConverter
    {
        private static readonly IDictionary<Keys, string> KnownKeys = new Dictionary<Keys, string>
        {
            {Keys.Back, "\u232B"},  // Not sure about this. Ugly in Segoe UI
            {Keys.Tab, "\u21E5"},
            {Keys.Return, "\u23CE"},
            {Keys.Escape, "\u241B"},
            {Keys.Space, "\u2423"}, // Not sure about this. Not terribly obvious
            {Keys.Left, "\u2190"},
            {Keys.Up, "\u2191"},
            {Keys.Right, "\u2192"},
            {Keys.Down, "\u2193"},
            {Keys.D0, "0"},
            {Keys.D1, "1"},
            {Keys.D2, "2"},
            {Keys.D3, "3"},
            {Keys.D4, "4"},
            {Keys.D5, "5"},
            {Keys.D6, "6"},
            {Keys.D7, "7"},
            {Keys.D8, "8"},
            {Keys.D9, "9"},
            {Keys.Next, Keys.PageDown.ToString()},
            {Keys.Prior, Keys.PageUp.ToString()},
            {Keys.OemSemicolon, ";"},
            {Keys.Oemplus, "+"},
            {Keys.Oemcomma, ","},
            {Keys.OemMinus, "-"},
            {Keys.OemPeriod, "."},
            {Keys.OemQuestion, "/"},    // Weird name, but it's right
            {Keys.Oemtilde, "`"},   // Same here
            {Keys.OemOpenBrackets, "["},
            {Keys.OemPipe, "|"},
            {Keys.OemCloseBrackets, "]"},
            {Keys.OemQuotes, "\""},
            {Keys.OemBackslash, "\\"}
        };

        public static string Convert(Keys key)
        {
            string s;
            if (KnownKeys.TryGetValue(key, out s))
                return s;
            return key.ToString();
        }
    }
}