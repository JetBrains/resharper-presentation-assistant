using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Navigation.Features.GoToDeclaration;
using JetBrains.ReSharper.Features.Navigation.Features.GoToImplementation;
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
        private readonly IActionDefs defs;
        private readonly VsShortcutFinder vsShortcutFinder;
        private readonly ActionPresentationHelper actionPresentationHelper;

        public ShortcutFactory(IActionShortcuts actionShortcuts, IActionDefs defs,
                               VsShortcutFinder vsShortcutFinder, ActionPresentationHelper actionPresentationHelper)
        {
            this.actionShortcuts = actionShortcuts;
            this.defs = defs;
            this.vsShortcutFinder = vsShortcutFinder;
            this.actionPresentationHelper = actionPresentationHelper;
        }

        public Shortcut Create(IActionDefWithId def, int multiplier)
        {
            IActionDefWithId overridingDef;
            def = GetPrimaryDef(def, out overridingDef);

            var shortcut = new Shortcut
            {
                ActionId = def.ActionId,
                Text = GetText(def),
                Path = GetPath(def),
                Description = def.Description,
                CurrentScheme = actionShortcuts.CurrentScheme,
                Multiplier = multiplier
            };

            SetShortcuts(shortcut, def, overridingDef);
            return shortcut;
        }

        private IActionDefWithId GetPrimaryDef(IActionDefWithId originalDef, out IActionDefWithId secondaryDef)
        {
            // The way ReSharper overrides Visual Studio's go to methods is a bit confusing.
            // Normally, an overridden command is just an action that also overrides a VS
            // command. The go to commands have both ReSharper actions and VS overriding 
            // commands. Also, the names are confusing. Visual Studio's "Go to Definition"
            // maps to ReSharper "Go to Declaration" and its "Go to Declaration" maps to
            // "Go to Implementation". The IntelliJ shortcuts are specified in the ReSharper
            // actions, and the VS shortcuts are simply what's mapped to the overridden
            // command. So, we need to handle the two shortcuts at the same time - one
            // will provide the standard name, and the IntelliJ shortcut, the other will
            // provide the details to get to the overridden VS command to give us the
            // proper bindings.
            switch (originalDef.ActionId)
            {
                case "GotoDefinitionOverride":
                    secondaryDef = originalDef;
                    return defs.GetActionDef<GotoDeclarationAction>();

                case GotoDeclarationAction.ACTION_ID:
                    secondaryDef = defs.GetActionDef<GotoDefinitionOverrideAction>();
                    return originalDef;

                case "GoToDeclarationOverride":
                    secondaryDef = originalDef;
                    return defs.GetActionDef<GotoImplementationsAction>();

                case GotoImplementationsAction.GOTO_IMPLEMENTATION_ACTION_ID:
                    secondaryDef = defs.GetActionDef<GoToDeclarationOverrideAction>();
                    return originalDef;
            }

            secondaryDef = originalDef;
            return originalDef;
        }

        private string GetPath(IActionDefWithId def)
        {
            var actionWithPath = def as IActionWithPath;
            var path = actionWithPath != null ? actionWithPath.Path : actionPresentationHelper.GetPathPresentationToRoot(def);

            return !string.IsNullOrEmpty(path) ? MnemonicStore.RemoveMnemonicMark(path) + " \u2192 " : string.Empty;
        }

        private string GetText(IActionDefWithId def)
        {
            var text = MnemonicStore.RemoveMnemonicMark(def.Text).Trim(TrimCharacters);
            text = string.IsNullOrEmpty(text) ? def.ActionId : text;

            switch (text)
            {
                    // TODO: Maybe "Expand Live Template/Next hotspot"? Based on context
                case "TextControl.Tab":
                    text = "Tab";
                    break;

                    // TODO: Maybe "Previous hotspot" if editing a Live Template
                case "TabLeft":
                    text = "Shift+Tab";
                    break;
            }

            return text;
        }

        private void SetShortcuts(Shortcut shortcut, IActionDefWithId def, IActionDefWithId overridingDef)
        {
            // TODO: Should this be an option in the options dialog? Show secondary scheme if different?
            const bool showSecondarySchemeIfSame = false;

            SetGivenShortcuts(shortcut, def, showSecondarySchemeIfSame);
            SetVsOverriddenShortcuts(shortcut, overridingDef, showSecondarySchemeIfSame);
            SetWellKnownShortcuts(shortcut, def, showSecondarySchemeIfSame);
        }

        private void SetGivenShortcuts(Shortcut shortcut, IActionDefWithId def, bool showSecondarySchemeIfSame)
        {
            shortcut.VsShortcut = GetFirstShortcutSequence(def.VsShortcuts);
            shortcut.IntellijShortcut = GetFirstShortcutSequence(def.IdeaShortcuts);

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
                shortcut.VsShortcut = GetShortcutSequence(vsShortcutFinder.GetOverriddenVsShortcut(def));

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