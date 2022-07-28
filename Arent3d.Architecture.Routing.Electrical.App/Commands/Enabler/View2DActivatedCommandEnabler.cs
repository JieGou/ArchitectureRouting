using Arent3d.Architecture.Routing.AppBase.Commands.Enabler ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Enabler
{
  public class View2DActivatedCommandEnabler : View2DActivatedCommandEnablerBase
  {
    protected override AddInType GetAddInType() => AppCommandSettings.AddInType ;
  }
}