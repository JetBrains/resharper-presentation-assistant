using System;
using System.Windows;
using System.Windows.Media.Animation;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Application.UI.WindowManagement;
using JetBrains.Lifetimes;
using JetBrains.UI.PopupLayout;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class FadingWpfPopupWindow : WpfPopupWindow
    {
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(300);

        private readonly Window window;
        private readonly double opacity;

        public FadingWpfPopupWindow(LifetimeDefinition lifetimeDefinition, IPopupWindowContext context,
                                    PopupWindowMutex mutex, PopupWindowManager popupWindowManager,
                                    Window window, double opacity, HideFlags hideFlags = HideFlags.None)
            : base(lifetimeDefinition, context, mutex, popupWindowManager, window, hideFlags)
        {
            this.window = window;
            this.opacity = opacity;
            window.AllowsTransparency = true;
        }

        protected override void ShowWindowCore()
        {
            window.Opacity = 0;
            window.Visibility = Visibility.Visible;

            var animation = new DoubleAnimation(opacity, Duration);
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        protected override void HideWindowCore()
        {
            var animation = new DoubleAnimation(0, Duration);

            EventHandler handler = null;
            handler = (s, e) =>
            {
                window.Hide();
                FireClosed();
                animation.Completed -= handler;
            };
            animation.Completed += handler;
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}