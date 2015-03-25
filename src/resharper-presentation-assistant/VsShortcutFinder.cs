using System;
using System.Linq;
using EnvDTE;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Threading;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.VsIntegration.Shell.ActionManagement;
using JetBrains.VsIntegration.Shell.Actions.Revised;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class VsShortcutFinder
    {
        private readonly DTE dte;
        private readonly IThreading threading;
        private readonly IVsActionsDefs vsActionDefs;

        // TODO: Optional components for when VS isn't available
        public VsShortcutFinder(DTE optionalDte, IThreading threading, IVsActionsDefs vsActionDefs)
        {
            dte = optionalDte;
            this.threading = threading;
            this.vsActionDefs = vsActionDefs;
        }

        // This is the current key binding for the command being overridden by an action
        // By definition, this is the VS key binding (because we're overriding an existing
        // VS command and using its key bindings)
        public ActionShortcut GetOverriddenVsShortcut(IActionDefWithId def)
        {
            if (dte == null)
                return null;

            // Not sure if we're ever called on a non-UI thread, but better safe than sorry
            if (!threading.Dispatcher.CheckAccess())
                return null;

            // def.CommandId is the command ID of the ReSharper action. We want the command ID
            // of the VS command it's overriding
            var commandId = vsActionDefs.TryGetOverriddenCommandIds(def).FirstOrDefault();
            if (commandId == null)
                return null;

            var command = VsCommandHelpers.TryGetVsCommandAutomationObject(commandId, dte);
            if (command == null)
                return null;

            return GetVsShortcut(command);
        }

        public ActionShortcut GetVsShortcut(Command command)
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
            return ShortcutUtil.BindingsToShortcut(length > 0 ? binding.Substring(length + 2) : binding);
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