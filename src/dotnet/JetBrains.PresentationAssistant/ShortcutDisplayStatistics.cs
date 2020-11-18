using System;
using JetBrains.Application;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ShortcutDisplayStatistics
    {
        private static readonly TimeSpan MultiplierTimeout = TimeSpan.FromSeconds(10);

        private DateTime lastDisplayed;

        public int Multiplier { get; private set; }
        public string LastActionId { get; private set; }

        public void OnAction(string actionId)
        {
            var now = DateTime.UtcNow;
            if (actionId == LastActionId && (now - lastDisplayed) < MultiplierTimeout)
                Multiplier++;
            else
                Multiplier = 1;
            lastDisplayed = now;
            LastActionId = actionId;
        }

        public void TouchTimeout()
        {
            lastDisplayed = DateTime.UtcNow;
        }
    }
}