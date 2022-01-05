using System.Collections.Generic ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AllReRouteCommandBase : RoutingCommandBaseWithoutOperation
  {
    protected abstract AddInType GetAddInType() ;

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      return document.CollectRoutes( GetAddInType() ).ToSegmentsWithName().EnumerateAll() ;
    }
  }
}