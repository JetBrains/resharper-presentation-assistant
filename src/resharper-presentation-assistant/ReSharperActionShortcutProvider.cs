using System;
using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ReSharperActionShortcutProvider : IShortcutProvider
    {
        private static readonly char[] TrimCharacters = {'.', '\u2026'};

        private readonly ShortcutDisplayStatistics statistics;
        private readonly IActionDefs defs;
        private readonly IActionShortcuts actionShortcuts;
        private readonly ActionPresentationHelper actionPresentationHelper;
        private readonly OverriddenShortcutFinder overriddenShortcutFinder;
        private readonly HotspotSessionExecutor hotspotSessionExecutor;
        private readonly SettingsStore settingsStore;

        public ReSharperActionShortcutProvider(ShortcutDisplayStatistics statistics,
                                               IActionDefs defs,
                                               IActionShortcuts actionShortcuts,
                                               ActionPresentationHelper actionPresentationHelper,
                                               OverriddenShortcutFinder overriddenShortcutFinder,
                                               HotspotSessionExecutor hotspotSessionExecutor,
                                               SettingsStore settingsStore)
        {
            this.statistics = statistics;
            this.defs = defs;
            this.actionShortcuts = actionShortcuts;
            this.actionPresentationHelper = actionPresentationHelper;
            this.overriddenShortcutFinder = overriddenShortcutFinder;
            this.hotspotSessionExecutor = hotspotSessionExecutor;
            this.settingsStore = settingsStore;
        }

        public Shortcut GetShortcut(string actionId)
        {
            var def = defs.TryGetActionDefById(actionId);
            if (def == null)
                return null;

            actionId = HandleTabOverrides(actionId);

            var text = GetText(def.Text, actionId);
            if (string.IsNullOrEmpty(text))
                return null;

            statistics.OnAction(actionId);

            var shortcut = new Shortcut
            {
                ActionId = actionId,
                Text = text,
                Path = GetPath(def),
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = statistics.Multiplier
            };

            SetShortcuts(shortcut, def);
            return shortcut;
        }

        private string HandleTabOverrides(string actionId)
        {
            if (IsHotspotActive())
                return HandleActiveHotspots(actionId);
            if (IsCodeCompletionActive())
                return HandleActiveCodeCompletion(actionId);

            return actionId;
        }

        private string HandleActiveHotspots(string actionId)
        {
            switch (actionId)
            {
                case "TextControl.Enter":
                case "TextControl.Tab":
                    return "Synthetic.NextHotspot";

                case "TabLeft":
                    return "Synthetic.PreviousHotspot";
            }

            return actionId;
        }

        private string HandleActiveCodeCompletion(string actionId)
        {
            switch (actionId)
            {
                case "TextControl.Enter":
                    return "Synthetic.CompleteItem.Enter";

                case "TextControl.Tab":
                    return "Synthetic.CompleteItem.Tab";
            }

            return actionId;
        }

        private string GetText(string text, string actionId)
        {
            switch (actionId)
            {
                case "TextControl.Enter":
                    return null;

                    // TODO: "Insert Live Template"
                    // We only get this when someone uses Tab to insert a live template.
                    // It would be nice to recognise that. We can't assume it, as any other
                    // tab handler could be mis-recognised
                case "TextControl.Tab":
                    return "Tab";

                case "Synthetic.NextHotspot":
                    return "Next Hotspot";

                case "Synthetic.PreviousHotspot":
                    return "Previous Hotspot";

                case "Synthetic.CompleteItem.Enter":
                    return GetEnterCompleteItemText();

                case "Synthetic.CompleteItem.Tab":
                    return GetTabCompleteItemText();
            }

            var trim = MnemonicStore.RemoveMnemonicMark(text).Trim(TrimCharacters);
            return string.IsNullOrEmpty(trim) ? actionId : trim;
        }

        private string GetEnterCompleteItemText()
        {
            var boundSettingsStore = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            var insertType = boundSettingsStore.GetValue((CodeCompletionSettingsKey s) => s.EnterKeyInsertType);
            return GetCompleteItemText(insertType);
        }

        private string GetTabCompleteItemText()
        {
            var boundSettingsStore = settingsStore.BindToContextTransient(ContextRange.ApplicationWide);
            var insertType = boundSettingsStore.GetValue((CodeCompletionSettingsKey s) => s.TabKeyInsertType);
            return GetCompleteItemText(insertType);
        }

        private string GetCompleteItemText(LookupItemInsertType insertType)
        {
            switch (insertType)
            {
                case LookupItemInsertType.Insert:
                    return "Insert Item";
                case LookupItemInsertType.Replace:
                    return "Replace with Item";
                default:
                    throw new ArgumentOutOfRangeException(nameof(insertType), insertType, null);
            }
        }

        private bool IsHotspotActive()
        {
            return hotspotSessionExecutor.CurrentSession != null;
        }

        private bool IsCodeCompletionActive()
        {
            var codeCompletionSessionManager = GetCodeCompletionSessionManager();
            return codeCompletionSessionManager != null && codeCompletionSessionManager.HasActiveLookup();
        }

        private ICodeCompletionSessionManager GetCodeCompletionSessionManager()
        {
            var solutionOwners = Shell.Instance.GetComponents<ISolutionOwner>();
            foreach (var solutionOwner in solutionOwners.OfType<SolutionManagerBase>())
            {
                var solution = solutionOwner.CurrentSolution;
                if (solution != null)
                {
                    return solution.GetComponent<ICodeCompletionSessionManager>();
                }
            }
            return null;
        }

        private string GetPath(IActionDefWithId def)
        {
            var path = actionPresentationHelper.GetPathPresentationToRoot(def);

            return !string.IsNullOrEmpty(path) ? MnemonicStore.RemoveMnemonicMark(path) + " \u2192 " : string.Empty;
        }

        private void SetShortcuts(Shortcut shortcut, IActionDefWithId def)
        {
            const bool showSecondarySchemeIfSame = false;

            SetGivenShortcuts(shortcut, def, showSecondarySchemeIfSame);
            SetVsOverriddenShortcuts(shortcut, def, showSecondarySchemeIfSame);
            SetWellKnownShortcuts(shortcut, def, showSecondarySchemeIfSame);
        }

        private void SetGivenShortcuts(Shortcut shortcut, IActionDefWithId def, bool showSecondarySchemeIfSame)
        {
            shortcut.VsShortcut = GetFirstShortcutSequence(def.VsShortcuts);
            shortcut.IntellijShortcut = GetFirstShortcutSequence(def.IdeaShortcuts);

            if (HasSameShortcuts(shortcut) && !showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = null;
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

        private void SetVsOverriddenShortcuts(Shortcut shortcut, IActionDefWithId def, bool showSecondarySchemeIfSame)
        {
            // If we don't have any VS shortcuts, look to see if the action is an override of a
            // VS command, and get the current key binding for that command
            if (!shortcut.HasVsShortcuts)
                shortcut.VsShortcut = GetShortcutSequence(overriddenShortcutFinder.GetOverriddenVsShortcut(def));

            if (HasSameShortcuts(shortcut) && !showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = null;
        }

        private void SetWellKnownShortcuts(Shortcut shortcut, IActionDefWithId def,
                                           bool showSecondarySchemeIfSame)
        {
            switch (def.ActionId)
            {
                // The Escape action doesn't have a bound shortcut, or a VS override
                case "Escape":
                    shortcut.VsShortcut = GetShortcutSequence("Escape");
                    break;

                // Only happens when we're tabbing around hotspots in Live Templates. Useful to show
                case "TextControl.Tab":
                    shortcut.VsShortcut = GetShortcutSequence("Tab");
                    break;

                case "TextControl.Enter":
                    shortcut.VsShortcut = GetShortcutSequence("Enter");
                    break;

                // The shortcuts for the overridden VS "go to" commands come from the current
                // binding, but if we're in the IntelliJ binding, they get removed, so we have
                // to hard code them.
                case "GotoDeclaration":
                    if (shortcut.VsShortcut == null)
                        shortcut.VsShortcut = GetShortcutSequence("F12");
                    break;

                case "GotoImplementation":
                    if (shortcut.VsShortcut == null)
                        shortcut.VsShortcut = GetShortcutSequence("Control+F12");
                    break;
            }

            if (!shortcut.HasIntellijShortcuts && showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = shortcut.VsShortcut;
        }
    }
}