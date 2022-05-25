using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AllReRouteByFloorCommandBase : RoutingCommandBase<AllReRouteByFloorCommandBase.ReRouteByFloorState>
  {
    public record ReRouteByFloorState ( IReadOnlyCollection<ElementId> LevelIds, Dictionary<string, HashSet<string>> ConduitIdsOfRoute ) ;
    protected abstract AddInType GetAddInType() ;

    protected override OperationResult<ReRouteByFloorState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var dialog = new GetLevel( document ) ;
      if ( false == dialog.ShowDialog() ) return OperationResult<ReRouteByFloorState>.Cancelled ;
      var levelIds = dialog.GetSelectedLevels().Select( item => item.Id ).ToList() ;
      var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      // get route names belong to selected level
      var routeNames = allConduits.Where( conduit => levelIds.Contains( conduit.ReferenceLevel.Id ) )
        .GroupBy( conduit => conduit.GetRouteName() ).Select( conduit => conduit.Key ).ToList() ;
      var allConduitsByRoute = document.GetAllElements<Element>()
        .OfCategory( BuiltInCategorySets.Conduits )
        .Where( e => routeNames.Contains( e.GetRouteName() ! ) )
        .GroupBy( e => e.GetRouteName() ! )
        .ToDictionary( d => d.Key, d => d.Select( e => e.UniqueId ).ToHashSet() ) ;
      return new OperationResult<ReRouteByFloorState>( new ReRouteByFloorState( levelIds, allConduitsByRoute ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments(
      Document document, ReRouteByFloorState reRouteByFloorState )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      var (levelIds, _) = reRouteByFloorState ;
      var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit ).AsEnumerable().OfType<Conduit>() ;
      // get route names belong to selected level
      var routeNames = allConduits.Where( conduit => levelIds.Contains( conduit.ReferenceLevel.Id ) )
        .GroupBy( conduit => conduit.GetRouteName() ).Select( conduit => conduit.Key ).ToList() ;
      var routes = document.CollectRoutes( GetAddInType() ).ToSegmentsWithName()
        .Where( segment => routeNames.Contains( segment.RouteName ) ).EnumerateAll() ;
      return routes ;
    }
  }
}