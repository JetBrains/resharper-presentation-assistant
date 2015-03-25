using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using EnvDTE;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.Util;
using JetBrains.Util.Lazy;
using JetBrains.Util.Logging;
using JetBrains.VsIntegration.Interop.Extension;
using JetBrains.VsIntegration.Shell;
using JetBrains.VsIntegration.Shell.ActionManagement;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class VsActionFinder : ActionFinder
    {
        private readonly IVsCmdNameMapping vsCmdNameMapping;
        private readonly VsShortcutFinder vsShortcutFinder;
        private readonly DTE dte;
        private readonly IDictionary<string, IActionDefWithId> cachedActionDefs;

        public VsActionFinder(Lifetime lifetime, IActionDefs defs, DTE dte, IVsCmdNameMapping vsCmdNameMapping,
                              VsShortcutFinder vsShortcutFinder, VsToolsOptionsMonitor vsToolsOptionsMonitor)
            : base(defs)
        {
            this.vsCmdNameMapping = vsCmdNameMapping;
            this.vsShortcutFinder = vsShortcutFinder;
            this.dte = dte;

            cachedActionDefs = new Dictionary<string, IActionDefWithId>();
            vsToolsOptionsMonitor.VsOptionsMightHaveChanged.Advise(lifetime, _ => cachedActionDefs.Clear());
        }

        public override IActionDefWithId Find(string actionId)
        {
            var def = base.Find(actionId);
            if (def != null)
                return def;

            var cached = GetCachedActionDefs();
            return cached.TryGetValue(actionId, out def) ? def : null;
        }

        private IDictionary<string, IActionDefWithId> GetCachedActionDefs()
        {
            if (cachedActionDefs.Any())
                return cachedActionDefs;

            PopulateCachedActionDefs();

            return cachedActionDefs;
        }

        private void PopulateCachedActionDefs()
        {
            var menuBar = Logger.CatchSilent(() => dte.CommandBars()["MenuBar"]);
            if (menuBar != null)
            {
                var compoundException = new CompoundException();

                var enumDescendantControls = menuBar.EnumDescendantControls(compoundException).ToList();
                foreach (var tuple in enumDescendantControls)
                {
                    // Make sure it's an actionable type (e.g. not a CommandBarPopup, or _CommandBarActiveX)
                    var commandBarControl = tuple.A;
                    if (commandBarControl is CommandBarButton || commandBarControl is CommandBarComboBox)
                    {
                        var commandId = VsCommandHelpersTodo.TryGetVsControlCommandID(commandBarControl, dte);
                        if (commandId == null)
                            continue;

                        // Breadth first enumeration of descendant controls means the first time a command is encountered
                        // is always the shortest path to a control for that command
                        var actionId = vsCmdNameMapping.TryMapCommandIdToVsCommandName(commandId);
                        if (string.IsNullOrEmpty(actionId) || cachedActionDefs.ContainsKey(actionId))
                            continue;

                        var commandBarPopups = tuple.B;
                        var def = new CommandBarActionDef(vsShortcutFinder, dte, actionId, commandId, commandBarControl,
                            commandBarPopups ?? EmptyArray<CommandBarPopup>.Instance);
                        cachedActionDefs.Add(actionId, def);
                    }
                }

                if (compoundException.Exceptions.Any())
                    Logger.LogException(compoundException);
            }
        }

        private class CommandBarActionDef : IActionDefWithId, IActionWithPath
        {
            private readonly Util.Lazy.Lazy<BackingFields> backingFields;

            private class BackingFields
            {
                public string Text;
                public string Path;
                public string[] VsShortcuts;
            }

            public CommandBarActionDef(VsShortcutFinder vsShortcutFinder, DTE dte, string actionId, CommandID commandId,
                                       CommandBarControl control, CommandBarPopup[] parentPopups)
            {
                ActionId = actionId;
                CommandId = commandId;

                // Lazily initialise. Talking to the command bar objects is SLOOOOOOOWWWWWW.
                backingFields = Lazy.Of(() =>
                {
                    if (control == null)
                        MessageBox.ShowInfo("actionId", "Control is null!!!");

                    var fields = new BackingFields
                    {
                        Text = control.Caption,
                        Path = string.Join(" \u2192 ", parentPopups.Select(p => p.Caption))
                    };

                    var command = VsCommandHelpers.TryGetVsCommandAutomationObject(commandId, dte);
                    var vsShortcut = vsShortcutFinder.GetVsShortcut(command);
                    if (vsShortcut != null && vsShortcut.KeyboardShortcuts != null &&
                        vsShortcut.KeyboardShortcuts.Length > 0)
                    {
                        fields.VsShortcuts = new[] {vsShortcut.KeyboardShortcuts[0].ToString()};
                    }

                    return fields;
                });
            }

            public bool IsInternal
            {
                get { return false; }
            }

            public PartCatalogueType Part
            {
                get { return null; }
            }

            public PartCatalogueType IconType
            {
                get { return null; }
            }

            public string ActionId { get; private set; }

            public string Text
            {
                get { return backingFields.Value.Text; }
                set { /* Do nothing */ }
            }

            public string Description
            {
                get { return null; }
            }

            public CommandID CommandId { get; private set; }

            public string[] VsShortcuts
            {
                get { return backingFields.Value.VsShortcuts; }
            }

            public string[] IdeaShortcuts
            {
                get { return null; }
            }

            public ShortcutScope ShortcutScope
            {
                get { return ShortcutScope.Global; }
            }

            public string Path
            {
                get { return backingFields.Value.Path; }
            }
        }
    }

    // We can't use ReSharper's VsCommandHelpers directly as it uses an embedded interop type
    // which isn't available across assemblies
    public static class VsCommandHelpers2
    {
        /// <summary>
        /// Does BFS on descendant controls and command bars of a command bar.
        /// Each returned item is the descandant control plus an array of its parents, top to bottom.
        /// </summary>
        [NotNull]
        public static IEnumerable<JetTuple<CommandBarControl, CommandBarPopup[]>> EnumDescendantControls(
            [NotNull] this CommandBar root, [NotNull] CompoundException cex)
        {
            if (root == null)
                throw new ArgumentNullException("root");
            if (cex == null)
                throw new ArgumentNullException("cex");

            var queueEnumChildren =
                new Queue<JetTuple<CommandBar, CommandBarPopup[]>>(new[]
                {JetTuple.Of(root, EmptyArray<CommandBarPopup>.Instance)});
            int nBar = -1;

            while (queueEnumChildren.Any())
            {
                JetTuple<CommandBar, CommandBarPopup[]> tuple = queueEnumChildren.Dequeue();

                nBar++;
                List<CommandBarControl> children = null;
                try
                {
                    // All children
                    children = tuple.A.Controls.OfType<CommandBarControl>().ToList();

                    // EnqueueJob child containers
                    children.OfType<CommandBarPopup>()
                        .Select(popup => JetTuple.Of(popup.CommandBar, tuple.B.Concat(popup).ToArray()))
                        .ForEach(queueEnumChildren.Enqueue);
                }
                catch (Exception e)
                {
                    var ex = new LoggerException("Failed to enum command bar child controls.", e);
                    ex.AddData("IndexInQueue", () => nBar);
                    ex.AddData("CommandBarName", () => tuple.A.Name);
                    ex.AddData("CommandBarIndexProperty", () => tuple.A.Index);
                    cex.Exceptions.Add(ex);
                }

                // Emit
                if (children != null) // Null if were exceptions (cannot yield from a catch)
                {
                    foreach (CommandBarControl child in children)
                        yield return JetTuple.Of(child, tuple.B);
                }
            }
        }
    }
}