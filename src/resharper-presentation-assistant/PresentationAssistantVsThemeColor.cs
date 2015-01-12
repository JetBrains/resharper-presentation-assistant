using System;
using JetBrains.Application;
using JetBrains.Platform.VisualStudio.SinceVs11.Shell.Theming;
using JetBrains.ReSharper.Plugins.PresentationAssistant;
using JetBrains.UI.Components.Theming;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

// Register the theme colours with Visual Studio
[assembly: RegisterThemeColor("PresentationAssistantWindowBorder", CategoryDescriptor = typeof(ReSharperPresentationAssistantCategoryDescriptor),
    DarkBackgroundColor = "Black", LightBackgroundColor = "#D1D1D1")]

[assembly: RegisterThemeColor("PresentationAssistantWindow", CategoryDescriptor = typeof(ReSharperPresentationAssistantCategoryDescriptor), 
    DarkBackgroundColor = "#497549", DarkForegroundColor = "Black", LightBackgroundColor = "#BAEEBA", LightForegroundColor = "Black")]

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public class ReSharperPresentationAssistantCategoryDescriptor : RegisterThemeColorAttribute.ICategoryDescriptor
    {
        public static readonly Guid Category = new Guid("{9FEAEEFD-3AC8-4ABD-95D2-FE44A4F9F57B}");

        public string CategoryName
        {
            get { return "ReSharperPresentationAssistant"; }
        }

        public Guid CategoryGuid
        {
            get { return Category; }
        }
    }

    // TODO: Move this to a VS 11 zone
    // This IThemeColorFiller overrides the default values with values from VS, which are
    // theme specific
    [ShellComponent]
    public class Vs11PresentationAssistantThemeColourFiller : PresentationAssistantThemeColourFiller
    {
        private readonly IVsUIShell5 vsUiShell5;

        public Vs11PresentationAssistantThemeColourFiller(IVsUIShell5 vsUIShell5)
        {
            vsUiShell5 = vsUIShell5;
        }

        public override void FillColorTheme(ColorTheme theme)
        {
            // Get the static defaults
            base.FillColorTheme(theme);

            // Override with the values from Visual Studio's Fonts and Colours dialog
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBorder,
                VS11ThemeManager.GetThemedGDIColor(vsUiShell5, PresentationAssistantVsColours.BorderColourKey));
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowBackground,
                VS11ThemeManager.GetThemedGDIColor(vsUiShell5, PresentationAssistantVsColours.BackgroundColourKey));
            theme.SetGDIColor(PresentationAssistantThemeColor.PresentationAssistantWindowForeground,
                VS11ThemeManager.GetThemedGDIColor(vsUiShell5, PresentationAssistantVsColours.ForegroundColourKey));
        }

        private static class PresentationAssistantVsColours
        {
            // This is the same as ReSharperColors.Category and RegisterReSharperThemeColorAttribute.CategoryDescriptor
            // But ReSharperColors is in a SinceVs11 assembly which is hard to reference
            private static readonly Guid Category = new Guid("{9FEAEEFD-3AC8-4ABD-95D2-FE44A4F9F57B}");

            private static ThemeResourceKey borderColourKey;
            private static ThemeResourceKey backgroundColourKey;
            private static ThemeResourceKey foregroundColourKey;

            public static ThemeResourceKey BorderColourKey
            {
                get
                {
                    return borderColourKey ??
                           (borderColourKey = new ThemeResourceKey(Category, "PresentationAssistantWindowBorder", ThemeResourceKeyType.BackgroundColor));
                }
            }

            public static ThemeResourceKey BackgroundColourKey
            {
                get
                {
                    return backgroundColourKey ??
                           (backgroundColourKey = new ThemeResourceKey(Category, "PresentationAssistantWindow", ThemeResourceKeyType.BackgroundColor));
                }
            }
            public static ThemeResourceKey ForegroundColourKey
            {
                get
                {
                    return foregroundColourKey ??
                           (foregroundColourKey = new ThemeResourceKey(Category, "PresentationAssistantWindow", ThemeResourceKeyType.ForegroundColor));
                }
            }
        }
    }
}