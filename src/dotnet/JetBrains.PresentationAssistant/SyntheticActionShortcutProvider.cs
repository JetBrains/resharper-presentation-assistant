using System.Windows.Forms;
using JetBrains.Application;
using JetBrains.Application.Shortcuts;
using JetBrains.Application.UI.ActionsRevised.Shortcuts;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class SyntheticActionShortcutProvider : IShortcutProvider
    {
        private readonly ShortcutDisplayStatistics statistics;
        private readonly IActionShortcuts actionShortcuts;

        public SyntheticActionShortcutProvider(ShortcutDisplayStatistics statistics,
                                               IActionShortcuts actionShortcuts)
        {
            this.statistics = statistics;
            this.actionShortcuts = actionShortcuts;
        }

        public Shortcut GetShortcut(string actionId)
        {
            // ReSharper records the "StructuralNavigation" action after handling
            // "TextControl.Tab" or "Tab Left", so we'll already have seen that
            // and handled it
            if (actionId == "StructuralNavigation")
            {
                var forwards = statistics.LastActionId == "TextControl.Tab";
                var text = forwards
                    ? "Forward Structural Navigation"
                    : "Backward Structural Navigation";

                // Touch the timeout, but don't change the last action ID or multiplier
                statistics.TouchTimeout();

                return new Shortcut
                {
                    ActionId = actionId,
                    Text = text,
                    CurrentScheme = actionShortcuts.CurrentScheme,
                    Multiplier = statistics.Multiplier,
                    VsShortcut =
                        new ShortcutSequence(new ShortcutDetails(KeyConverter.Convert(Keys.Tab),
                            forwards ? KeyboardModifiers.None : KeyboardModifiers.Shift))
                };
            }

            return null;
        }
    }
}