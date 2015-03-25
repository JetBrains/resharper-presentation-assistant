using System.ComponentModel.Design;
using JetBrains.Annotations;
using JetBrains.VsIntegration.Shell.ActionManagement;
using Microsoft.VisualStudio.Shell.Interop;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    public static class Compatibility
    {
        //  ReSharper 9.1 moved this method from VsCommandHelpers to an extension method on IVsCmdNameMapping
        public static string TryMapCommandIdToVsCommandName([NotNull] this IVsCmdNameMapping mapping,
                                                            [NotNull] CommandID commandid)
        {
            return VsCommandHelpers.TryMapVsCommandIDToVsCommandName(commandid, mapping);
        }
    }
}