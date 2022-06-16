using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using Autodesk.Revit.DB.Electrical ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseSelectedPullBoxCommandBase : RoutingCommandBase<EraseSelectedPullBoxCommandBase.PickState>
  {
    public record PickState(IEnumerable<IEndPoint> FromEndPoints,IEnumerable<IEndPoint> ToEndPoints, List<Route> RoutesRelatedPullBox, Element PullBox) ;
    protected abstract AddInType GetAddInType() ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      PullPoxPickFilter detailSymbolFilter = new() ;
      var pickedPullBox = uiDocument.Selection.PickObject( ObjectType.Element, detailSymbolFilter ) ;
      var elementPullBox = document.GetElement( pickedPullBox?.ElementId ) ;

      //Get information to reroute
      var routes = document.CollectRoutes( GetAddInType()) ;
      var routesRelatedPullBox = GetRouteRelatedPullBox( routes, elementPullBox ).ToList() ;
      var routeSegments = GetAllRouteSegment(routesRelatedPullBox).ToList() ; ;
      var (fromEndPoints, toEndPoints) = GetEndPoints( routeSegments ) ;
      var newFromEndPoints = fromEndPoints.Where( x => ( x as ConnectorEndPoint )?.EquipmentUniqueId != elementPullBox.UniqueId ) ;
      var newToEndPoints = toEndPoints.Where( x => ( x as ConnectorEndPoint )?.EquipmentUniqueId != elementPullBox.UniqueId ) ;
      
      return new OperationResult<PickState>( new PickState(newFromEndPoints, newToEndPoints, routesRelatedPullBox,  elementPullBox) ) ;
    }
    
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (newFromEndPoints, newToEndPoints, routesRelatedPullBox, elementPullBox) = pickState ;
      var diameter = routesRelatedPullBox.First().UniqueDiameter ;
      var classificationInfo = routesRelatedPullBox.First().GetSystemClassificationInfo() ;
      var systemType = routesRelatedPullBox.First().GetMEPSystemType() ;
      var curveType = routesRelatedPullBox.First().UniqueCurveType ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = routesRelatedPullBox.First().UniqueIsRoutingOnPipeSpace ?? false ;
      var avoidType = routesRelatedPullBox.First().UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = routesRelatedPullBox.First().UniqueShaftElementUniqueId ;
      var fromFixedHeight = routesRelatedPullBox.First().UniqueFromFixedHeight ;
      var toFixedHeight = routesRelatedPullBox.First().UniqueToFixedHeight ;

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType! ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;
      
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      result.AddRange( newFromEndPoints.Zip( newToEndPoints, ( f, t ) =>
      {
        RenameRoutePassPoint( document, name, f, t ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, f, t, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
        return ( name , segment ) ;
      } ) ) ;
      
      result.AddRange( GetSelectedRouteSegments( document, routesRelatedPullBox ) );
      
      //Delete pull box
      document.Delete( elementPullBox.Id ) ;
      
      return result ;
    }
    
    private void RenameRoutePassPoint( Document document, string name, IEndPoint fromEndPoint, IEndPoint toEndPoint) 
    {
      var fromEndPointKey = fromEndPoint.Key ;
      var toEndPointKey   = toEndPoint.Key ;
      var fromPassPoint = document.GetElementById<Instance>( fromEndPointKey.GetElementUniqueId() ) ;
      var toPassPoint = document.GetElementById<Instance>( toEndPointKey.GetElementUniqueId() ) ;
      if ( fromPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() ) {
        fromPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ; 
      }
      if ( toPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() ) {
        toPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ;
      }
    }
    
    private IEnumerable<RouteSegment> GetAllRouteSegment( IEnumerable<Route> routesRelatedPullBox )
    {
      foreach ( var routeRelatedPullBox in routesRelatedPullBox ) {
        foreach ( var routeSegment in routeRelatedPullBox.RouteSegments ) {
          yield return routeSegment ;

        }
      }
    }
    
    private int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }
    
    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( Document document, IEnumerable<Route> routesRelatedPullBox )
    {
      var selectedRoutes = Route.CollectAllDescendantBranches( routesRelatedPullBox ) ;

      var recreatedRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
      recreatedRoutes.ExceptWith( selectedRoutes ) ;
      RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), false ) ;

      // Returns affected but not deleted routes to recreate them.
      return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
    }
    
    private IEnumerable<Route> GetRouteRelatedPullBox( IEnumerable<Route> routes, Element pickedPullBox )
    {
      foreach ( var route in routes ) {
        var connectorsOfRoute = route.GetAllConnectors() ;
        if ( connectorsOfRoute.Any( x => x.Owner.UniqueId == pickedPullBox.UniqueId ) ) {
          yield return route ;
        }
      }
    }
    
    private (IEnumerable<IEndPoint> fromEndPoints,IEnumerable<IEndPoint> toEndPoints) GetEndPoints( IEnumerable<RouteSegment> segments )
    {
      List<IEndPoint> fromEndPoints = new List<IEndPoint>() ;
      List<IEndPoint> toEndPoints = new List<IEndPoint>() ;
      foreach ( var segment in segments ) {
         fromEndPoints.Add(segment.FromEndPoint) ;
         toEndPoints.Add(segment.ToEndPoint) ;
      }
      return ( fromEndPoints, toEndPoints ) ;
    }
    
    private class PullPoxPickFilter : ISelectionFilter
    {
      
      public bool AllowElement( Element e )
      {
        return ( ((FamilyInstance) e).GetConnectorFamilyType() == ConnectorFamilyType.PullBox ) ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true ;
      }
    }
  }
}