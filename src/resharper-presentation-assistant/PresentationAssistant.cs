using System;
using JetBrains.Application;
using JetBrains.Application.ActivityTrackingNew;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent(Requirement = InstantiationRequirement.Instant)]
    public class PresentationAssistant : IActivityTracking
    {
        private static readonly TimeSpan MultiplierTimeout = TimeSpan.FromSeconds(10);

        private readonly IActionDefs defs;
        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly ShortcutFactory shortcutFactory;

        private DateTime lastDisplayed;
        private string lastActionId;
        private int multiplier;

        public PresentationAssistant(Lifetime lifetime, IActionDefs defs,
                                     PresentationAssistantWindowOwner presentationAssistantWindowOwner,
                                     ShortcutFactory shortcutFactory)
        {
            this.defs = defs;
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.shortcutFactory = shortcutFactory;
        }

        // Implementing IActivityTracking is better than subscribing to ActionEvents, as
        // extensible workflow based actions (Refactor This, Navigate To, Generate, etc.)
        // will notify activity tracking, but invoke the action manually, so it doesn't
        // get reported by ActionEvents
        public void TrackAction(string actionId)
        {
            var def = defs.TryGetActionDefById(actionId);
            if (def != null)
                OnAction(def);
        }

        public void TrackActivity(string activityGroup, string activityId, int count = 1)
        {
            // TODO: Track activities in VsAction activityGroup
        }

        private void OnAction(IActionDefWithId def)
        {
            if (ActionIdBlacklist.IsBlacklisted(def.ActionId))
                return;

            UpdateMultiplier(def);
            var shortcut = shortcutFactory.Create(def, multiplier);
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
