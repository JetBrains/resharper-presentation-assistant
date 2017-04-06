using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Interop.NativeHook;
using JetBrains.DataFlow;
using JetBrains.UI;
using JetBrains.UI.Application;
using JetBrains.UI.PopupWindowManager;
using JetBrains.UI.Utils;
using JetBrains.Util.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantPopupWindowContext : PopupWindowContext
    {
        private static readonly PopupWindowMutex PopupWindowMutex = new PopupWindowMutex();

        private readonly IWindowsHookManager windowsHookManager;
        private readonly IMainWindow mainWindow;

        public PresentationAssistantPopupWindowContext(Lifetime lifetime, IActionManager actionManager,
                                                       IMainWindow mainWindow, IWindowsHookManager windowsHookManager)
            : base(lifetime, actionManager)
        {
            this.windowsHookManager = windowsHookManager;
            this.mainWindow = mainWindow;
            Mutex = PopupWindowMutex;
        }

        public PopupWindowMutex Mutex { get; private set; }

        public override IPopupLayouter CreateLayouter(Lifetime lifetime)
        {
            var anchor = WindowAnchoringRect.AnchorToPrimaryMainWindowSafe(lifetime, mainWindow, windowsHookManager);
            var dispositions = new[] {new Anchoring2D(Anchoring.MiddleWithin, Anchoring.FarWithin)};

            // Padding is in pixels...
            unsafe
            {
                var dpi = DpiResolutions.FromHWnd((void*) mainWindow.GetPrimaryWindow().Handle);
                var padding = 75.0/DpiResolution.DeviceIndependent96DpiValue*dpi.DpiY;
                return new DockingLayouter(lifetime, anchor, dispositions, (int)padding);
            }
        }
    }
}