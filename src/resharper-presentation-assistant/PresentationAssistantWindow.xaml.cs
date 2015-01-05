using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public partial class PresentationAssistantWindow
    {
        private static readonly TextStyle Bold = new TextStyle(System.Drawing.FontStyle.Bold);

        public PresentationAssistantWindow()
        {
            InitializeComponent();
        }

        public void SetShortcut(Shortcut shortcut)
        {
            var richText = new RichText();
            richText.Append(shortcut.Text, Bold);
            if (!string.IsNullOrEmpty(shortcut.VsShortcut))
            {
                richText.Append(" via ", TextStyle.Default);
                richText.Append(shortcut.VsShortcut);
                if (!string.IsNullOrEmpty(shortcut.IntellijShortcut) && shortcut.VsShortcut != shortcut.IntellijShortcut)
                    richText.Append(string.Format(" ({0} for IntelliJ)", shortcut.IntellijShortcut));
            }

            DataContext = richText;
        }
    }
}
