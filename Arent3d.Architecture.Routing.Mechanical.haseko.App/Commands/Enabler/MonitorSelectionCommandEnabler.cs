using Arent3d.Architecture.Routing.AppBase.Commands.Enabler ;

namespace Arent3d.Architecture.Routing.Mechanical.haseko.App.Commands.Enabler
{
  public class MonitorSelectionCommandEnabler : MonitorSelectionCommandEnablerBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}