using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.UI.RichText;

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
                    VsShortcuts = new []{new ShortcutSequence(new ShortcutDetails("F11", KeyboardModifiers.Shift | KeyboardModifiers.Alt))},
                    IntellijShortcuts = new[] { new ShortcutSequence(new ShortcutDetails("F7", KeyboardModifiers.Control | KeyboardModifiers.Shift)) }
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
                    VsShortcuts = new[] { new ShortcutSequence(ctrlR, new ShortcutDetails("T")) },
                    IntellijShortcuts = new[] { new ShortcutSequence(ctrlR, new ShortcutDetails("T")) }
                };
            }
        }

        public static Shortcut MultipleShortcut
        {
            get
            {
                var ctrlR = new ShortcutDetails("R", KeyboardModifiers.Control);
                return new Shortcut()
                {
                    Text = "Rename",
                    VsShortcuts = new []{ new ShortcutSequence(ctrlR, new ShortcutDetails("R")), new ShortcutSequence(ctrlR, ctrlR) },
                    IntellijShortcuts = new []{ new ShortcutSequence(new ShortcutDetails("F2")), new ShortcutSequence(new ShortcutDetails("F6", KeyboardModifiers.Shift)) }
                };
            }
        }
    }
}
