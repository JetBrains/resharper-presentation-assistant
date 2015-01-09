using System;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent(Requirement = InstantiationRequirement.Instant)]
    public class PresentationAssistant
    {
        private static readonly TimeSpan MultiplierTimeout = TimeSpan.FromSeconds(10);

        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly IActionShortcuts actionShortcuts;

        private DateTime lastDisplayed;
        private string lastActionId;
        private int multiplier;

        public PresentationAssistant(Lifetime lifetime, ActionEvents actionEvents,
            PresentationAssistantWindowOwner presentationAssistantWindowOwner, IActionShortcuts actionShortcuts, IActionDefs defs)
        {
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.actionShortcuts = actionShortcuts;
            actionEvents.AdviseExecuteAction(lifetime, OnAction);
        }

        private void OnAction(ActionEvents.ActionEventArgs obj)
        {
            // Would be nice to use IActionShortcuts here, but that only returns for a single scheme

            if (ActionIdBlacklist.IsBlacklisted(obj.ActionDef.ActionId))
                return;

            // TODO: Remove trailing ellipsis
            var text = MnemonicStore.RemoveMnemonicMark(obj.ActionDef.Text);
            text = string.IsNullOrEmpty(text) ? obj.ActionDef.ActionId : text;

            var now = DateTime.UtcNow;
            if (obj.ActionDef.ActionId != lastActionId)
                multiplier = 1;
            else if (now - lastDisplayed < MultiplierTimeout)
                multiplier++;

            var vsShortcut = GetPrimaryShortcutSequence(obj.ActionDef.VsShortcuts);
            ShortcutSequence intellijShortcut = null;
            // TODO: Make this a setting? Only show secondary scheme if different
            if (!HasSamePrimaryShortcuts(obj.ActionDef))
                intellijShortcut = GetPrimaryShortcutSequence(obj.ActionDef.IdeaShortcuts);

            if (intellijShortcut == null && vsShortcut == null)
                vsShortcut = GetWellKnownShortcutSequence(obj.ActionDef, out intellijShortcut);

            var shortcut = new Shortcut
            {
                ActionId = obj.ActionDef.ActionId,
                Text = text,
                Description = obj.ActionDef.Description,
                VsShortcut = vsShortcut,
                IntellijShortcut = intellijShortcut,
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = multiplier
            };

            presentationAssistantWindowOwner.Show(shortcut);

            lastDisplayed = now;
            lastActionId = obj.ActionDef.ActionId;
        }

        private static bool HasSamePrimaryShortcuts(IActionDefWithId actionDef)
        {
            var vsShortcuts = actionDef.VsShortcuts ?? EmptyArray<string>.Instance;
            var ideaShortcuts = actionDef.IdeaShortcuts ?? EmptyArray<string>.Instance;
            if (vsShortcuts.Length > 0 && ideaShortcuts.Length > 0)
                return vsShortcuts[0] == ideaShortcuts[0];

            return false;
        }

        private static ShortcutSequence GetWellKnownShortcutSequence(IActionDefWithId actionDef, out ShortcutSequence intellijShortcut)
        {
            intellijShortcut = null;

            // Some actions don't have defined shortcuts, as they're wired into VS below
            // the level of keyboard commands (e.g. overriding intellisense's ctrl+space)
            switch (actionDef.ActionId)
            {
                case "CompleteCodeBasic":
                    return GetShortcutSequence("Control+Space");

                case "Escape":
                    return GetShortcutSequence("Escape");

                // Standard VS commands that ReSharper overrides
                // TODO: Look these up in VS? Via CommandID
                // Some define their own keyboard shortcuts, though, e.g. ParameterInfo.Show
                case "WordDeleteToStart":
                    return GetShortcutSequence("Control+Backspace");

                case "WordDeleteToEnd":
                    return GetShortcutSequence("Control+Delete");

                    // Only overridden in IntelliJ scheme, and we don't show IntelliJ if there's no VS shortcut
                case "ParameterInfo.Show":
                    intellijShortcut = GetShortcutSequence("Control+P");
                    return GetShortcutSequence("Control+Shift+Space");

                case "Bookmarks.ClearAll":
                    return GetShortcutSequence("Control+K Control+L");
            }
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

            var details = new ShortcutDetails[parsedShortcut.KeyboardShortcuts.Length];
            for (int i = 0; i < parsedShortcut.KeyboardShortcuts.Length; i++)
            {
                var keyboardShortcut = parsedShortcut.KeyboardShortcuts[i];
                details[i] = new ShortcutDetails(KeyConverter.Convert(keyboardShortcut.Key),
                    keyboardShortcut.Modifiers);
            }
            return new ShortcutSequence(details);
        }
    }
}
