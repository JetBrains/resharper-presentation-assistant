using JetBrains.ActionManagement;
using JetBrains.Application;
using JetBrains.Application.Interop.NativeHook;
using JetBrains.DataFlow;
using JetBrains.UI;
using JetBrains.UI.Application;
using JetBrains.UI.PopupWindowManager;
using JetBrains.Util.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class PresentationAssistantPopupWindowContext : PopupWindowContext
    {
        private static readonly PopupWindowMutex mutex = new PopupWindowMutex();

        private readonly IWindowsHookManager windowsHookManager;

        public PresentationAssistantPopupWindowContext(Lifetime lifetime, IActionManager actionManager,
                                                       PopupWindowManager popupWindowManager,
                                                       IMainWindow mainWindow,
                                                       IWindowsHookManager windowsHookManager,
                                                       IIsApplicationActiveState applicationActiveState)
            : base(lifetime, actionManager)
        {
            this.windowsHookManager = windowsHookManager;
            MainWindow = mainWindow;
            PopupWindowManager = popupWindowManager;
            IsApplicationActive = applicationActiveState.IsApplicationActive;
            Mutex = mutex;
        }

        public IMainWindow MainWindow { get; private set; }
        public PopupWindowManager PopupWindowManager { get; private set; }
        public IProperty<bool> IsApplicationActive { get; private set; }
        public PopupWindowMutex Mutex { get; private set; }

        public override IPopupLayouter CreateLayouter(Lifetime lifetime)
        {
            var anchor = WindowAnchoringRect.AnchorToMainWindowSafe(lifetime, MainWindow, windowsHookManager);
            var dispositions = new[] {new Anchoring2D(Anchoring.MiddleWithin, Anchoring.FarWithin)};

            // Padding is in pixels...
            var padding = 75.0/DpiResolution.DeviceIndependent96DpiValue*DpiResolution.CurrentScreenDpi.DpiY;
            return new DockingLayouter(lifetime, anchor, dispositions, (int)padding);
        }
    }
}