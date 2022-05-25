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
  public abstract class AllReRouteCommandBase : RoutingCommandBase<Dictionary<string, HashSet<string>>>
  {
    protected abstract AddInType GetAddInType() ;
    
    protected override OperationResult<Dictionary<string, HashSet<string>>> OperateUI( ExternalCommandData commandData, ElementSet elements )
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
      return new OperationResult<Dictionary<string, HashSet<string>>>( allConduitsByRoute ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, Dictionary<string, HashSet<string>> allConduitsByRoute )
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      return document.CollectRoutes( GetAddInType() ).ToSegmentsWithName().EnumerateAll() ;
    }
  }
}