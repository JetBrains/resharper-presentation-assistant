using JetBrains.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public partial class ShortcutFactory
    {
        // ReSharper 9.1 handles overriding the go to definition/declaration more
        // cleanly than 9.0, so we don't need to override anything
        private IActionDefWithId GetPrimaryDef(IActionDefWithId originalDef, out IActionDefWithId secondaryDef)
        {
            secondaryDef = originalDef;
            return originalDef;
        }
    }
}