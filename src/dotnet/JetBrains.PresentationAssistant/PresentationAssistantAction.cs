using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.ReSharper.Feature.Services.Menu;

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