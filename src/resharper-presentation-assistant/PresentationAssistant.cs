using JetBrains.Application;
using JetBrains.Application.ActivityTrackingNew;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent(Requirement = InstantiationRequirement.Instant)]
    public class PresentationAssistant : IActivityTracking
    {
        private readonly ShortcutProvider shortcutProvider;
        private readonly PresentationAssistantWindowOwner presentationAssistantWindowOwner;
        private readonly PresentationAssistantSettingsStore settingsStore;

        private bool enabled;

        public PresentationAssistant(Lifetime lifetime,
                                     ShortcutProvider shortcutProvider,
                                     PresentationAssistantWindowOwner presentationAssistantWindowOwner,
                                     PresentationAssistantSettingsStore settingsStore,
                                     IThreading threading)
        {
            this.shortcutProvider = shortcutProvider;
            this.presentationAssistantWindowOwner = presentationAssistantWindowOwner;
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
            settingsStore.SettingsChanged.Advise(lifetime,
                _ => threading.ExecuteOrQueue("Presentation Assistant update enabled", () => UpdateSettings(true)));
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
            if (!enabled 
                || activityGroup != "VsAction"
                || ActionIdBlacklist.IsBlacklisted(activityId))
            {
                return;
            }

            // TODO: "BulbAction" gives full type name of quick fix or context action.
            // IContextActionInfo.Name would give a name to display. Get all context
            // actions via IContextActionTable.AllActions (refresh when types change
            // available in ShellPartCatalogueType), keyed on ICAI.ActionKey.
            // QuickFix doesn't have a name, instance returns bulb actions with text.
            // Don't think it's possible to get a friendly name
            OnAction(activityId);
        }

        private void OnAction(string actionId)
        {
            var shortcut = shortcutProvider.GetShortcut(actionId);
            if (shortcut != null)
                presentationAssistantWindowOwner.Show(shortcut);
        }
    }
}
