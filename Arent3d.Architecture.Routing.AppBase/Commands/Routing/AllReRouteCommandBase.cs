using System.Collections.Generic ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AllReRouteCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;
    protected override IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return uiDocument.Document.CollectRoutes(GetAddInType()).ToSegmentsWithName() ;
    }
  }
}