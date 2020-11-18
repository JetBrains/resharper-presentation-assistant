using System.Collections.Generic;
using JetBrains.Application;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class ShortcutProvider
    {
        private readonly IEnumerable<IShortcutProvider> shortcutProviders;

        public ShortcutProvider(IEnumerable<IShortcutProvider> shortcutProviders)
        {
            this.shortcutProviders = shortcutProviders;
        }

        public Shortcut GetShortcut(string actionId)
        {
            foreach (var shortcutProvider in shortcutProviders)
            {
                var shortcut = shortcutProvider.GetShortcut(actionId);
                if (shortcut != null)
                    return shortcut;
            }
            return null;
        }
    }
}