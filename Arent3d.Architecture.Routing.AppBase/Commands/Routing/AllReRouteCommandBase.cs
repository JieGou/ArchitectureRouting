using System.Collections.Generic ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [Image( "resources/RerouteAll.png" )]
  public abstract class AllReRouteCommandBase : RoutingCommandBase
  {
    protected override IEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegmentsInTransaction( UIDocument uiDocument )
    {
      return uiDocument.Document.CollectRoutes().ToSegmentsWithName() ;
    }
  }
}