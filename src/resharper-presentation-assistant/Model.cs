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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShortcutSequence)obj);
        }

        protected bool Equals(ShortcutSequence other)
        {
            if (Details.Length != other.Details.Length)
                return false;
            for (int i = 0; i < Details.Length; i++)
            {
                if (Details[i] != other.Details[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return (Details != null ? Details.GetHashCode() : 0);
        }
    }

    public class ShortcutDetails
    {
        private readonly KeyboardModifiers modifiers;

        public ShortcutDetails(string key, KeyboardModifiers modifiers = KeyboardModifiers.None)
        {
            this.modifiers = modifiers;
            Key = key;
            HasAlt = (modifiers & KeyboardModifiers.Alt) != 0;
            HasControl = (modifiers & KeyboardModifiers.Control) != 0;
            HasShift = (modifiers & KeyboardModifiers.Shift) != 0;
        }

        public string Key { get; set; }
        public bool HasAlt { get; private set; }
        public bool HasControl { get; private set; }
        public bool HasShift { get; private set; }

        protected bool Equals(ShortcutDetails other)
        {
            return modifiers == other.modifiers && string.Equals(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShortcutDetails)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)modifiers * 397) ^ (Key != null ? Key.GetHashCode() : 0);
            }
        }
    }

    public class Shortcut
    {
        public string Text { get; set; }
        public string Description { get; set; } // Only used by 1 action in ReSharper!
        public int Multiplier { get; set; }
        public ShortcutSequence VsShortcut { get; set; }
        public ShortcutSequence IntellijShortcut { get; set; }
        public ShortcutScheme CurrentScheme { get; set; }

        public bool HasShortcuts
        {
            get { return VsShortcut != null; }
        }

        public bool HasIntellijShortcuts
        {
            get { return IntellijShortcut != null; }
        }

        public bool HasMultiplier
        {
            get { return Multiplier > 1; }
        }
    }
}