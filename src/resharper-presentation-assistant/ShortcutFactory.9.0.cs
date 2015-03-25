using JetBrains.ReSharper.Features.Navigation.Features.GoToDeclaration;
using JetBrains.ReSharper.Features.Navigation.Features.GoToImplementation;
using JetBrains.UI.ActionsRevised.Loader;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public partial class ShortcutFactory
    {
        private IActionDefWithId GetPrimaryDef(IActionDefWithId originalDef, out IActionDefWithId secondaryDef)
        {
            // The way ReSharper overrides Visual Studio's go to methods is a bit confusing.
            // Normally, an overridden command is just an action that also overrides a VS
            // command. The go to commands have both ReSharper actions and VS overriding 
            // commands. Also, the names are confusing. Visual Studio's "Go to Definition"
            // maps to ReSharper "Go to Declaration" and its "Go to Declaration" maps to
            // "Go to Implementation". The IntelliJ shortcuts are specified in the ReSharper
            // actions, and the VS shortcuts are simply what's mapped to the overridden
            // command. So, we need to handle the two shortcuts at the same time - one
            // will provide the standard name, and the IntelliJ shortcut, the other will
            // provide the details to get to the overridden VS command to give us the
            // proper bindings.
            switch (originalDef.ActionId)
            {
                case "GotoDefinitionOverride":
                    secondaryDef = originalDef;
                    return defs.GetActionDef<GotoDeclarationAction>();

                case GotoDeclarationAction.ACTION_ID:
                    secondaryDef = defs.GetActionDef<GotoDefinitionOverrideAction>();
                    return originalDef;

                case "GoToDeclarationOverride":
                    secondaryDef = originalDef;
                    return defs.GetActionDef<GotoImplementationsAction>();

                case GotoImplementationsAction.GOTO_IMPLEMENTATION_ACTION_ID:
                    secondaryDef = defs.GetActionDef<GoToDeclarationOverrideAction>();
                    return originalDef;
            }

            secondaryDef = originalDef;
            return originalDef;
        }
    }
}