using System;
using JetBrains.Application;
using JetBrains.Application.ActivityTrackingNew;
using JetBrains.Application.Parts;
using JetBrains.DataFlow;
using JetBrains.Threading;
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
        private readonly PresentationAssistantSettingsStore settingsStore;

        private DateTime lastDisplayed;
        private string lastActionId;
        private int multiplier;
        private bool enabled;

        public PresentationAssistant(Lifetime lifetime, ActionFinder actionFinder,
                                     PresentationAssistantWindowOwner presentationAssistantWindowOwner,
                                     ShortcutFactory shortcutFactory, PresentationAssistantSettingsStore settingsStore,
                                     IThreading threading)
        {
            this.actionFinder = actionFinder;
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
            this.shortcutFactory = shortcutFactory;
            this.settingsStore = settingsStore;

            // Post to the UI thread so that the app has time to start before we show the message
            threading.ExecuteOrQueue("Presentation Assistant initial message", () =>
            {
                var settings = settingsStore.GetSettings();
                UpdateSettings(!settings.WelcomeMessageShown);
                settings.WelcomeMessageShown = true;
                settingsStore.SetSettings(settings);
            });

            // Post to the UI thread because settings change are raised on a background thread.
            // Has the unfortunate side effect that the action disabling the assistant is shown
            // very briefly, so we blacklist it
            settingsStore.SettingsChanged.Advise(lifetime, _ => threading.ExecuteOrQueue("Presentation Assistant update enabled", () => UpdateSettings(true)));
        }

        private void UpdateSettings(bool showOwnActionImmediately)
        {
            var localSettings = settingsStore.GetSettings();
            presentationAssistantWindowOwner.Enabled.SetValue(localSettings.Enabled);
            enabled = localSettings.Enabled;

            if (enabled && showOwnActionImmediately)
                OnAction(PresentationAssistantAction.ActionId);
        }

        // Implementing IActivityTracking is better than subscribing to ActionEvents, as
        // extensible workflow based actions (Refactor This, Navigate To, Generate, etc.)
        // will notify activity tracking, but invoke the action manually, so it doesn't
        // get reported by ActionEvents
        public void TrackAction(string actionId)
        {
            if (!enabled || ActionIdBlacklist.IsBlacklisted(actionId))
                return;

            OnAction(actionId);
        }

        public void TrackActivity(string activityGroup, string activityId, int count = 1)
        {
            if (!enabled)
                return;

            if (activityGroup == "VsAction" && !ActionIdBlacklist.IsBlacklisted(activityId))
                OnAction(activityId);
        }

        private void OnAction(string actionId)
        {
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
