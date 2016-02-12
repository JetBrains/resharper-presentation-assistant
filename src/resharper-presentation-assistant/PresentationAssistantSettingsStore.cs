using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantSettingsStore
    {
        private readonly ISettingsStore settingsStore;
        private readonly DataContexts dataContexts;

        public PresentationAssistantSettingsStore(Lifetime lifetime, ISettingsStore settingsStore, DataContexts dataContexts)
        {
            this.settingsStore = settingsStore;
            this.dataContexts = dataContexts;

            SettingsChanged = new SimpleSignal(lifetime, "Presentation Assistant settings changed");

            var key = settingsStore.Schema.GetKey<PresentationAssistantSettings>();
            settingsStore.Changed.Advise(lifetime, args =>
            {
                foreach (var changedEntry in args.ChangedEntries)
                {
                    if (changedEntry.Parent == key)
                    {
                        if (changedEntry.LocalName == "Enabled")
                            SettingsChanged.Fire();
                        break;
                    }
                }
            });
        }

        public SimpleSignal SettingsChanged { get; private set; }

        public PresentationAssistantSettings GetSettings()
        {
            var boundSettings = BindSettingsStore();
            return boundSettings.GetKey<PresentationAssistantSettings>(SettingsOptimization.OptimizeDefault);
        }

        public void SetSettings(PresentationAssistantSettings settings)
        {
            var boundSettings = BindSettingsStore();
            boundSettings.SetKey(settings, SettingsOptimization.OptimizeDefault);
        }

        private IContextBoundSettingsStore BindSettingsStore()
        {
            var store = settingsStore.BindToContextTransient(ContextRange.Smart((l, _) => dataContexts.CreateOnSelection(l)));
            return store;
        }
    }
}