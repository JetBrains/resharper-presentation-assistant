using System;
using System.Windows.Interop;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Interop.WinApi;
using JetBrains.Threading;
using JetBrains.UI.PopupWindowManager;
using JetBrains.UI.Theming;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantWindowOwner
    {
        private static readonly TimeSpan VisibleTimeSpan = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan TopmostTimeSpan = TimeSpan.FromMilliseconds(200);

        private readonly IThreading threading;
        private readonly PresentationAssistantPopupWindowContext context;
        private readonly PopupWindowManager popupWindowManager;
        private readonly ITheming theming;
        private readonly SequentialLifetimes windowVisibilityLifetime;
        private readonly LifetimeDefinition windowLifetimeDefinition;
        private PresentationAssistantWindow window;
        private WindowInteropHelper windowInteropHelper;
        private IPopupWindow popupWindow;

        public PresentationAssistantWindowOwner(Lifetime lifetime, IThreading threading,
            PresentationAssistantPopupWindowContext context, PopupWindowManager popupWindowManager, ITheming theming)
        {
            this.threading = threading;
            this.context = context;
            this.popupWindowManager = popupWindowManager;
            this.theming = theming;

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
                windowInteropHelper = new WindowInteropHelper(window) {Owner = context.MainWindow.Handle};

                theming.PopulateResourceDictionary(windowLifetimeDefinition.Lifetime, window.Resources);
                popupWindow = new FadingWpfPopupWindow(windowLifetimeDefinition, context, context.Mutex,
                    popupWindowManager, window, opacity: 0.8);
            }

            windowVisibilityLifetime.DefineNext((ld, l) =>
            {
                window.SetShortcut(shortcut);
                popupWindow.ShowWindow();

                // HACK! We need to keep our window on top of everything (even on top of JetPopupMenu) and
                // there's no way of influencing the layout of other popups, so we have to rather crudely
                // force ourselves topmost
                threading.TimedActions.Queue(l, "PresentationAssistantWindow::Topmost", () => MakeTopmost(windowInteropHelper.Handle),
                    TopmostTimeSpan, TimedActionsHost.Recurrence.Recurring, Rgc.Invariant);

                threading.TimedActions.Queue(l, "PresentationAssistantWindow::HideWindow", () => popupWindow.HideWindow(),
                    VisibleTimeSpan, TimedActionsHost.Recurrence.OneTime, Rgc.Invariant);
            });
        }

        private static void MakeTopmost(IntPtr handle)
        {
            Win32Declarations.SetWindowPos(handle, (IntPtr)HwndSpecial.HWND_TOP, 0, 0, 0, 0,
                SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        }
    }
}