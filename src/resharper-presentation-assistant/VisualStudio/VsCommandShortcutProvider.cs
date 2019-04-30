using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using EnvDTE;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.UI.ActionsRevised.Shortcuts;
using JetBrains.Application.UI.Controls.JetPopupMenu.Detail;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Util;
using JetBrains.Util.Logging;
using JetBrains.VsIntegration.Shell;
using JetBrains.VsIntegration.Shell.ActionManagement;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant.VisualStudio
{
    [ShellComponent]
    public class VsCommandShortcutProvider : IShortcutProvider
    {
        private readonly ShortcutDisplayStatistics statistics;
        private readonly IVsCmdNameMapping vsCmdNameMapping;
        private readonly VsShortcutFinder vsShortcutFinder;
        private readonly IActionShortcuts actionShortcuts;
        private readonly DTE dte;
        private readonly IDictionary<string, CommandBarActionDef> cachedActionDefs;

        public VsCommandShortcutProvider(Lifetime lifetime, ShortcutDisplayStatistics statistics, DTE dte,
                                         IVsCmdNameMapping vsCmdNameMapping,
                                         VsShortcutFinder vsShortcutFinder,
                                         VsToolsOptionsMonitor vsToolsOptionsMonitor,
                                         IActionShortcuts actionShortcuts)
        {
            this.statistics = statistics;
            this.vsCmdNameMapping = vsCmdNameMapping;
            this.vsShortcutFinder = vsShortcutFinder;
            this.actionShortcuts = actionShortcuts;
            this.dte = dte;

            cachedActionDefs = new Dictionary<string, CommandBarActionDef>();
            vsToolsOptionsMonitor.VsOptionsMightHaveChanged.Advise(lifetime, _ => cachedActionDefs.Clear());
        }

        public Shortcut GetShortcut(string actionId)
        {
            var def = Find(actionId);
            if (def == null)
                return null;

            statistics.OnAction(actionId);

            return new Shortcut
            {
                ActionId = def.ActionId,
                Text = def.Text,
                Path = def.Path,
                CurrentScheme = actionShortcuts.CurrentScheme,
                VsShortcut = def.VsShortcuts,
                Multiplier = statistics.Multiplier
            };
        }

        private CommandBarActionDef Find(string actionId)
        {
            var cache = GetCachedActionDefs();
            CommandBarActionDef def;
            return cache.TryGetValue(actionId, out def) ? def : null;
        }

        private IDictionary<string, CommandBarActionDef> GetCachedActionDefs()
        {
            if (cachedActionDefs.Any())
                return cachedActionDefs;

            PopulateCachedActionDefs();

            return cachedActionDefs;
        }

        private void PopulateCachedActionDefs()
        {
            var menuBar = Logger.CatchSilent(() => ((CommandBars) dte.CommandBars)["MenuBar"]);
            if (menuBar != null)
            {
                var compoundException = new CompoundException();

                var enumDescendantControls = menuBar.EnumDescendantControls(compoundException).ToList();
                foreach (var tuple in enumDescendantControls)
                {
                    // Make sure it's an actionable type (e.g. not a CommandBarPopup, or _CommandBarActiveX)
                    var commandBarControl = tuple.Item1;
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

                        var commandBarPopups = tuple.Item2;
                        var def = new CommandBarActionDef(vsShortcutFinder, dte, actionId, commandId, commandBarControl,
                            commandBarPopups ?? EmptyArray<CommandBarPopup>.Instance);
                        cachedActionDefs.Add(actionId, def);
                    }
                }

                if (compoundException.Exceptions.Any())
                    Logger.LogException(compoundException);
            }
        }

        private class CommandBarActionDef
        {
            private readonly Lazy<BackingFields> backingFields;

            private class BackingFields
            {
                public string Text;
                public string Path;
                public ShortcutSequence VsShortcut;
            }

            public CommandBarActionDef(VsShortcutFinder vsShortcutFinder, DTE dte, string actionId,
                                       CommandID commandId, CommandBarControl control,
                                       CommandBarPopup[] parentPopups)
            {
                ActionId = actionId;

                // Lazily initialise. Talking to the command bar objects is SLOOOOOOOWWWWWW.
                backingFields = Lazy.Of(() =>
                {
                    Assertion.AssertNotNull(control, "control != null");

                    var sb = new StringBuilder();
                    foreach (var popup in parentPopups)
                        sb.AppendFormat("{0} \u2192 ", popup.Caption);

                    var fields = new BackingFields
                    {
                        Text = MnemonicStore.RemoveMnemonicMark(control.Caption),
                        Path = MnemonicStore.RemoveMnemonicMark(sb.ToString())
                    };

                    var command = VsCommandHelpers.TryGetVsCommandAutomationObject(commandId, dte);
                    var vsShortcut = vsShortcutFinder.GetVsShortcut(command);
                    if (vsShortcut != null)
                    {
                        var details = new ShortcutDetails[vsShortcut.KeyboardShortcuts.Length];
                        for (int i = 0; i < vsShortcut.KeyboardShortcuts.Length; i++)
                        {
                            var keyboardShortcut = vsShortcut.KeyboardShortcuts[i];
                            details[i] = new ShortcutDetails(KeyConverter.Convert(keyboardShortcut.Key),
                                keyboardShortcut.Modifiers);
                        }
                        fields.VsShortcut = new ShortcutSequence(details);
                    }

                    return fields;
                }, true);
            }

            public string ActionId { get; }

            public string Text => backingFields.Value.Text;
            public ShortcutSequence VsShortcuts => backingFields.Value.VsShortcut;
            public string Path => backingFields.Value.Path;
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
        public static IEnumerable<Tuple<CommandBarControl, CommandBarPopup[]>> EnumDescendantControls(
            [NotNull] this CommandBar root, [NotNull] CompoundException cex)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (cex == null)
                throw new ArgumentNullException(nameof(cex));

            var queueEnumChildren =
                new Queue<Tuple<CommandBar, CommandBarPopup[]>>(new[]
                {Tuple.Create(root, EmptyArray<CommandBarPopup>.Instance)});
            int nBar = -1;

            while (queueEnumChildren.Any())
            {
                Tuple<CommandBar, CommandBarPopup[]> tuple = queueEnumChildren.Dequeue();

                nBar++;
                List<CommandBarControl> children = null;
                try
                {
                    // All children
                    children = tuple.Item1.Controls.OfType<CommandBarControl>().ToList();

                    // EnqueueJob child containers
                    var popups = children.OfType<CommandBarPopup>()
                        .Select(popup => Tuple.Create(popup.CommandBar, tuple.Item2.Concat(popup).ToArray()));
                    foreach (var popup in popups)
                        queueEnumChildren.Enqueue(popup);
                }
                catch (Exception e)
                {
                    var ex = new LoggerException("Failed to enum command bar child controls.", e);
                    ex.AddData("IndexInQueue", () => nBar);
                    ex.AddData("CommandBarName", () => tuple.Item1.Name);
                    ex.AddData("CommandBarIndexProperty", () => tuple.Item1.Index);
                    cex.Exceptions.Add(ex);
                }

                // Emit
                if (children != null) // Null if were exceptions (cannot yield from a catch)
                {
                    foreach (CommandBarControl child in children)
                        yield return Tuple.Create(child, tuple.Item2);
                }
            }
        }
    }
}