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
            "WordPrev",
            "WordPrevExtend",
            "WordNext",
            "WordNextExtend",
        };

        public static bool IsBlacklisted(string actionId)
        {
            return ActionIds.Contains(actionId);
        }
    }
}