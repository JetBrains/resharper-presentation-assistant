using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant.VisualStudio
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioEnvZone>
    {
    }
}