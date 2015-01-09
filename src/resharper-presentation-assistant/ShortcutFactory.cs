using System;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ShortcutFactory
    {
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
            text = String.IsNullOrEmpty(text) ? def.ActionId : text;
            return text;
        }

        private void SetShortcuts(Shortcut shortcut, IActionDefWithId def)
        {
            var vsShortcut = GetPrimaryShortcutSequence(def.VsShortcuts);
            // TODO: Make this a setting? Only show secondary scheme if different?
            ShortcutSequence intellijShortcut = null;
            if (!HasSamePrimaryShortcuts(def))
                intellijShortcut = GetPrimaryShortcutSequence(def.IdeaShortcuts);

            // There's no primary shortcut, try and find it by asking Visual Studio for the
            // shortcut of the overridden VS command (if there is one). Find any associated
            // intelliJ shortcut
            if (vsShortcut == null)
                vsShortcut = GetWellKnownShortcutSequence(def, out intellijShortcut);

            shortcut.VsShortcut = vsShortcut;
            shortcut.IntellijShortcut = intellijShortcut;
        }

        private ShortcutSequence GetWellKnownShortcutSequence(IActionDefWithId actionDef,
                                                              out ShortcutSequence intellijShortcut)
        {
            intellijShortcut = null;

            var s = vsShortcutFinder.GetOverriddenShortcut(actionDef);
            if (s != null)
                return GetShortcutSequence(s);

            // The escape action doesn't override any VS command, but also doesn't have a shortcut associated
            if (actionDef.ActionId == "Escape")
                return GetShortcutSequence("Escape");

            return null;
        }

        private static ShortcutSequence GetPrimaryShortcutSequence(string[] shortcuts)
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
            var details = new ShortcutDetails[parsedShortcut.KeyboardShortcuts.Length];
            for (int i = 0; i < parsedShortcut.KeyboardShortcuts.Length; i++)
            {
                var keyboardShortcut = parsedShortcut.KeyboardShortcuts[i];
                details[i] = new ShortcutDetails(KeyConverter.Convert(keyboardShortcut.Key),
                    keyboardShortcut.Modifiers);
            }
            return new ShortcutSequence(details);
        }

        private static bool HasSamePrimaryShortcuts(IActionDefWithId actionDef)
        {
            var vsShortcuts = actionDef.VsShortcuts ?? EmptyArray<string>.Instance;
            var ideaShortcuts = actionDef.IdeaShortcuts ?? EmptyArray<string>.Instance;
            if (vsShortcuts.Length > 0 && ideaShortcuts.Length > 0)
                return vsShortcuts[0] == ideaShortcuts[0];

            return false;
        }

    }
}