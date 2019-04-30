using System;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Application.UI.WindowManagement;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.UI;
using JetBrains.UI.PopupLayout;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    // Base implementation of IPopupWindow, implementing common functionality, irrespective of what's used
    // to show the popup (e.g. Windows Forms or WPF). Windows Forms has the PopupFormController class to
    // implement IPopupWindow, but WPF only has TrackedWindow, which has issues (mucks about with transparency
    // and background even when not trying to display glass, and doesn't handle all instances of layout).
    // This class was implemented to allow WpfPopupWindow to concentrate on just handling WPF popup window
    // handling, and not all of this common stuff.
    // 
    // Things in PopupFormController that isn't implemented here:
    // * Hide popup when application isn't active
    // * Show popup when application becomes active again (PFC implementation conflicts with FormHideMethod.Closing)
    // * Handle resizing of the window/form, rather than having the contents driving the size
    public abstract class BasePopupWindow : IPopupWindow
    {
        private readonly LifetimeDefinition lifetimeDefinition;
        private readonly Lifetime lifetime;
        private readonly HideFlags hideFlags;

        protected BasePopupWindow(LifetimeDefinition lifetimeDefinition, IPopupWindowContext context,
            PopupWindowMutex mutex, HideFlags hideFlags)
        {
            this.lifetimeDefinition = lifetimeDefinition;
            lifetime = lifetimeDefinition.Lifetime;
            Context = context;
            Mutex = mutex;

            HideMethod = FormHideMethod.Visibility;

            this.hideFlags = hideFlags;

            lifetime.AddAction(() =>
            {
                if (!Visible)
                {
                    CloseWindowCore();
                    return;
                }

                EventHandler handle = null;
                handle = (sender, args) =>
                {
                    CloseWindowCore();
                    Closed -= handle;
                };
                Closed += handle;
                HideWindow();
            });
        }

        protected IPopupLayouter Layouter { get; private set; }

        protected void AttachEvents(PopupWindowManager popupWindowManager)
        {
            lifetime.AddAction(DetachEvents);

            var context = Context;
            if (context != null)
            {
                lifetime.AddBracket(() => Layouter = context.CreateLayouter(lifetime), () => Layouter = null);
                Layouter?.Layout.Change.Advise_HasNew(lifetime, OnLayouterResultChanged);

                context.AnyOtherAction += OnContextOwnerAnyActionPerformed;
                context.Scroll += OnContextOwnerScroll;
                context.SelectionChanged += OnContextOwnerSelectionChanged;
                context.Deactivated += OnContextOwnerDeactivated;
                context.EscapePressed += OnContextOwnerEscapePressed;
            }

            AttachWindowEvents();

            popupWindowManager?.PopupWindows.Add(lifetime, this);
        }

        protected abstract void OnLayouterResultChanged(PropertyChangedEventArgs<LayoutResult> args);

        private void DetachEvents()
        {
            var context = Context;
            if (context != null)
            {
                context.AnyOtherAction -= OnContextOwnerAnyActionPerformed;
                context.Scroll -= OnContextOwnerScroll;
                context.SelectionChanged -= OnContextOwnerSelectionChanged;
                context.Deactivated -= OnContextOwnerDeactivated;
                context.EscapePressed -= OnContextOwnerEscapePressed;
            }

            DetachWindowEvents();
        }

        protected abstract void AttachWindowEvents();
        protected abstract void DetachWindowEvents();

        private void OnContextOwnerAnyActionPerformed(object sender, EventArgs args)
        {
            if ((hideFlags & HideFlags.AnyOtherAction) != 0)
                HideWindow();
        }

        private void OnContextOwnerDeactivated(object sender, EventArgs args)
        {
            if ((hideFlags & HideFlags.Deactivated) != 0)
                HideWindow();
        }

        private void OnContextOwnerEscapePressed(object sender, EventArgs args)
        {
            if ((hideFlags & HideFlags.Escape) != 0)
                HideWindow();
        }

        private void OnContextOwnerScroll(object sender, EventArgs args)
        {
            if ((hideFlags & HideFlags.Scrolling) != 0)
                HideWindow();
        }

        private void OnContextOwnerSelectionChanged(object sender, EventArgs args)
        {
            if ((hideFlags & HideFlags.SelectionChanged) != 0)
                HideWindow();
        }

        protected void OnWindowFocusLoss()
        {
            if ((hideFlags & HideFlags.FocusLoss) != 0)
                HideWindow();
        }

        public virtual void Dispose()
        {
            lifetimeDefinition.Terminate();
        }

        public void HideWindow()
        {
            switch (HideMethod)
            {
                case FormHideMethod.Visibility:
                    HideWindowCore();
                    break;
                case FormHideMethod.Closing:
                    CloseWindowCore();
                    break;
                case FormHideMethod.FocusingAndClosing:
                    FocusWindow();
                    CloseWindowCore();
                    break;
            }
        }

        public virtual bool ShowWindow()
        {
            ShowWindowCore();

            // TODO
            return true;
        }

        public IPopupWindowContext Context { get; }
        public FormHideMethod HideMethod { get; set; }

        public bool IsDisposed => lifetimeDefinition.IsTerminated;

        public abstract PopupWindowLayoutMode LayoutMode { get; set; }
        public PopupWindowMutex Mutex { get; }
        public abstract bool Visible { get; }

        public event EventHandler Closed;

        protected void FireClosed()
        {
            try
            {
                Closed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        protected abstract void HideWindowCore();
        protected abstract void ShowWindowCore();
        protected abstract void CloseWindowCore();
        protected abstract void FocusWindow();
    }
}