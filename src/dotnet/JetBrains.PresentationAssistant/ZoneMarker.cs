using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.PresentationAssistant
{
    // TODO: Proper zone requirements
    [ZoneMarker]
    public class ZoneMarker : IRequire<DaemonZone>, IRequire<IUIInteractiveEnvZone>
    {
    }
}