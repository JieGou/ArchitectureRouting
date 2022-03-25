using Arent3d.Architecture.Routing.AppBase.Commands.Enabler ;

namespace Arent3d.Architecture.Routing.Mechanical.Haseko.App.Commands.Enabler
{
  public class MonitorSelectionCommandEnabler : MonitorSelectionCommandEnablerBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}