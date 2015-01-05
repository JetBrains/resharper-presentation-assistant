using System;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Threading;
using JetBrains.Util.Logging;
using Microsoft.VisualStudio.Shell.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    [ShellComponent]
    public class OutputPanelLogger
    {
        private static readonly Guid ourReSharperPaneGuid = new Guid("0E290563-A718-4D15-B422-D511A162B8E2");
        private const string ReSharperPaneName = "ReSharper";

        private readonly VisualStudioPane myPane;

        public OutputPanelLogger(Lifetime lifetime, Util.Lazy.Lazy<IVsOutputWindow> vsOutputWindow, IThreading threading)
        {
            myPane = new VisualStudioPane(lifetime, ourReSharperPaneGuid, ReSharperPaneName, true, true, vsOutputWindow, threading);
        }

        #region IShellComponent Members

        #endregion

        public void Log(string message)
        {
            myPane.Log(message);
        }
    }

    internal class VisualStudioPane
    {
        private readonly Lifetime myLifetime;

        private readonly Guid myGuid;
        private readonly string myName;
        private readonly bool myInitiallyVisible;
        private readonly bool myClearOnSolutionClose;
        private readonly Util.Lazy.Lazy<IVsOutputWindow> myOutputWindow;

        private readonly IThreading myThreading;

        private IVsOutputWindowPane myVsPane;

        public VisualStudioPane([NotNull]Lifetime lifetime, Guid guid, [NotNull] string name, bool initiallyVisible, bool clearOnSolutionClose, [NotNull] Util.Lazy.Lazy<IVsOutputWindow> outputWindow, [NotNull] IThreading threading)
        {
            if (lifetime == null)
                throw new ArgumentNullException("lifetime");
            if (name == null)
                throw new ArgumentNullException("name");
            if (outputWindow == null)
                throw new ArgumentNullException("outputWindow");
            if (threading == null)
                throw new ArgumentNullException("threading");

            myLifetime = lifetime;
            myGuid = guid;
            myClearOnSolutionClose = clearOnSolutionClose;
            myOutputWindow = outputWindow;
            myThreading = threading;
            myInitiallyVisible = initiallyVisible;
            myName = name;
        }

        private void Create()
        {
            var paneGuid = myGuid;
            myThreading.Dispatcher.AssertAccess();
            myLifetime.AddBracket(() => myOutputWindow.Value.CreatePane(ref paneGuid, myName, myInitiallyVisible ? 1 : 0, myClearOnSolutionClose ? 1 : 0), () => myOutputWindow.Value.DeletePane(ref paneGuid));
        }

        private IVsOutputWindowPane GetPane()
        {
            Guid paneGuid = myGuid;
            IVsOutputWindowPane outputPane;
            int result = myOutputWindow.Value.GetPane(ref paneGuid, out outputPane);
            if (result != 0)
                return null;

            return outputPane;
        }

        public void Log(string message)
        {
            Logger.LogMessage("To " + myName + " VS pane: " + message);

            if (myVsPane == null)
            {
                myVsPane = GetPane();
                if (myVsPane == null)
                {
                    Create();
                    myVsPane = GetPane();
                }

                if (myVsPane == null)
                {
                    // Ooops, something failed, log it silently
                    Logger.LogExceptionSilently(new Exception("Unable to create output pane " + myName + "."));
                    return;
                }
            }

            myVsPane.OutputString(message + "\n");
        }
    }
}