using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
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
    public record PickState(List<Route> RoutesRelatedPullBox, Element PullBox) ;
    protected abstract AddInType GetAddInType() ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var pickedPullBox = uiDocument.Selection.PickObject( ObjectType.Element, new PullPoxPickFilter() ) ;
      var elementPullBox = document.GetElement( pickedPullBox?.ElementId ) ;

      //Get information to reroute
      var routes = document.CollectRoutes( GetAddInType()) ;
      var routesRelatedPullBox = GetRouteRelatedPullBox( routes, elementPullBox ).ToList();
      
      return new OperationResult<PickState>( new PickState(routesRelatedPullBox, elementPullBox) ) ;
    }
    
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (routesRelatedPullBox, elementPullBox) = pickState ;
      
      var routeRecords = GetRelatedBranchSegments( routesRelatedPullBox.FirstOrDefault(x=>x.RouteSegments.Count() > 1) ).ToList() ;
      
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
      
      // init result route
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      // Remove old route
      result.AddRange( GetSelectedRouteSegments( document, routesRelatedPullBox ) );
      
      var allRouteSegments =  GetAllRouteSegment(routesRelatedPullBox) ;
      List<IEndPoint> fromEndPoints = new() ;
      List<IEndPoint> toEndPoints = new() ;
      bool isHaveFromEndPoint = false ;
      bool isHaveToEndPoint = false ;
      var routeSegments = allRouteSegments.ToList() ;
      foreach ( var routeSegment in routeSegments ) {
        if ( isHaveFromEndPoint && isHaveToEndPoint ) break ;
        if ( routeSegment.ToEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) 
        {
          fromEndPoints.Add( routeSegment.FromEndPoint );
          isHaveFromEndPoint = true ;
        }
        
        if ( routeSegment.FromEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) 
        {
          toEndPoints.Add( routeSegment.ToEndPoint );
          isHaveToEndPoint = true ;
        }
      }

      if ( fromEndPoints.Any() && toEndPoints.Any() ) {
        List<RouteSegment> routeSegmentsTemp = new() ;
        int countAddSegmentPullBox = 0 ;
        foreach ( var routeSegment in routeSegments ) {
          if ( routeSegment.FromEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId || routeSegment.ToEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) 
          {
            if ( countAddSegmentPullBox < 1 ) {
              routeSegmentsTemp.Add( new RouteSegment( classificationInfo, systemType, curveType, fromEndPoints.First(), toEndPoints.First(), diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) );
              countAddSegmentPullBox++ ;
            }
          }
          else {
            // var segment = new RouteSegment( routeSegment.SystemClassificationInfo, routeSegment.SystemType, routeSegment.CurveType,  routeSegment.FromEndPoint, routeSegment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
            routeSegmentsTemp.Add(  routeSegment ) ;
          }
        }
        
        // Rename route name for pass point 
        RenameRoutePassPoint( document, name, routeSegmentsTemp ) ;
        
        // Add main route
        var firstRouteSegment = FirstRouteSegment( routeSegmentsTemp ) ;
        result.Add( (name, firstRouteSegment)) ;       // Add first segment

        // Add next segments
        while ( true ) {
          var nextSegment = NextRouteSegment( result.Last().Segment.ToEndPoint, routeSegmentsTemp ) ;
          if(nextSegment == null) break ;
          result.Add( (name, nextSegment) ) ;
        }

        // Add branch route
        foreach ( var ( routeName, segment ) in routeRecords ) {
          var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
          if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
            var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ?? firstRouteSegment.FromEndPoint.Key ;
            var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
            result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, segment.ToEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
          }
          else {
            result.Add( ( routeName, segment ) ) ;
          }
        }
      }

      //Delete label of pull box
      var pullBoxInfoStorable = document.GetPullBoxInfoStorable() ;
      var pullBoxInfoModel = pullBoxInfoStorable.PullBoxInfoModelData.FirstOrDefault( p => p.PullBoxUniqueId == elementPullBox.UniqueId ) ;
      if ( pullBoxInfoModel != null ) {
        var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel.TextNoteUniqueId == t.UniqueId ) ;
        if( textNote != null )
          document.Delete( textNote.Id ) ;
        pullBoxInfoStorable.PullBoxInfoModelData.Remove( pullBoxInfoModel ) ;
      }

      //Delete pull box
      document.Delete( elementPullBox.Id ) ;

      return result ;
    }

    private RouteSegment FirstRouteSegment(List<RouteSegment> routeSegments)
    {
      return routeSegments.First( x => x.FromEndPoint is ConnectorEndPoint) ;
    } 

    private RouteSegment? NextRouteSegment(IEndPoint toEndPoint , List<RouteSegment> routeSegments)
    {
      return routeSegments.FirstOrDefault( x => x.FromEndPoint.Key.GetElementUniqueId() == toEndPoint.Key.GetElementUniqueId() ) ;
    }
    
    private EndPointKey? GetFromEndPointKey( Document document, List<(string RouteName, RouteSegment Segment)> segments, string passPointEndPointUniqueId )
    {
      var fromRouteName = string.Empty ;
      foreach ( var ( routeName, segment ) in segments ) {
        if ( segment.FromEndPoint.Key.GetElementUniqueId() != passPointEndPointUniqueId && segment.FromEndPoint.Key.GetElementUniqueId() != passPointEndPointUniqueId ) continue ;
        fromRouteName = routeName ;
        break ;
      }

      if ( string.IsNullOrEmpty( fromRouteName ) ) return null ;
      var fromSegment = segments.FirstOrDefault( s => s.RouteName == fromRouteName ) ;
      var fromEndPointKey = fromSegment.Segment.FromEndPoint.Key ;
      var passPoint = document.GetElementById<Instance>( passPointEndPointUniqueId ) ;
      passPoint?.SetProperty( RoutingParameter.RouteName, fromRouteName ) ;
      
      return fromEndPointKey ;
    }
    
    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRelatedBranchSegments( Route? route )
    {
      if ( route == null) {
        return new List<(string RouteName, RouteSegment Segment)>() ;
      }
      // add all related branches
      var relatedBranches = route.GetAllRelatedBranches() ;
      relatedBranches.Remove( route ) ;
      return relatedBranches.ToSegmentsWithName() ;
    }
    
    private void RenameRoutePassPoint( Document document, string name, List<RouteSegment> routeSegments) 
    {
      foreach ( var routeSegment in routeSegments ) {
        var fromEndPointKey = routeSegment.FromEndPoint.Key ;
        var toEndPointKey   = routeSegment.ToEndPoint.Key ;
        var fromPassPoint = document.GetElementById<Instance>( fromEndPointKey.GetElementUniqueId() ) ;
        var toPassPoint = document.GetElementById<Instance>( toEndPointKey.GetElementUniqueId() ) ;
        if ( fromPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() ) {
          fromPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ; 
        }
        if ( toPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() ) {
          toPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ;
        }
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
    
    private IEnumerable<Route> GetRouteRelatedPullBox( IEnumerable<Route> routes, Element pickedPullBox)
    {
      foreach ( var route in routes ) {
        var connectorsOfRoute = route.GetAllConnectors().ToList() ;

        if ( connectorsOfRoute.Any( x => x.Owner.UniqueId == pickedPullBox.UniqueId )  && connectorsOfRoute.Count() > 1) {
          yield return route ;
        }
      }
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