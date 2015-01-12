using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ShortcutFactory
    {
        private static readonly char[] TrimCharacters = {'.', '\u2026'};

        private readonly IActionShortcuts actionShortcuts;
        private readonly VsShortcutFinder vsShortcutFinder;

        public ShortcutFactory(IActionShortcuts actionShortcuts, VsShortcutFinder vsShortcutFinder)
        {
            this.actionShortcuts = actionShortcuts;
            this.vsShortcutFinder = vsShortcutFinder;
        }

        public Shortcut Create(IActionDefWithId def, int multiplier)
        {
            var shortcut = new Shortcut
            {
                ActionId = def.ActionId,
                Text = GetText(def),
                Description = def.Description,
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = multiplier
            };

            SetShortcuts(shortcut, def);
            return shortcut;
        }

        private static string GetText(IActionDefWithId def)
        {
            var text = MnemonicStore.RemoveMnemonicMark(def.Text);
            text = text.Trim(TrimCharacters);
            text = string.IsNullOrEmpty(text) ? def.ActionId : text;
            return text;
        }

        private void SetShortcuts(Shortcut shortcut, IActionDefWithId def)
        {
            // TODO: Should this be an option in the options dialog? Show secondary scheme if different?
            const bool showSecondarySchemeIfSame = false;

            SetGivenShortcuts(shortcut, def, showSecondarySchemeIfSame);
            SetVsOverriddenShortcuts(shortcut, def);
            SetWellKnownShortcuts(shortcut, def, showSecondarySchemeIfSame);
        }

        private void SetGivenShortcuts(Shortcut shortcut, IActionDefWithId def, bool showSecondarySchemeIfSame)
        {
            shortcut.VsShortcut = GetFirstShortcutSequence(def.VsShortcuts);
            shortcut.IntellijShortcut = GetFirstShortcutSequence(def.IdeaShortcuts);

            if (HasSameShortcuts(shortcut) && !showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = null;
        }

        private static bool HasSameShortcuts(Shortcut shortcut)
        {
            // We can't rely on the strings in IActionWithDefId as the modifiers can be in any order.
            // So we use the string version of the parsed shortcuts
            if (!shortcut.HasIntellijShortcuts && !shortcut.HasVsShortcuts)
                return true;

            if (shortcut.HasIntellijShortcuts != shortcut.HasVsShortcuts)
                return false;

            return shortcut.VsShortcut.ToString() == shortcut.IntellijShortcut.ToString();
        }

        private static ShortcutSequence GetFirstShortcutSequence(string[] shortcuts)
        {
            if (shortcuts == null || shortcuts.Length == 0)
                return null;

            return GetShortcutSequence(shortcuts[0]);
        }

        private static ShortcutSequence GetShortcutSequence(string shortcut)
        {
            // ReSharper registers chords twice, once where the second char doesn't have modifiers
            // and once where the second char repeats the modifier of the first char.
            // E.g. Ctrl+R, R and Ctrl+R, Ctrl+R. This allows for flexibility in hitting that chord
            // (do you hold Ctrl down all the time, or just for the first char? Doesn't matter!)
            // Empirically, there are only two actions that have a genuine alternative shortcut -
            // SafeDelete (VS): Ctrl+R, D and Alt+Delete
            // Rename (IntelliJ): F2 and Shift+F6
            // These can be safely ignored, meaning we can just show the primary shortcut
            var parsedShortcut = ShortcutUtil.ParseKeyboardShortcut(shortcut);
            if (parsedShortcut == null)
                return null;

            return GetShortcutSequence(parsedShortcut);
        }

        private static ShortcutSequence GetShortcutSequence(ActionShortcut parsedShortcut)
        {
            if (parsedShortcut == null)
                return null;

            var details = new ShortcutDetails[parsedShortcut.KeyboardShortcuts.Length];
            for (int i = 0; i < parsedShortcut.KeyboardShortcuts.Length; i++)
            {
                var keyboardShortcut = parsedShortcut.KeyboardShortcuts[i];
                details[i] = new ShortcutDetails(KeyConverter.Convert(keyboardShortcut.Key),
                    keyboardShortcut.Modifiers);
            }
            return new ShortcutSequence(details);
        }

        private void SetVsOverriddenShortcuts(Shortcut shortcut, IActionDefWithId def)
        {
            // If we don't have any VS shortcuts, look to see if the action is an override of a
            // VS command, and get the current key binding for that command
            if (!shortcut.HasVsShortcuts)
                shortcut.VsShortcut = GetShortcutSequence(vsShortcutFinder.GetOverriddenVsShortcut(def));

            // An action that overrides a VS command uses the VS keybinding, but it can also
            // specify an IntelliJ shortcut
            if (!shortcut.HasIntellijShortcuts)
                shortcut.IntellijShortcut = GetShortcutSequence(vsShortcutFinder.GetOverriddenIntellijShortcut(def));
        }

        private void SetWellKnownShortcuts(Shortcut shortcut, IActionDefWithId def,
            bool showSecondarySchemeIfSame)
        {
            // The Escape action doesn't have a bound shortcut, or a VS override
            if (def.ActionId == "Escape")
            {
                shortcut.VsShortcut = GetShortcutSequence("Escape");
                if (showSecondarySchemeIfSame)
                    shortcut.IntellijShortcut = shortcut.VsShortcut;
            }
        }
    }
}