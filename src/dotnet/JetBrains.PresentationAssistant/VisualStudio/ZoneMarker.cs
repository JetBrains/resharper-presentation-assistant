using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.VisualStudio.SinceVs11.Shell.Zones;
using JetBrains.VsIntegration.Shell.Zones;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant.VisualStudio
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<IVisualStudioFrontendEnvZone>, IRequire<ISinceVs11FrontEnvZone>
    {
    }
}
