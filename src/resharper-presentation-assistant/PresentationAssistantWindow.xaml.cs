using JetBrains.ActionManagement;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public partial class PresentationAssistantWindow
    {
        public PresentationAssistantWindow()
        {
            InitializeComponent();
        }

        public void SetShortcut(Shortcut shortcut)
        {
            DataContext = shortcut;
        }
    }

    public static class SampleData
    {
        public static Shortcut SimpleShortcut
        {
            get
            {
                return new Shortcut
                {
                    Text = "Highlight Usages in File",
                    VsShortcut = new ShortcutSequence(new ShortcutDetails("F11", KeyboardModifiers.Shift | KeyboardModifiers.Alt)),
                    IntellijShortcut = new ShortcutSequence(new ShortcutDetails("F7", KeyboardModifiers.Control | KeyboardModifiers.Shift))
                };
            }
        }

        public static Shortcut ChordShortcut
        {
            get
            {
                var ctrlR = new ShortcutDetails("R", KeyboardModifiers.Control);
                return new Shortcut
                {
                    Text = "Run Unit Tests",
                    VsShortcut = new ShortcutSequence(ctrlR, new ShortcutDetails("T")),
                    IntellijShortcut = new ShortcutSequence(ctrlR, new ShortcutDetails("T"))
                };
            }
        }

        public static Shortcut MultiplierShortcut
        {
            get
            {
                return new Shortcut
                {
                    Text = "Highlight Usages in File",
                    VsShortcut = new ShortcutSequence(new ShortcutDetails("F11", KeyboardModifiers.Shift | KeyboardModifiers.Alt)),
                    IntellijShortcut = new ShortcutSequence(new ShortcutDetails("F7", KeyboardModifiers.Control | KeyboardModifiers.Shift)),
                    Multiplier = 2
                };
            }
        }
    }
}
