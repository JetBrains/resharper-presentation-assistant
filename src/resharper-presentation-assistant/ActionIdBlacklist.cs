using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public static class ActionIdBlacklist
    {
        private static readonly HashSet<string> ActionIds = new HashSet<string>
        {
            // TODO: Maybe all TextControl.* actions should be blacklisted
            // These are the only actions that should be hidden
            "TextControl.Enter",
            "TextControl.Backspace",
            "TextControl.Delete",
            "TextControl.Cut",
            "TextControl.Paste",

            // Used when code completion window is visible
            "TextControl.Up", "TextControl.Down",
            "TextControl.PageUp", "TextControl.PageDown",

            // If camel humps are enabled in the editor
            "WordPrev",
            "WordPrevExtend",
            "WordNext",
            "WordNextExtend",

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