using System;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;

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

            // TODO: Remove trailing ellipsis
            var text = MnemonicStore.RemoveMnemonicMark(obj.ActionDef.Text);
            text = string.IsNullOrEmpty(text) ? obj.ActionDef.ActionId : text;

            var now = DateTime.UtcNow;
            if (obj.ActionDef.ActionId != lastActionId)
                multiplier = 1;
            else if (now - lastDisplayed < MultiplierTimeout)
                multiplier++;

            var shortcut = new Shortcut
            {
                Text = text,
                Description = obj.ActionDef.Description,
                VsShortcut = GetPrimaryShortcut(obj.ActionDef.VsShortcuts),
                IntellijShortcut = GetPrimaryShortcut(obj.ActionDef.IdeaShortcuts),
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = multiplier
            };

            presentationAssistantWindowOwner.Show(shortcut);

            lastDisplayed = now;
            lastActionId = obj.ActionDef.ActionId;
        }

        private ShortcutSequence GetPrimaryShortcut(string[] shortcuts)
        {
            if (shortcuts == null || shortcuts.Length == 0)
                return null;

            // ReSharper registers chords twice, once where the second char doesn't have modifiers
            // and once where the second char repeats the modifier of the first char.
            // E.g. Ctrl+R, R and Ctrl+R, Ctrl+R. This allows for flexibility in hitting that chord
            // (do you hold Ctrl down all the time, or just for the first char? Doesn't matter!)
            // Empirically, there are only two actions that have a genuine alternative shortcut -
            // SafeDelete (VS): Ctrl+R, D and Alt+Delete
            // Rename (IntelliJ): F2 and Shift+F6
            // These can be safely ignored, meaning we can just show the primary shortcut
            var shortcut = shortcuts[0];
            var parsedShortcut = ShortcutUtil.ParseKeyboardShortcut(shortcut);
            if (parsedShortcut == null)
                return null;

            var details = new ShortcutDetails[parsedShortcut.KeyboardShortcuts.Length];
            for (var i = 0; i < parsedShortcut.KeyboardShortcuts.Length; i++)
            {
                var keyboardShortcut = parsedShortcut.KeyboardShortcuts[i];
                details[i] = new ShortcutDetails(KeyConverter.Convert(keyboardShortcut.Key),
                    keyboardShortcut.Modifiers);
            }
            return new ShortcutSequence(details);
        }
    }

    public class ShortcutSequence
    {
        public ShortcutSequence(params ShortcutDetails[] details)
        {
            Details = details;
        }

        public ShortcutDetails[] Details { get; private set; }
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
        public bool HasAlt { get; set; }
        public bool HasControl { get; set; }
        public bool HasShift { get; set; }
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
