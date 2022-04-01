using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Manager ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class ReRouteByFloorCommandBase : RoutingCommandBase<IReadOnlyCollection<ElementId>>
  {
    protected abstract AddInType GetAddInType() ;

    protected override OperationResult<IReadOnlyCollection<ElementId>> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var dialog = new GetLevel( document ) ;
      return false == dialog.ShowDialog() 
        ? OperationResult<IReadOnlyCollection<ElementId>>.Cancelled 
        : new OperationResult<IReadOnlyCollection<ElementId>>( dialog.GetSelectedLevels().Select( item => item.Id ).ToList() ) ;
    }
    
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, IReadOnlyCollection<ElementId> levelIds)
    {
      RouteGenerator.CorrectEnvelopes( document ) ;
      var allConduits = new FilteredElementCollector( document ).OfClass( typeof( Conduit ) )
        .OfCategory( BuiltInCategory.OST_Conduit )
        .AsEnumerable()
        .OfType<Conduit>();
      // get route names belong to selected level
      var routeNames = allConduits.Where( conduit => levelIds.Contains( conduit.ReferenceLevel.Id ) )
                                .GroupBy( conduit => conduit.GetRouteName() )
                                .Select( conduit => conduit.Key )
                                .ToList() ;
      var routes = document.CollectRoutes( GetAddInType() )
                                                            .ToSegmentsWithName()
                                                            .Where( segment => routeNames.Contains( segment.RouteName ) )
                                                            .EnumerateAll() ;
      return routes;
    }
  }
}