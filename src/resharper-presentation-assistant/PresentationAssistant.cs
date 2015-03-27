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

        private readonly ActionFinder actionFinder;
        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly ShortcutFactory shortcutFactory;

        private DateTime lastDisplayed;
        private string lastActionId;
        private int multiplier;

        public PresentationAssistant(Lifetime lifetime, ActionFinder actionFinder,
                                     PresentationAssistantWindowOwner presentationAssistantWindowOwner,
                                     ShortcutFactory shortcutFactory)
        {
            this.actionFinder = actionFinder;
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.shortcutFactory = shortcutFactory;

            Enabled = new Property<bool>(lifetime, "PresentationAssistant::Enabled");
            Enabled.FlowInto(lifetime, presentationAssistantWindowOwner.Enabled);
        }

        public Property<bool> Enabled { get; private set; }

        // Implementing IActivityTracking is better than subscribing to ActionEvents, as
        // extensible workflow based actions (Refactor This, Navigate To, Generate, etc.)
        // will notify activity tracking, but invoke the action manually, so it doesn't
        // get reported by ActionEvents
        public void TrackAction(string actionId)
        {
            if (!Enabled.Value)
                return;

            OnAction(actionId);
        }

        public void TrackActivity(string activityGroup, string activityId, int count = 1)
        {
            if (!Enabled.Value)
                return;

            if (activityGroup == "VsAction")
                OnAction(activityId);
        }

        private void OnAction(string actionId)
        {
            if (ActionIdBlacklist.IsBlacklisted(actionId))
                return;

            var def = actionFinder.Find(actionId);
            if (def == null)
                return;

            OnAction(def);
        }

        private void OnAction(IActionDefWithId def)
        {
            UpdateMultiplier(def.ActionId);
            var shortcut = shortcutFactory.Create(def, multiplier);
            presentationAssistantWindowOwner.Show(shortcut);
        }

        private void UpdateMultiplier(string actionId)
        {
            var now = DateTime.UtcNow;
            if (actionId == lastActionId && (now - lastDisplayed) < MultiplierTimeout)
                multiplier++;
            else
                multiplier = 1;
            lastDisplayed = now;
            lastActionId = actionId;
        }
    }
}
