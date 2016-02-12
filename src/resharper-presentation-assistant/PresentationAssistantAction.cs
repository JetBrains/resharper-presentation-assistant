using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Menu;
using JetBrains.UI.ActionsRevised;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [Action(ActionId, "Presentation Assistant", Description = "Enable and disable the presentation assistant", Id = 987987)]
    public class PresentationAssistantAction : ICheckableAction, IInsertLast<ToolsMenu>
    {
        public const string ActionId = "PresentationAssistant.Toggle";

        public bool Update(IDataContext context, CheckedActionPresentation presentation)
        {
            var settings = context.GetComponent<PresentationAssistantSettingsStore>().GetSettings();
            presentation.Checked = settings.Enabled;
            return true;
        }

        public void Execute(IDataContext context)
        {
            var settingsStore = context.GetComponent<PresentationAssistantSettingsStore>();
            var settings = settingsStore.GetSettings();
            settings.Enabled = !settings.Enabled;
            settingsStore.SetSettings(settings);
        }
    }
}