using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Platform.VisualStudio.SinceVs11.Zones;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant.VisualStudio
{
    [ZoneMarker]
    public class ZoneMarker : IRequire<ISinceVs11FrontEnvZone>
    {
    }
}
