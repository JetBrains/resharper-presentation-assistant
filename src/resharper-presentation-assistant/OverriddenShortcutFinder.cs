using JetBrains.Application;
using JetBrains.Application.Shortcuts;
using JetBrains.Application.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class OverriddenShortcutFinder
    {
        // This is the current key binding for the command being overridden by an action
        // By definition, this is the VS key binding (because we're overriding an existing
        // VS command and using its key bindings)
        public virtual ActionShortcut GetOverriddenVsShortcut(IActionDefWithId def)
        {
            return null;
        }
    }
}