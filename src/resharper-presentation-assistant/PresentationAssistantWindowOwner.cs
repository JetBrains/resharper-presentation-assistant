using System;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Threading;
using JetBrains.UI.Application;
using JetBrains.UI.PopupWindowManager;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantWindowOwner
    {
        private static readonly TimeSpan VisibleTimeSpan = TimeSpan.FromSeconds(4);

        private readonly IThreading threading;
        private readonly PresentationAssistantPopupWindowContext context;
        private readonly PopupWindowManager popupWindowManager;
        private readonly SequentialLifetimes windowVisibilityLifetime;
        private readonly LifetimeDefinition windowLifetimeDefinition;
        private PresentationAssistantWindow window;
        private IPopupWindow popupWindow;

        public PresentationAssistantWindowOwner(Lifetime lifetime, IThreading threading,
            PresentationAssistantPopupWindowContext context, PopupWindowManager popupWindowManager)
        {
            this.threading = threading;
            this.context = context;
            this.popupWindowManager = popupWindowManager;

            // TODO: Tie this to an enable/disable option
            // This is the lifetime of the actual window, when it's terminated, the window is
            // properly closed. When the window is closed, the lifetime is terminated
            windowLifetimeDefinition = Lifetimes.Define(lifetime, "PresentationAssistant");

            // Used to queue hiding the window with a timer. If the lifetime is terminated (i.e. a new
            // window is shown) the hide is removed from the queue
            windowVisibilityLifetime = new SequentialLifetimes(windowLifetimeDefinition.Lifetime);
        }

        public void Show(Shortcut shortcut)
        {
            if (window == null)
            {
                window = new PresentationAssistantWindow();
                window.SetOwner(context.MainWindow.Handle);
                popupWindow = new FadingWpfPopupWindow(windowLifetimeDefinition, context, context.Mutex,
                    popupWindowManager, window, opacity: 0.8);
            }

            windowVisibilityLifetime.DefineNext((ld, l) =>
            {
                window.SetShortcut(shortcut);
                popupWindow.ShowWindow();

                threading.TimedActions.Queue(l, "PresentationAssistantWindow", () => popupWindow.HideWindow(),
                    VisibleTimeSpan, TimedActionsHost.Recurrence.OneTime, Rgc.Invariant);
            });
        }
    }
}