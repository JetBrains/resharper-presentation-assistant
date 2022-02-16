using System.Drawing;
using JetBrains.Application;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.Platform.VisualStudio.SinceVs11.Shell.Theming;
using Microsoft.VisualStudio.Shell;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    // The colours we're defining, with default colours
    public class PresentationAssistantThemeColor : ThemeColor
    {
        public static ThemeResourceKey AccentLightBrushKey = BundledThemeColors.Environment.AccentLightBrushKey;
        public static ThemeResourceKey PanelTextBrushKey = BundledThemeColors.Environment.PanelTextBrushKey;
        public static ThemeResourceKey AccentBorderBrushKey = BundledThemeColors.Environment.AccentBorderBrushKey;

        public static readonly PresentationAssistantThemeColor PresentationAssistantWindowBorder =
            new PresentationAssistantThemeColor("PresentationAssistantWindowBorder", Color.FromArgb(0xD1, 0xD1, 0xD1));
        public static readonly PresentationAssistantThemeColor PresentationAssistantWindowBackground =
            new PresentationAssistantThemeColor("PresentationAssistantWindowBackground", Color.FromArgb(0xBA, 0xEE, 0xBA));
        public static readonly PresentationAssistantThemeColor PresentationAssistantWindowForeground =
            new PresentationAssistantThemeColor("PresentationAssistantWindowForeground", Color.Black);

        // Key to the colour brush in the ResourceDictionary populated by ITheming.PopulateResourceDictionary
        // WPF uses this to bind against a brush, rather than a colour
        public static readonly object PresentationAssistantWindowBorderBrushKey = PresentationAssistantWindowBorder.BrushKey;
        public static readonly object PresentationAssistantWindowBackgroundBrushKey = PresentationAssistantWindowBackground.BrushKey;
        public static readonly object PresentationAssistantWindowForegroundBrushKey = PresentationAssistantWindowForeground.BrushKey;

        private PresentationAssistantThemeColor(string name, Color defaultColour)
            : base(name, defaultColour, false, true)
        {
        }
    }

    [ShellComponent]
    public class PresentationAssistantThemeColourFiller : IThemeColorFiller
    {
        public virtual void FillColorTheme(ColorTheme theme)
        {
            //if (isDarkTheme.Value)
            //    FillDarkTheme(theme);
            //else
                FillLightTheme(theme);
        }

/*
        private void FillDarkTheme(ColorTheme theme)
        {
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBorder, Color.Black);
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBackground, Color.FromArgb(0x49, 0x75, 0x49));
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowForeground, Color.Black);
        }
*/

        private void FillLightTheme(ColorTheme theme)
        {
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBorder, Color.FromArgb(0xD1, 0xD1, 0xD1));
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBackground, Color.FromArgb(0xBA, 0xEE, 0xBA));
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowForeground, Color.Black);
        }
    }
}
