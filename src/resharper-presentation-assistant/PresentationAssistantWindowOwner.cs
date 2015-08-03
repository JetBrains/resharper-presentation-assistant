using System;
using System.Runtime.InteropServices;
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
        private Action<Shortcut> showAction;

        public PresentationAssistantWindowOwner(Lifetime lifetime, IThreading threading,
            PresentationAssistantPopupWindowContext context, PopupWindowManager popupWindowManager, ITheming theming)
        {
            this.threading = threading;
            this.context = context;
            this.popupWindowManager = popupWindowManager;
            this.theming = theming;

            Enabled = new Property<bool>(lifetime, "PresentationAssistantWindow::Enabled");
            Enabled.WhenTrue(lifetime, EnableShortcuts);
        }

        public IProperty<bool> Enabled { get; private set; }

        public void Show(Shortcut shortcut)
        {
            showAction(shortcut);
        }

        private void EnableShortcuts(Lifetime enabledLifetime)
        {
            var popupWindowLifetimeDefinition = Lifetimes.Define(enabledLifetime, "PresentationAssistant::PopupWindow");

            var window = new PresentationAssistantWindow();
            var windowInteropHelper = new WindowInteropHelper(window) { Owner = context.MainWindow.Handle };

            theming.PopulateResourceDictionary(popupWindowLifetimeDefinition, window.Resources);

            var popupWindow = new FadingWpfPopupWindow(popupWindowLifetimeDefinition, context, context.Mutex,
                popupWindowManager, window, opacity: 0.8);

            var visibilityLifetimes = new SequentialLifetimes(popupWindowLifetimeDefinition);

            showAction = shortcut =>
            {
                window.SetShortcut(shortcut);
                popupWindow.ShowWindow();

                visibilityLifetimes.DefineNext((visibleLifetimeDefinition, visibleLifetime) =>
                {
                    // HACK! We need to keep our window on top of everything (even on top of JetPopupMenu) and
                    // there's no way of influencing the layout of other popups, so we have to rather crudely
                    // force ourselves topmost
                    // Queue this on the visibility lifetime
                    //threading.TimedActions.Queue(visibleLifetime, "PresentationAssistantWindow::Topmost",
                    //    () => EnsureTopmost(windowInteropHelper.Handle), TopmostTimeSpan,
                    //    TimedActionsHost.Recurrence.Recurring, Rgc.Invariant);

                    // Hide the window after a timespan, and queue it on the visible sequential lifetime so
                    // that the timer is cancelled when we want to show a new shortcut. Don't hide the window
                    // by attaching it to the sequential lifetime, as that will cause the window to hide when
                    // we need to show a new shortcut. But, make sure we do terminate the lifetime so that
                    // the topmost timer hack is stopped when this window is not visible
                    threading.TimedActions.Queue(visibleLifetime, "PresentationAssistantWindow::HideWindow",
                        () =>
                        {
                            popupWindow.HideWindow();
                            //visibleLifetimeDefinition.Terminate();
                        }, VisibleTimeSpan,
                        TimedActionsHost.Recurrence.OneTime, Rgc.Invariant);
                });
            };

            enabledLifetime.AddAction(() =>
            {
                showAction = _ => { };
            });
        }

        //private static void EnsureTopmost(IntPtr handle)
        //{
        //    if (IsObscured(handle))
        //        MakeTopmost(handle);
        //}

        //private static void MakeTopmost(IntPtr handle)
        //{
        //    Win32Declarations.SetWindowPos(handle, (IntPtr)HwndSpecial.HWND_TOP, 0, 0, 0, 0,
        //        SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);
        //}

        //private static bool IsObscured(IntPtr handle)
        //{
        //    var dc = Win32Declarations.GetDC(handle);
        //    try
        //    {
        //        RECT rcClip, rcClient = RECT.Empty;
        //        var clipbox = GetClipBox(dc, out rcClip);
        //        Win32Declarations.GetClientRect(handle, ref rcClient);

        //        switch (clipbox)
        //        {
        //            case GetClipBoxReturn.Error:
        //                return false;
        //            case GetClipBoxReturn.NullRegion:
        //                return true;   // Window is hidden
        //            case GetClipBoxReturn.SimpleRegion:
        //                if (rcClip.left == rcClient.left && rcClip.right == rcClient.right && rcClip.top == rcClient.top && rcClip.bottom == rcClient.bottom)
        //                    return false;
        //                return true;
        //            case GetClipBoxReturn.ComplexRegion:
        //                return true;
        //            default:
        //                throw new ArgumentOutOfRangeException();
        //        }
        //    }
        //    finally
        //    {
        //        Win32Declarations.ReleaseDC(handle, dc);
        //    }
        //}

        //[DllImport("gdi32.dll")]
        //public static extern GetClipBoxReturn GetClipBox(IntPtr hdc, out RECT lpRect);

        //public enum GetClipBoxReturn : int
        //{
        //    Error = 0,
        //    NullRegion = 1,
        //    SimpleRegion = 2,
        //    ComplexRegion = 3
        //}
    }
}