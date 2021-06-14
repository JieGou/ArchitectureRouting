using Arent3d.Architecture.Routing.AppBase.Commands.Enabler ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Enabler
{
  public class MonitorSelectionCommandEnabler : MonitorSelectionCommandEnablerBase
  {
    protected override AddInType GetAddInType()
    {
      return AddInType.Electrical;
    }
  }
}