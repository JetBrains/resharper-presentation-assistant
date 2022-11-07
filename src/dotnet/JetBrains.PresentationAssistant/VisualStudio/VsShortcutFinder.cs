using System;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Shortcuts;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Actions.Automations;
using JetBrains.Application.UI.ActionsRevised.Loader;
using JetBrains.Application.UI.ActionsRevised.Shortcuts;
using JetBrains.VsIntegration.Shell.ActionManagement;
using JetBrains.VsIntegration.Shell.Actions.Revised;
using JetBrains.VsIntegration.Shell.EnvDte;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant.VisualStudio
{
    [ShellComponent]
    public class VsShortcutFinder : OverriddenShortcutFinder
    {
        private readonly IEnvDteWrapper dte;
        private readonly IThreading threading;
        private readonly IVsOverridingActionDefs vsActionDefs;
        private readonly VsKeyBindingsCache keyBindingsCache;

        public VsShortcutFinder(IEnvDteWrapper optionalDte, IThreading threading, IVsOverridingActionDefs vsActionDefs, VsKeyBindingsCache keyBindingsCache)
        {
            dte = optionalDte;
            this.threading = threading;
            this.vsActionDefs = vsActionDefs;
            this.keyBindingsCache = keyBindingsCache;
        }

        // This is the current key binding for the command being overridden by an action
        // By definition, this is the VS key binding (because we're overriding an existing
        // VS command and using its key bindings)
        public override ActionShortcut GetOverriddenVsShortcut(IActionDefWithId def)
        {
            if (dte == null)
                return null;

            // Not sure if we're ever called on a non-UI thread, but better safe than sorry
            if (!threading.Dispatcher.CheckAccess())
                return null;

            // def.CommandId is the command ID of the ReSharper action. We want the command ID
            // of the VS command it's overriding
            var vsOverridingDef = vsActionDefs.GetOverriddenVsCommands(def).FirstOrDefault();
            if (vsOverridingDef == null)
                return null;

            var binding = keyBindingsCache.GetKeyBindings(vsOverridingDef.OverriddenCommandId).FirstOrDefault();
            return binding?.Shortcut;
        }

        public ActionShortcut GetVsShortcut(IEnvDteCommand command)
        {
            var binding = GetFirstBinding(command.Bindings);
            if (binding != null)
            {
                // Can use ShortcutUtil.BindingsToShortcut
                // But would need to globalize the string?
                return ToActionShortcut(binding);
            }
            return null;
        }

        private static ActionShortcut ToActionShortcut(string binding)
        {
            var length = binding.IndexOf("::", StringComparison.Ordinal);
            return ShortcutUtil.BindingsToShortcut(length > 0 ? binding.Substring(length + 2) : binding, onerror: null);
        }

        private string GetFirstBinding(object o)
        {
            var s = o as object[];
            if (s != null && s.Length > 0)
                return s[0] as string;

            return null;
        }
    }
}
