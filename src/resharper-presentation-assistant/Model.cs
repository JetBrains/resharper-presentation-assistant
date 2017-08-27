using System.Linq;
using JetBrains.Application.Shortcuts;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class ShortcutSequence
    {
        public ShortcutSequence(params ShortcutDetails[] details)
        {
            Details = details;
        }

        public ShortcutDetails[] Details { get; }

        public override string ToString()
        {
            if (Details == null)
                return "undefined";
            return string.Join(", ", Details.Select(d => d.ToString()));
        }
    }

    public class ShortcutDetails
    {
        public ShortcutDetails(string key, KeyboardModifiers modifiers = KeyboardModifiers.None)
        {
            Key = key;
            HasAlt = (modifiers & KeyboardModifiers.Alt) != 0;
            HasControl = (modifiers & KeyboardModifiers.Control) != 0;
            HasShift = (modifiers & KeyboardModifiers.Shift) != 0;
        }

        public string Key { get; set; }
        public bool HasAlt { get; }
        public bool HasControl { get; }
        public bool HasShift { get; }

        public override string ToString()
        {
            string value = string.Empty;
            if (HasControl) value += "Control+";
            if (HasShift) value += "Shift+";
            if (HasAlt) value += "Alt+";
            value += Key;
            return value;
        }
    }

    public class Shortcut
    {
        public string ActionId { get; set; }
        public string Text { get; set; }
        public string Path { get; set; }
        public int Multiplier { get; set; }
        public ShortcutSequence VsShortcut { get; set; }
        public ShortcutSequence IntellijShortcut { get; set; }
        public ShortcutScheme CurrentScheme { get; set; }

        public bool HasVsShortcuts => VsShortcut != null;
        public bool HasIntellijShortcuts => IntellijShortcut != null;
        // VS shortcuts are the primary. If we don't have them, don't show anything
        public bool HasShortcuts => HasVsShortcuts;
        public bool HasMultiplier => Multiplier > 1;
    }
}