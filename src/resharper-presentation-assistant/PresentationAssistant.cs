using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.BuildScript.Application;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.ActionSystem;
using JetBrains.UI.PopupMenu.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent(Requirement = InstantiationRequirement.Instant)]
    public class PresentationAssistant
    {
        private readonly OutputPanelLogger logger;
        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly IActionShortcuts actionShortcuts;

        public PresentationAssistant(Lifetime lifetime, ActionEvents actionEvents, OutputPanelLogger logger, PresentationAssistantWindowOwner presentationAssistantWindowOwner, IActionShortcuts actionShortcuts, IActionDefs defs)
        {
            this.logger = logger;
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.actionShortcuts = actionShortcuts;
            actionEvents.AdviseExecuteAction(lifetime, OnAction);

            var ids = defs.GetAllActionDefs().Where(d => !string.IsNullOrEmpty(d.Description));
            foreach (var id in ids)
            {
                logger.Log(id.Text + " " + id.Description);
            }
        }

        private void OnAction(ActionEvents.ActionEventArgs obj)
        {
            // Would be nice to use IActionShortcuts here, but that only returns for a single scheme
            var vsShortcut = string.Empty;
            var intellijShortcut = string.Empty;

            // TODO: Remove trailing ellipsis
            var text = MnemonicStore.RemoveMnemonicMark(obj.ActionDef.Text);
            text = string.IsNullOrEmpty(text) ? obj.ActionDef.ActionId : text;

            if (obj.ActionDef.IdeaShortcuts != null)
            {
                // Remove near duplicate chords (e.g. Control+R R, Control+R Control+R
                // Replace "Control" with "Ctrl", or an icon of the key
                //foreach (var s in obj.ActionDef.IdeaShortcuts)
                //{
                //    var s2 = ShortcutUtil.ParseKeyboardShortcut(s);
                //    var shortcuts = s2.KeyboardShortcuts;
                //    if (shortcuts[0].Key == shortcuts[1].Key)
                //}
                //ActionShortcut keyboardShortcut = ShortcutUtil.ParseKeyboardShortcut(obj.ActionDef.IdeaShortcuts[0]);

                intellijShortcut = string.Join(", ", obj.ActionDef.IdeaShortcuts);
            }

            if (obj.ActionDef.VsShortcuts != null)
                vsShortcut = string.Join(", ", obj.ActionDef.VsShortcuts);

            var shortcut = new Shortcut
            {
                Text = text,
                Description = obj.ActionDef.Description,
                VsShortcut = vsShortcut,
                IntellijShortcut = intellijShortcut,
                CurrentScheme = actionShortcuts.CurrentScheme
            };

            presentationAssistantWindowOwner.Show(shortcut);
        }
    }

    public class Shortcut
    {
        public string Text { get; set; }
        public string Description { get; set; }
        public string VsShortcut { get; set; }
        public string IntellijShortcut { get; set; }
        public ShortcutScheme CurrentScheme { get; set; }
    }
}
