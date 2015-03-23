using JetBrains.Application;
using JetBrains.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ActionFinder
    {
        private readonly IActionDefs defs;

        public ActionFinder(IActionDefs defs)
        {
            this.defs = defs;
        }

        public virtual IActionDefWithId Find(string actionId)
        {
            return defs.TryGetActionDefById(actionId);
        }
    }

    public interface IActionWithPath
    {
        string Path { get; }
    }
}
