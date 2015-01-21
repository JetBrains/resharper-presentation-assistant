using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Handlers;
using JetBrains.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent(Requirement = InstantiationRequirement.Instant)]
    public class PresentationAssistant
    {
        private static readonly TimeSpan MultiplierTimeout = TimeSpan.FromSeconds(10);

        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly ShortcutFactory shortcutFactory;

        private DateTime lastDisplayed;
        private string lastActionId;
        private int multiplier;

        public PresentationAssistant(Lifetime lifetime, ActionEvents actionEvents,
                                     PresentationAssistantWindowOwner presentationAssistantWindowOwner,
                                     ShortcutFactory shortcutFactory)
        {
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.shortcutFactory = shortcutFactory;
            actionEvents.AdviseExecuteAction(lifetime, OnAction);
        }

        private void OnAction(ActionEvents.ActionEventArgs obj)
        {
            if (ActionIdBlacklist.IsBlacklisted(obj.ActionDef.ActionId))
                return;

            UpdateMultiplier(obj.ActionDef);
            var shortcut = shortcutFactory.Create(obj.ActionDef, multiplier);
            presentationAssistantWindowOwner.Show(shortcut);
        }

        private void UpdateMultiplier(IActionDefWithId def)
        {
            var now = DateTime.UtcNow;
            if (def.ActionId == lastActionId && (now - lastDisplayed) < MultiplierTimeout)
                multiplier++;
            else
                multiplier = 1;
            lastDisplayed = now;
            lastActionId = def.ActionId;
        }
    }
}
