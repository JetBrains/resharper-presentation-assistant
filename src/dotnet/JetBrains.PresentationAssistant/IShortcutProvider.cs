namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public interface IShortcutProvider
    {
        Shortcut GetShortcut(string actionId);
    }
}