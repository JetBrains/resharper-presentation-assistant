using System;
using JetBrains.Application;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.Threading;
using JetBrains.UI.PopupLayout;
using JetBrains.UI.StdApplicationUI;
using JetBrains.UI.Theming;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantWindowOwner
    {
        private static readonly TimeSpan VisibleTimeSpan = TimeSpan.FromSeconds(4);

        private readonly IThreading threading;
        private readonly PresentationAssistantPopupWindowContext context;
        private readonly PopupWindowManager popupWindowManager;
        private readonly ITheming theming;
        private readonly IWpfMainWindow mainWindow;
        private Action<Shortcut> showAction;

        public PresentationAssistantWindowOwner(Lifetime lifetime, IThreading threading,
            PresentationAssistantPopupWindowContext context, PopupWindowManager popupWindowManager, ITheming theming,
            IWpfMainWindow mainWindow)
        {
            this.threading = threading;
            this.context = context;
            this.popupWindowManager = popupWindowManager;
            this.theming = theming;
            this.mainWindow = mainWindow;

            Enabled = new Property<bool>(lifetime, "PresentationAssistantWindow::Enabled");
            Enabled.WhenTrue(lifetime, EnableShortcuts);
        }

        public IProperty<bool> Enabled { get; }

        public void Show(Shortcut shortcut)
        {
            showAction(shortcut);
        }

        private void EnableShortcuts(Lifetime enabledLifetime)
        {
            var popupWindowLifetimeDefinition = Lifetime.Define(enabledLifetime, "PresentationAssistant::PopupWindow");

            var window = new PresentationAssistantWindow();
            window.Owner = mainWindow.MainWpfWindow.Value;


            theming.PopulateResourceDictionary(popupWindowLifetimeDefinition.Lifetime, window.Resources);

            var popupWindow = new FadingWpfPopupWindow(popupWindowLifetimeDefinition, context, context.Mutex,
                popupWindowManager, window, opacity: 0.8);

            var visibilityLifetimes = new SequentialLifetimes(popupWindowLifetimeDefinition.Lifetime);

            showAction = shortcut =>
            {
                window.SetShortcut(shortcut);
                popupWindow.ShowWindow();

                visibilityLifetimes.DefineNext(visibleLifetime =>
                {
                    // Hide the window after a timespan, and queue it on the visible sequential lifetime so
                    // that the timer is cancelled when we want to show a new shortcut. Don't hide the window
                    // by attaching it to the sequential lifetime, as that will cause the window to hide when
                    // we need to show a new shortcut. But, make sure we do terminate the lifetime so that
                    // the topmost timer hack is stopped when this window is not visible
                    threading.TimedActions.Queue(visibleLifetime.Lifetime, "PresentationAssistantWindow::HideWindow",
                        () => popupWindow.HideWindow(), VisibleTimeSpan,
                        TimedActionsHost.Recurrence.OneTime, Rgc.Invariant);
                });
            };

            enabledLifetime.OnTermination(() =>
            {
                showAction = _ => { };
            });
        }
    }
}