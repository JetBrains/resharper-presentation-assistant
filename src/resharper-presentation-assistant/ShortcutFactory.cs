using System.Linq;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.StructuralNavigation;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.ActionsRevised.Loader;
using JetBrains.UI.ActionsRevised.Shortcuts;
using JetBrains.UI.PopupMenu.Impl;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ShortcutFactory
    {
        private static readonly char[] TrimCharacters = {'.', '\u2026'};

        private readonly IActionShortcuts actionShortcuts;
        private readonly OverriddenShortcutFinder overriddenShortcutFinder;
        private readonly HotspotSessionExecutor hotspotSessionExecutor;
        private StructuralNavigationManager structuralNavigationManager;

        public ShortcutFactory(IActionShortcuts actionShortcuts, OverriddenShortcutFinder overriddenShortcutFinder, HotspotSessionExecutor hotspotSessionExecutor)
        {
            this.actionShortcuts = actionShortcuts;
            this.overriddenShortcutFinder = overriddenShortcutFinder;
            this.hotspotSessionExecutor = hotspotSessionExecutor;
        }

        public Shortcut Create(string actionId, string text, string path, int multiplier, [CanBeNull] IActionDefWithId def)
        {
            Initialise();

            if (actionId == "TextControl.Enter" && !IsHotspotActive() && !IsCodeCompletionActive())
                return null;

            var shortcut = new Shortcut
            {
                ActionId = actionId,
                Text = GetText(actionId, text),
                Path = path,
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = multiplier
            };

            SetShortcuts(shortcut, actionId, def.VsShortcuts, def.IdeaShortcuts, def);
            return shortcut;
        }

        private void Initialise()
        {
            // Can't initialise this through the constructor, appears to be a circular dependency
            if (structuralNavigationManager == null)
                structuralNavigationManager = Shell.Instance.GetComponent<StructuralNavigationManager>();
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

        private string GetText(string actionId, string text)
        {
            var trim = MnemonicStore.RemoveMnemonicMark(text).Trim(TrimCharacters);
            trim = string.IsNullOrEmpty(trim) ? actionId : trim;

            switch (trim)
            {
                case "TextControl.Enter":
                    if (IsHotspotActive())
                        trim = "Next Hotspot";
                    else if (IsCodeCompletionActive())
                        trim = "Complete Item"; // TODO: Insert or replace?
                    break;

                case "TextControl.Tab":
                    // TODO: Expand live template
                    // TODO: forward structural navigation
                    if (IsHotspotActive())
                        trim = "Next Hotspot";
                    else if (IsCodeCompletionActive())
                        trim = "Complete Item"; // TODO: Insert or replace?
                    else if (structuralNavigationManager.IsSelectedByTabNavigation)
                        trim = "Forward Structural Navigation";
                    else
                        trim = "Tab";
                    break;

                case "TabLeft":
                case "Tab Left":
                    // TODO: backward structural navigation
                    if (IsHotspotActive())
                        trim = "Previous Hotspot";
                    else if (structuralNavigationManager.IsSelectedByTabNavigation)
                        trim = "Backward Structural Navigation";
                    else
                        trim = "Shift+Tab";
                    break;
            }

            return trim;
        }

        private void SetShortcuts(Shortcut shortcut, string actionId, string[] vsShortcuts, string[] ideaShortcuts, [CanBeNull] IActionDefWithId def)
        {
            // TODO: Should this be an option in the options dialog? Show secondary scheme if different?
            const bool showSecondarySchemeIfSame = false;

            SetGivenShortcuts(shortcut, vsShortcuts, ideaShortcuts, showSecondarySchemeIfSame);
            SetVsOverriddenShortcuts(shortcut, def, showSecondarySchemeIfSame);
            SetWellKnownShortcuts(shortcut, actionId, showSecondarySchemeIfSame);
        }

        private void SetGivenShortcuts(Shortcut shortcut, string[] vsShortcuts, string[] ideaShortcuts, bool showSecondarySchemeIfSame)
        {
            shortcut.VsShortcut = GetFirstShortcutSequence(vsShortcuts);
            shortcut.IntellijShortcut = GetFirstShortcutSequence(ideaShortcuts);

            if (HasSameShortcuts(shortcut) && !showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = null;
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

        private void SetVsOverriddenShortcuts(Shortcut shortcut, IActionDefWithId def, bool showSecondarySchemeIfSame)
        {
            // If we don't have any VS shortcuts, look to see if the action is an override of a
            // VS command, and get the current key binding for that command
            if (!shortcut.HasVsShortcuts)
                shortcut.VsShortcut = GetShortcutSequence(overriddenShortcutFinder.GetOverriddenVsShortcut(def));

            if (HasSameShortcuts(shortcut) && !showSecondarySchemeIfSame)
                shortcut.IntellijShortcut = null;
        }

        private void SetWellKnownShortcuts(Shortcut shortcut, string actionId,
            bool showSecondarySchemeIfSame)
        {
            switch (actionId)
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