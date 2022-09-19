using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Architecture.Routing.Storages ;
using Arent3d.Architecture.Routing.Storages.Models ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.Exceptions ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class EraseSelectedPullBoxCommandBase : RoutingCommandBase<EraseSelectedPullBoxCommandBase.PickState>
  {
    public record PickState(List<Route> RoutesRelatedPullBox, Element PullBox, Dictionary<string, string> RouteNameDictionary ) ;
    protected abstract AddInType GetAddInType() ;
    protected virtual ISelectionFilter GetFilter() => new PullPoxPickFilter();

    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;

      Reference pickedPullBox ;
      try {
        pickedPullBox = uiDocument.Selection.PickObject( ObjectType.Element, GetFilter() ) ;
      }
      catch ( OperationCanceledException ) {
        return OperationResult<PickState>.Cancelled ;
      }

      var elementPullBox = document.GetElement( pickedPullBox.ElementId ) ;

      //Get information to reroute
      var routes = document.CollectRoutes( GetAddInType() ) ;
      var routesRelatedPullBox = GetRouteRelatedPullBox( routes, elementPullBox ).ToList() ;

      var routeNameDictionary = new Dictionary<string, string>() ;
      return new OperationResult<PickState>( new PickState( routesRelatedPullBox, elementPullBox, routeNameDictionary ) ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      var (routesRelatedPullBox, elementPullBox, routeNameDictionary) = pickState ;
      var routesRelatedPullBoxWithMultiConnectors = routesRelatedPullBox.Where( r => r.GetAllConnectors().Count() > 1 ).ToList() ;
      foreach ( var route in routesRelatedPullBoxWithMultiConnectors ) {
        if ( routeNameDictionary.ContainsKey( route.Name ) ) continue ;

        var conduitOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).FirstOrDefault( e => e.GetRouteName() == route.Name ) ;
        if ( conduitOfRoute == null ) continue ;

        var representativeRouteName = conduitOfRoute.GetRepresentativeRouteName() ;
        if ( ! string.IsNullOrEmpty( representativeRouteName ) && representativeRouteName != route.Name )
          routeNameDictionary.Add( route.Name, representativeRouteName! ) ;
      }

      var routeRecords = GetRelatedBranchSegments( routesRelatedPullBoxWithMultiConnectors.Where( x => x.RouteSegments.Count() > 1 ).ToList() ).ToList() ;

      var defaultRouteRelatedPullBoxWithMultiConnector = routesRelatedPullBoxWithMultiConnectors.First() ;
      var diameter = defaultRouteRelatedPullBoxWithMultiConnector.UniqueDiameter ;
      var classificationInfo = defaultRouteRelatedPullBoxWithMultiConnector.GetSystemClassificationInfo() ;
      var systemType = defaultRouteRelatedPullBoxWithMultiConnector.GetMEPSystemType() ;
      var curveType = defaultRouteRelatedPullBoxWithMultiConnector.UniqueCurveType ;
      var radius = diameter * 0.5 ;
      var isRoutingOnPipeSpace = defaultRouteRelatedPullBoxWithMultiConnector.UniqueIsRoutingOnPipeSpace ?? false ;
      var avoidType = defaultRouteRelatedPullBoxWithMultiConnector.UniqueAvoidType ?? AvoidType.Whichever ;
      var shaftElementUniqueId = defaultRouteRelatedPullBoxWithMultiConnector.UniqueShaftElementUniqueId ;
      var fromFixedHeight = defaultRouteRelatedPullBoxWithMultiConnector.UniqueFromFixedHeight ;
      var toFixedHeight = defaultRouteRelatedPullBoxWithMultiConnector.UniqueToFixedHeight ;

      #region Special case: Pull box at the end of branch route

      if ( ! routeRecords.Any() && routesRelatedPullBox.Any( r => r.GetAllConnectors().Count() == 1 ) ) {
        var routeSegmentsWithName = routesRelatedPullBox.ToSegmentsWithName().ToList() ;
        var endPointsOfBranchRouteSegments = GetEndpointsOfBranchRouteSegmentsCrossPullBox( routeSegmentsWithName, elementPullBox ) ;

        foreach ( var (routeName, segment) in routeSegmentsWithName )
          if ( endPointsOfBranchRouteSegments.ContainsKey( routeName ) && routeRecords.All( x => x.RouteName != routeName ) )
            routeRecords.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, endPointsOfBranchRouteSegments[ routeName ].FromEndPoint!, endPointsOfBranchRouteSegments[ routeName ].ToEndPoint!, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
      }

      #endregion

      // init result route
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      // Remove old route
      result.AddRange( GetSelectedRouteSegments( document, routesRelatedPullBox ) ) ;

      GetNewRouteSegmentsAfterDeletingPullBox( document, routesRelatedPullBoxWithMultiConnectors, elementPullBox, classificationInfo, systemType, curveType, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, routeNameDictionary, result, routeRecords, radius ) ;
      DropOldRoutesRelatedToPullBox( document, routesRelatedPullBox, result ) ;
      DeletePullBoxAndNotation( document, elementPullBox ) ;

      return result ;
    }

    private static void GetNewRouteSegmentsAfterDeletingPullBox( Document document, List<Route> routesRelatedPullBoxWithMultiConnectors, Element elementPullBox, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, double? diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeight, FixedHeight? toFixedHeight, AvoidType avoidType, string? shaftElementUniqueId, Dictionary<string, string> routeNameDictionary, List<(string RouteName, RouteSegment Segment)> result, List<(string RouteName, RouteSegment Segment)> routeRecords, double? radius )
    {
      var endPointsOfRouteSegmentsCrossPullBox = GetEndPointsOfRouteSegmentsCrossPullBox( routesRelatedPullBoxWithMultiConnectors, elementPullBox ) ;
      var routeSegmentsGroup = GetMainRouteSegments( routesRelatedPullBoxWithMultiConnectors, elementPullBox, classificationInfo, systemType, curveType, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId, endPointsOfRouteSegmentsCrossPullBox ) ;

      // Remove main route from routeNameDictionary (unnecessary renaming representative route name)
      var filteredRoutes = new List<string>() ;
      foreach ( var (routeName, _) in routeNameDictionary )
        if ( ! routeSegmentsGroup.ContainsKey( routeName ) )
          filteredRoutes.Add( routeName ) ;
      filteredRoutes.ForEach( f => routeNameDictionary.Remove( f ) ) ;

      // Add main routes
      foreach ( var routeSegmentGroup in routeSegmentsGroup ) {
        // Rename route name for pass point 
        RenameRoutePassPoint( document, routeSegmentGroup.Key, routeSegmentGroup.Value ) ;

        // Add main route
        var firstRouteSegment = GetFirstRouteSegment( routeSegmentGroup.Value ) ;
        result.Add( ( routeSegmentGroup.Key, firstRouteSegment ) ) ; // Add first segment

        // Add next segments
        while ( true ) {
          var nextSegment = GetNextRouteSegment( result.Last().Segment.ToEndPoint, routeSegmentGroup.Value ) ;
          if ( nextSegment == null ) break ;
          result.Add( ( routeSegmentGroup.Key, nextSegment ) ) ;
        }
      }

      // Add branch routes
      foreach ( var (routeName, segment) in routeRecords ) {
        var passPointEndPointUniqueId = segment.FromEndPoint.Key.GetElementUniqueId() ;
        if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type ) {
          var fromEndPointKey = GetFromEndPointKey( document, result, passPointEndPointUniqueId ) ;
          if ( fromEndPointKey == null ) continue ;
          var branchEndPoint = new PassPointBranchEndPoint( document, passPointEndPointUniqueId, radius, fromEndPointKey ) ;
          var toEndPoint = segment.ToEndPoint ;

          if ( segment.ToEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) {
            var routeNameArray = routeName.Split( '_' ) ;
            var mainRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
            var associatedRouteName = endPointsOfRouteSegmentsCrossPullBox.Keys.FirstOrDefault( k =>
            {
              var rNameArray = k.Split( '_' ) ;
              var rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
              return rName == mainRouteName ;
            } ) ;
            if ( ! string.IsNullOrEmpty( associatedRouteName ) && endPointsOfRouteSegmentsCrossPullBox.ContainsKey( associatedRouteName ) )
              toEndPoint = endPointsOfRouteSegmentsCrossPullBox[ associatedRouteName ].ToEndPoint ;
          }

          result.Add( ( routeName, new RouteSegment( segment.SystemClassificationInfo, segment.SystemType, segment.CurveType, branchEndPoint, toEndPoint!, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
        }
        else {
          result.Add( ( routeName, segment ) ) ;
        }
      }
    }

    private static Dictionary<string, List<RouteSegment>> GetMainRouteSegments( List<Route> routesRelatedPullBoxWithMultiConnectors, Element elementPullBox, MEPSystemClassificationInfo classificationInfo, MEPSystemType? systemType, MEPCurveType? curveType, double? diameter, bool isRoutingOnPipeSpace, FixedHeight? fromFixedHeight, FixedHeight? toFixedHeight, AvoidType avoidType, string? shaftElementUniqueId, Dictionary<string, (IEndPoint? FromEndPoint, IEndPoint? ToEndPoint)> endPointsOfRouteSegmentsCrossPullBox )
    {
      var routeSegmentsGroup = new Dictionary<string, List<RouteSegment>>() ;
      foreach ( var route in routesRelatedPullBoxWithMultiConnectors ) {
        var routeSegments = route.RouteSegments ;
        var routeName = route.Name ;

        if ( ! endPointsOfRouteSegmentsCrossPullBox.ContainsKey( routeName ) ) {
          var routeNameArray = route.Name.Split( '_' ) ;
          var rName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
          var endPointsOfRouteSegmentCrossPullBox = endPointsOfRouteSegmentsCrossPullBox.FirstOrDefault( e =>
          {
            var rNameArray = e.Key.Split( '_' ) ;
            var eName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
            return rName == eName ;
          } ) ;
          if ( string.IsNullOrEmpty( endPointsOfRouteSegmentCrossPullBox.Key ) ) continue ;

          routeName = endPointsOfRouteSegmentCrossPullBox.Key ;
        }

        var (fromEndPoint, toEndPoint) = endPointsOfRouteSegmentsCrossPullBox[ routeName ] ;
        if ( fromEndPoint == null || toEndPoint == null ) continue ;

        var listRouteSegmentTemp = new List<RouteSegment>() ;
        foreach ( var segment in routeSegments ) {
          if ( segment.FromEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId || segment.ToEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId )
            listRouteSegmentTemp.Add( new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ;
          else
            listRouteSegmentTemp.Add( segment ) ;
        }

        if ( routeSegmentsGroup.ContainsKey( routeName ) ) {
          if ( routeSegmentsGroup[ routeName ] == null )
            routeSegmentsGroup[ routeName ] = listRouteSegmentTemp ;
          else
            routeSegmentsGroup[ routeName ].AddRange( listRouteSegmentTemp ) ;
        }
        else {
          routeSegmentsGroup.Add( routeName, listRouteSegmentTemp ) ;
        }
      }

      return routeSegmentsGroup ;
    }

    private static Dictionary<string, (IEndPoint? FromEndPoint, IEndPoint? ToEndPoint)> GetEndPointsOfRouteSegmentsCrossPullBox( List<Route> routesRelatedPullBoxWithMultiConnectors, Element elementPullBox )
    {
      var endPointsOfRouteSegmentsCrossPullBox = new Dictionary<string, (IEndPoint? FromEndPoint, IEndPoint? ToEndPoint)>() ;
      foreach ( var route in routesRelatedPullBoxWithMultiConnectors ) {
        var routeSegments = route.RouteSegments ;

        foreach ( var routeSegment in routeSegments ) {
          if ( routeSegment.FromEndPoint.Key.GetElementUniqueId() != elementPullBox.UniqueId ) continue ;

          if ( endPointsOfRouteSegmentsCrossPullBox.ContainsKey( route.Name ) )
            endPointsOfRouteSegmentsCrossPullBox[ route.Name ] = ( endPointsOfRouteSegmentsCrossPullBox[ route.Name ].FromEndPoint, routeSegment.ToEndPoint ) ;
          else
            endPointsOfRouteSegmentsCrossPullBox.Add( route.Name, ( null, routeSegment.ToEndPoint ) ) ;
        }
      }

      foreach ( var route in routesRelatedPullBoxWithMultiConnectors ) {
        var routeSegments = route.RouteSegments ;

        foreach ( var routeSegment in routeSegments ) {
          if ( routeSegment.ToEndPoint.Key.GetElementUniqueId() != elementPullBox.UniqueId ) continue ;

          var routeNameArray = route.Name.Split( '_' ) ;
          var routeName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
          var endPointsOfRouteSegmentCrossPullBox = endPointsOfRouteSegmentsCrossPullBox.FirstOrDefault( e =>
          {
            var rNameArray = e.Key.Split( '_' ) ;
            var rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
            return routeName == rName ;
          } ) ;
          if ( ! string.IsNullOrEmpty( endPointsOfRouteSegmentCrossPullBox.Key ) )
            endPointsOfRouteSegmentsCrossPullBox[ endPointsOfRouteSegmentCrossPullBox.Key ] = ( routeSegment.FromEndPoint, endPointsOfRouteSegmentsCrossPullBox[ endPointsOfRouteSegmentCrossPullBox.Key ].ToEndPoint ) ;
        }
      }

      return endPointsOfRouteSegmentsCrossPullBox ;
    }

    private static void DropOldRoutesRelatedToPullBox( Document document, IEnumerable<Route> routesRelatedPullBox, IReadOnlyCollection<(string RouteName, RouteSegment Segment)> result )
    {
      // Drop old routes in RouteCache
      var selectedRoutes = Route.CollectAllDescendantBranches( routesRelatedPullBox ) ;
      var droppedRoutes = selectedRoutes.Where( r => result.All( t => t.RouteName != r.RouteName ) ).Select( r => r.RouteName ) ;
      var routeCache = RouteCache.Get( DocumentKey.Get( document ) ) ;
      routeCache.Drop( droppedRoutes ) ;
    }

    private static void DeletePullBoxAndNotation( Document document, Element elementPullBox )
    {
      //Delete label of pull box
      var level = document.GetAllElements<Level>().OfCategory( BuiltInCategory.OST_Levels ).SingleOrDefault( l => l.Id == elementPullBox.LevelId ) ;
      if ( level != null ) {
        var storagePullBoxInfoServiceByLevel = new StorageService<Level, PullBoxInfoModel>( level ) ;
        var pullBoxInfoModel = storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.FirstOrDefault( p => p.PullBoxUniqueId == elementPullBox.UniqueId ) ;
        if ( pullBoxInfoModel != null ) {
          var textNote = document.GetAllElements<TextNote>().FirstOrDefault( t => pullBoxInfoModel.TextNoteUniqueId == t.UniqueId ) ;
          if ( textNote != null )
            document.Delete( textNote.Id ) ;
          storagePullBoxInfoServiceByLevel.Data.PullBoxInfoData.Remove( pullBoxInfoModel ) ;
        }
      }

      //Delete pull box
      document.Delete( elementPullBox.Id ) ;
    }

    private static Dictionary<string, (IEndPoint? FromEndPoint, IEndPoint? ToEndPoint)> GetEndpointsOfBranchRouteSegmentsCrossPullBox( List<(string RouteName, RouteSegment Segment)> routeSegmentsWithName, Element elementPullBox )
    {
      var endPointsOfBranchRouteSegments = new Dictionary<string, (IEndPoint? FromEndPoint, IEndPoint? ToEndPoint)>() ;
      foreach ( var (routeName, segment) in routeSegmentsWithName )
        if ( segment.FromEndPoint.DisplayTypeName == PassPointBranchEndPoint.Type && segment.ToEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) {
          if ( endPointsOfBranchRouteSegments.ContainsKey( routeName ) )
            endPointsOfBranchRouteSegments[ routeName ] = ( segment.FromEndPoint, endPointsOfBranchRouteSegments[ routeName ].ToEndPoint ) ;
          else
            endPointsOfBranchRouteSegments.Add( routeName, ( segment.FromEndPoint, null ) ) ;
        }
        else if ( segment.FromEndPoint.Key.GetElementUniqueId() == elementPullBox.UniqueId ) {
          var routeNameArray = routeName.Split( '_' ) ;
          var familyRouteName = string.Join( "_", routeNameArray.First(), routeNameArray.ElementAt( 1 ) ) ;
          var endPointsOfBranchRouteSegment = endPointsOfBranchRouteSegments.FirstOrDefault( e =>
          {
            var rNameArray = e.Key.Split( '_' ) ;
            var rName = string.Join( "_", rNameArray.First(), rNameArray.ElementAt( 1 ) ) ;
            return familyRouteName == rName ;
          } ) ;
          if ( ! string.IsNullOrEmpty( endPointsOfBranchRouteSegment.Key ) )
            endPointsOfBranchRouteSegments[ endPointsOfBranchRouteSegment.Key ] = ( endPointsOfBranchRouteSegments[ endPointsOfBranchRouteSegment.Key ].FromEndPoint, segment.ToEndPoint ) ;
          else
            endPointsOfBranchRouteSegments.Add( familyRouteName, ( null, segment.ToEndPoint ) ) ;
        }

      return endPointsOfBranchRouteSegments.Where( e => e.Value.FromEndPoint != null && e.Value.ToEndPoint != null ).ToDictionary( e => e.Key, e => e.Value ) ;
    }

    private static RouteSegment GetFirstRouteSegment( IEnumerable<RouteSegment> routeSegments )
    {
      return routeSegments.First( x => x.FromEndPoint is ConnectorEndPoint ) ;
    }

    private static RouteSegment? GetNextRouteSegment( IEndPoint toEndPoint, IEnumerable<RouteSegment> routeSegments )
    {
      return routeSegments.FirstOrDefault( x => x.FromEndPoint.Key.GetElementUniqueId() == toEndPoint.Key.GetElementUniqueId() ) ;
    }

    private static EndPointKey? GetFromEndPointKey( Document document, List<(string RouteName, RouteSegment Segment)> segments, string passPointEndPointUniqueId )
    {
      var fromRouteName = string.Empty ;
      foreach ( var (routeName, segment) in segments ) {
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

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetRelatedBranchSegments( List<Route> routes )
    {
      // add all related branches
      var relatedBranches = routes.SelectMany( r => r.GetAllRelatedBranches() ).ToList() ;
      routes.ForEach( r => relatedBranches.Remove( r ) ) ;
      return relatedBranches.ToSegmentsWithName() ;
    }

    private static void RenameRoutePassPoint( Document document, string name, List<RouteSegment> routeSegments )
    {
      foreach ( var routeSegment in routeSegments ) {
        var fromEndPointKey = routeSegment.FromEndPoint.Key ;
        var toEndPointKey = routeSegment.ToEndPoint.Key ;
        var fromPassPoint = document.GetElementById<Instance>( fromEndPointKey.GetElementUniqueId() ) ;
        var toPassPoint = document.GetElementById<Instance>( toEndPointKey.GetElementUniqueId() ) ;
        if ( fromPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() )
          fromPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ;
        if ( toPassPoint?.Name == RoutingFamilyType.PassPoint.GetFamilyName() )
          toPassPoint?.SetProperty( RoutingParameter.RouteName, name ) ;
      }
    }

    private static IEnumerable<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( Document document, IEnumerable<Route> routesRelatedPullBox )
    {
      var selectedRoutes = Route.CollectAllDescendantBranches( routesRelatedPullBox ) ;

      var recreatedRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
      recreatedRoutes.ExceptWith( selectedRoutes ) ;
      RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), false ) ;

      // Returns affected but not deleted routes to recreate them.
      return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
    }

    private static IEnumerable<Route> GetRouteRelatedPullBox( IEnumerable<Route> routes, Element pickedPullBox )
    {
      foreach ( var route in routes ) {
        var connectorsOfRoute = route.GetAllConnectors().ToList() ;
        if ( connectorsOfRoute.Any( x => x.Owner.UniqueId == pickedPullBox.UniqueId ) )
          yield return route ;
      }
    }

    protected override void AfterRouteGenerated( Document document, IReadOnlyCollection<Route> executeResultValue, PickState pickState )
    {
      if ( ! pickState.RouteNameDictionary.Any() ) return ;
      RouteGenerator.ChangeRepresentativeRouteName( document, pickState.RouteNameDictionary ) ;
    }

    private class PullPoxPickFilter : ISelectionFilter
    {
      public bool AllowElement( Element e )
      {
        return e is FamilyInstance familyInstance && familyInstance.GetConnectorFamilyType() == ConnectorFamilyType.PullBox ;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true ;
      }
    }
  }
}