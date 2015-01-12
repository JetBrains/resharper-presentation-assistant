using System.Linq;
using JetBrains.ActionManagement;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class ShortcutSequence
    {
        public ShortcutSequence(params ShortcutDetails[] details)
        {
            Details = details;
        }

        public ShortcutDetails[] Details { get; private set; }

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
        public bool HasAlt { get; private set; }
        public bool HasControl { get; private set; }
        public bool HasShift { get; private set; }

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
        public string Description { get; set; } // Only used by 1 action in ReSharper!
        public int Multiplier { get; set; }
        public ShortcutSequence VsShortcut { get; set; }
        public ShortcutSequence IntellijShortcut { get; set; }
        public ShortcutScheme CurrentScheme { get; set; }

        public bool HasVsShortcuts
        {
            get { return VsShortcut != null; }
        }

        public bool HasIntellijShortcuts
        {
            get { return IntellijShortcut != null; }
        }

        public bool HasShortcuts
        {
            // VS shortcuts are the primary. If we don't have them, don't show anything
            get { return HasVsShortcuts; }
        }

        public bool HasMultiplier
        {
            get { return Multiplier > 1; }
        }
    }
}