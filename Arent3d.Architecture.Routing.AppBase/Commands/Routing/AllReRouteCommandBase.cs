using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AllReRouteCommandBase : RoutingCommandBase<AllReRouteCommandBase.ReRouteState>
  {
    public record ReRouteState( Dictionary<string, HashSet<string>> AllConduitsByRoute, Dictionary<string, string> RouteNameDictionary ) ;
    
    protected abstract AddInType GetAddInType() ;
    
    protected override OperationResult<ReRouteState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      var routeNames = allConduits.Where( conduit => ! string.IsNullOrEmpty( conduit.GetRouteName() ) )
        .GroupBy( conduit => conduit.GetRouteName()  ).Select( conduit => conduit.Key ).ToList() ;
      var allConduitsByRoute = 
        routeNames.Any() ?
        document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( e => routeNames.Contains( e.GetRouteName() ! ) ).GroupBy( e => e.GetRouteName() ! )
          .ToDictionary( d => d.Key, d => d.Select( e => e.UniqueId ).ToHashSet() ) 
        : new Dictionary<string, HashSet<string>>() ;
      return new OperationResult<ReRouteState>( new ReRouteState( allConduitsByRoute, new Dictionary<string, string>() ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, ReRouteState reRouteState )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      var allRoutes = document.CollectRoutes( GetAddInType() ).ToList() ;
      var routeNames = allRoutes.Select( r => r.RouteName ).Distinct() ;
      RouteGenerator.GetRelatedBranchRouteNames( document, routeNames, reRouteState.RouteNameDictionary ) ;
      return allRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
  }
}