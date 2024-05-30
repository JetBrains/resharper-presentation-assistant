using System;
using System.Windows;
using JetBrains.Application.UI.CrossFramework;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Application.UI.WindowManagement;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.UI.PopupLayout;
using JetBrains.UI.Utils;
using JetBrains.Util.Media;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class WpfPopupWindow : BasePopupWindow
    {
        private readonly Window window;
        private PopupWindowLayoutMode layoutMode;

        protected WpfPopupWindow(LifetimeDefinition lifetimeDefinition, IPopupWindowContext context,
                                 PopupWindowMutex mutex, PopupWindowManager popupWindowManager,
                                 Window window, HideFlags hideFlags = HideFlags.None)
            : base(lifetimeDefinition, context, mutex, hideFlags)
        {
            this.window = window;

            UpdatePopupLayout();
            AttachEvents(popupWindowManager);
        }

        // Use the desired size to calculate a new layout
        // Called from:
        // * ctor (although unlikely to be valid)
        // * OnSourceInitialized
        // * Before show
        // * On size changed
        private void UpdatePopupLayout()
        {
            if (!window.IsMeasureValid)
                window.Measure(new Size(double.MaxValue, double.MaxValue));

            if (window.IsMeasureValid)
            {
                // Setting this causes the Layouter to compute a new LayoutResult and
                // we get notified via OnLayouterResultChanged
                var dpiResolution = DpiResolutions.FromAvalonElement(window);
                Layouter.Size.Value = window.DesiredSize.ToWinFormsSize(dpiResolution).ToJetPhysicalSize();
                // TODO: Should we check to see if the layouter gives us a different size, then re-measure?
            }
        }

        // Note that this can be called if e.g. the Layouter's AnchoringRect changes
        protected override void OnLayouterResultChanged(PropertyChangedEventArgs<LayoutResult> args)
        {
            var dpiResolution = DpiResolutions.FromAvalonElement(window);
            var location = args.New.Bounds.ToWinFormsRectangle().ToAvalonRect(dpiResolution);
            window.Top = location.Top;
            window.Left = location.Left;
        }

        protected override void AttachWindowEvents()
        {
            window.SizeChanged += OnWindowSizeChanged;
            window.Closed += OnWindowClosed;
            window.Deactivated += OnWindowDeactivated;
            window.SourceInitialized += OnWindowSourceInitialized;
        }

        protected override void DetachWindowEvents()
        {
            window.SizeChanged -= OnWindowSizeChanged;
            window.Closed -= OnWindowClosed;
            window.Deactivated -= OnWindowDeactivated;
            window.SourceInitialized -= OnWindowSourceInitialized;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePopupLayout();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            FireClosed();
            Dispose();
        }

        private void OnWindowDeactivated(object sender, EventArgs e)
        {
            OnWindowFocusLoss();
        }

        private void OnWindowSourceInitialized(object sender, EventArgs e)
        {
            UpdatePopupLayout();
        }

        protected override void ShowWindowCore()
        {
            UpdatePopupLayout();
            window.Show();
        }

        protected override void HideWindowCore()
        {
            window.Hide();
            FireClosed();
        }

        protected override void CloseWindowCore()
        {
            window.Close();
            FireClosed();
        }

        protected override void FocusWindow()
        {
            window.Focus();
        }

        public override PopupWindowLayoutMode LayoutMode
        {
            get { return layoutMode; }
            set
            {
                if (layoutMode == value)
                    return;

                layoutMode = value;

                if (layoutMode == PopupWindowLayoutMode.Full)
                    UpdatePopupLayout();
            }
        }

        public override bool Visible => !IsDisposed && window != null && window.IsVisible;
    }
}
