using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public static class ActionIdBlacklist
    {
        private static readonly HashSet<string> ActionIds = new HashSet<string>
        {
            // These are the only actions that should be hidden
            "TextControl.Backspace",
            "TextControl.Delete",
            "TextControl.Cut",
            "TextControl.Paste",
            "TextControl.Copy",

            // Used when code completion window is visible
            "TextControl.Up", "TextControl.Down",
            "TextControl.PageUp", "TextControl.PageDown",

            // If camel humps are enabled in the editor
            "TextControl.PreviousWord",
            "TextControl.PreviousWord.Selection",
            "TextControl.NextWord",
            "TextControl.NextWord.Selection",

            // VS commands, not R# commands
            "Edit.Up", "Edit.Down", "Edit.Left", "Edit.Right", "Edit.PageUp", "Edit.PageDown",

            // Make sure we don't try to show the presentation assistant popup just as we're
            // killing the popups
            PresentationAssistantAction.ActionId
        };

        public static bool IsBlacklisted(string actionId)
        {
            return ActionIds.Contains(actionId);
        }
    }
}