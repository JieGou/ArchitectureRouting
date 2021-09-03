using System.Collections.Generic ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AllReRouteCommandBase : RoutingCommandBase
  {
    protected abstract AddInType GetAddInType() ;

    protected override IAsyncEnumerable<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      return document.CollectRoutes( GetAddInType() ).ToSegmentsWithName().ToAsyncEnumerable() ;
    }
  }
}