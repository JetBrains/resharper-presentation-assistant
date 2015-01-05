using System;
using System.Windows;
using System.Windows.Media.Animation;
using JetBrains.DataFlow;
using JetBrains.UI.PopupWindowManager;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class FadingWpfPopupWindow : WpfPopupWindow
    {
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

            var fadeTime = TimeSpan.FromMilliseconds(100);
            var animation = new DoubleAnimation(opacity, fadeTime);
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        protected override void HideWindowCore()
        {
            var fadeTime = TimeSpan.FromMilliseconds(100);
            var animation = new DoubleAnimation(0, fadeTime);

            EventHandler handler = null;
            handler = (s, e) =>
            {
                window.Hide();
                animation.Completed -= handler;
            };
            animation.Completed += handler;
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}