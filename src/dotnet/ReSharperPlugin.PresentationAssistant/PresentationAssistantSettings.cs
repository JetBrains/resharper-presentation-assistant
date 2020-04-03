using JetBrains.Application.Settings;
using JetBrains.Application.UI.Utils;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [SettingsKey(typeof(UserInterfaceSettings), "Presentation Assistant settings")]
    public class PresentationAssistantSettings
    {
        [SettingsEntry(true, "Enabled")]
        public bool Enabled { get; set; }

        [SettingsEntry(false, "Welcome message shown")]
        public bool WelcomeMessageShown { get; set; }
    }
}