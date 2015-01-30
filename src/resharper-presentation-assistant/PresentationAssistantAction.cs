using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Menu;
using JetBrains.UI.ActionsRevised;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [Action("PresentationAssistant.Toggle", "Presentation Assistant", Description = "Enable and disable the presentation assistant", Id = 987987)]
    public class PresentationAssistantAction : ICheckableAction, IInsertLast<ToolsMenu>
    {
        public bool Update(IDataContext context, CheckedActionPresentation presentation)
        {
            var presentationAssistant = context.GetComponent<PresentationAssistant>();
            presentation.Checked = presentationAssistant.Enabled.Value;
            return true;
        }

        public void Execute(IDataContext context)
        {
            var presentationAssistant = context.GetComponent<PresentationAssistant>();
            presentationAssistant.Enabled.SetValue(!presentationAssistant.Enabled.Value);
        }
    }
}