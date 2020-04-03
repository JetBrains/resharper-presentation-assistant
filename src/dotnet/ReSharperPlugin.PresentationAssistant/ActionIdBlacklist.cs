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

            "TextControl.Left", "TextControl.Right",
            "TextControl.Left.Selection", "TextControl.Right.Selection",
            "TextControl.Up", "TextControl.Down",
            "TextControl.Up.Selection", "TextControl.Down.Selection",
            "TextControl.Home", "TextControl.End",
            "TextControl.Home.Selection", "TextControl.End.Selection",
            "TextControl.PageUp", "TextControl.PageDown",
            "TextControl.PageUp.Selection", "TextControl.PageDown.Selection",
             "TextControl.PreviousWord", "TextControl.NextWord",
            "TextControl.PreviousWord.Selection", "TextControl.NextWord.Selection",

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