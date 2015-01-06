using System.Linq;
using System.Windows.Forms;
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
        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly IActionShortcuts actionShortcuts;

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

            // TODO: Only report the first shortcut
            // ReSharper registers chords twice, once where the second char doesn't have modifiers
            // and once where the second char repeats the modifier of the first char.
            // E.g. Ctrl+R, R and Ctrl+R, Ctrl+R. This allows for flexibility in hitting that chord
            // (do you hold Ctrl down all the time, or just for the first char? Doesn't matter!)
            // Empirically, there are only two actions that have a genuine alternative shortcut -
            // SafeDelete (VS): Ctrl+R, D and Alt+Delete
            // Rename (IntelliJ): F2 and Shift+F6
            // These can be safely ignored, meaning we can just show the primary shortcut
            var shortcut = new Shortcut
            {
                Text = text,
                Description = obj.ActionDef.Description,
                VsShortcuts = GetShortcuts(obj.ActionDef.VsShortcuts),
                IntellijShortcuts = GetShortcuts(obj.ActionDef.IdeaShortcuts),
                CurrentScheme = actionShortcuts.CurrentScheme
            };

            presentationAssistantWindowOwner.Show(shortcut);
        }

        // TODO: This is getting expensive in terms of allocations...
        private ShortcutSequence[] GetShortcuts(string[] shortcuts)
        {
            // TODO: Remove near-duplicates (e.g. Control+R R, Control+R Control+R)
            var parsedShortcuts = from s in shortcuts ?? EmptyArray<string>.Instance
                let parsed = ShortcutUtil.ParseKeyboardShortcut(s)
                let details = from k in parsed.KeyboardShortcuts
                    select new ShortcutDetails(GetKey(k.Key), k.Modifiers)
                select new ShortcutSequence(details.ToArray());
            return parsedShortcuts.ToArray();
        }

        private string GetKey(Keys key)
        {
            var converter = new KeysConverter();
            return converter.ConvertToString(key);
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
        public ShortcutSequence[] VsShortcuts { get; set; }
        public ShortcutSequence[] IntellijShortcuts { get; set; }
        public ShortcutScheme CurrentScheme { get; set; }

        public bool HasShortcuts
        {
            get { return VsShortcuts.Length > 0; }
        }

        public bool HasIntellijShortcuts
        {
            get { return IntellijShortcuts.Length > 0; }
        }
    }
}
