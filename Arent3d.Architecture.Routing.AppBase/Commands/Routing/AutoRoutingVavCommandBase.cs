using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AutoRoutingVavCommandBase : RoutingCommandBase
  {
    private const int VavConnectorId = 4 ;
    private const string ErrorMessageNoVav = "No VAV on the AHU level." ;
    private const string ErrorMessageNoAhu = "No AHU are selected." ;
    private const string ErrorMessageNoSpace = "Find space cannot be found." ;

    protected abstract AddInType GetAddInType() ;

    private record SelectState( Connector? AhuConnector, IReadOnlyList<FamilyInstance> GrandParentConnectors, Dictionary<int, List<FamilyInstance>> ParentConnectors, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo, MEPSystemPipeSpec PipeSpec ) ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    protected abstract (IEndPoint EndPoint, IReadOnlyCollection<(string RouteName, RouteSegment Segment)>? OtherSegments) CreateEndPointOnSubRoute( ConnectorPicker.IPickResult newPickResult, ConnectorPicker.IPickResult anotherPickResult, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, bool newPickIsFrom ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var (fromPickResult, toPickResult, grandParentConnectors, parentConnectors, errorMessage) = SelectionAhuAndFindVav( uiDocument, routingExecutor, GetAddInType() ) ;
      if ( null != errorMessage ) return ( false, errorMessage ) ;
      var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, grandParentConnectors.First() ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;
      var pipeSpec = new MEPSystemPipeSpec( new RouteMEPSystem( uiDocument.Document, property.GetSystemType(), property.GetCurveType() ), routingExecutor.FittingSizeCalculator ) ;
      if ( GetMEPSystemClassificationInfo( fromPickResult, toPickResult, property.GetSystemType() ) is { } classificationInfo ) return ( true, new SelectState( fromPickResult.PickedConnector, grandParentConnectors, parentConnectors, property, classificationInfo, pipeSpec ) ) ;
      return ( false, null ) ;
    }

    private static ( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, IReadOnlyList<FamilyInstance> grandParentConnectors, Dictionary<int, List<FamilyInstance>> parentConnectors, string? ErrorMessage ) SelectionAhuAndFindVav( UIDocument uiDocument, RoutingExecutor routingExecutor, AddInType addInType )
    {
      var doc = uiDocument.Document ;

      // Select AHU
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, addInType ) ;
      if ( fromPickResult.PickedConnector == null ) return ( null!, null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoAhu ) ;
      var ahuPosition = fromPickResult.PickedConnector.Origin ;

      // Get all vav
      var dampers = doc.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ) ;
      var dampersInstances = dampers as FamilyInstance[] ?? dampers.ToArray() ;
      if ( ! dampersInstances.Any() ) return ( null!, null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoVav ) ;

      // Get all space box
      var spaceBoxes = GetAllSpaces( doc ) ;
      if ( ! spaceBoxes.Any() ) return ( null!, null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoSpace ) ;

      // Get group space
      var (grandParentSpaces, parentSpaces) = GetGroupSpace( spaceBoxes, ahuPosition ) ;
      
      var grandParentConnectors = grandParentSpaces.ConvertAll( space => GetVavFromSpace( doc, dampersInstances, space ) ) ;
      var parentConnectors = GetVavFromSpace( doc, dampersInstances, parentSpaces ) ;
      
      if ( ! grandParentConnectors.Any() ) return ( null!, null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoVav ) ;
      ConnectorPicker.IPickResult toPickResult = ConnectorPicker.GetVavConnector( grandParentConnectors.Last(), addInType ) ;
      
      return ( fromPickResult, toPickResult, grandParentConnectors, parentConnectors, null ) ;
    }

    private static FamilyInstance GetVavFromSpace( Document doc, FamilyInstance[] dampersInstances, Element space )
    {
      BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
      FamilyInstance vav = null! ;
      foreach ( var fi in dampersInstances ) {
        var vavPosition = fi.Location as LocationPoint ;
        if ( vavPosition == null ) continue ;
        if ( spaceBox.Max.X > vavPosition.Point.X && spaceBox.Max.Y > vavPosition.Point.Y && spaceBox.Max.Z > vavPosition.Point.Z && spaceBox.Min.X < vavPosition.Point.X && spaceBox.Min.Y < vavPosition.Point.Y && spaceBox.Min.Z < vavPosition.Point.Z ) {
          vav = fi ;
          break ;
        }
      }

      return vav ;
    }

    private static Dictionary<int, List<FamilyInstance>> GetVavFromSpace( Document doc, FamilyInstance[] dampersInstances, Dictionary<int, List<Element>> parentSpaces )
    {
      var result = new Dictionary<int, List<FamilyInstance>>() ;
      foreach ( var (key, value) in parentSpaces ) {
        foreach ( var space in value ) {
          BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
          FamilyInstance vav = null! ;
          foreach ( var fi in dampersInstances ) {
            var vavPosition = fi.Location as LocationPoint ;
            if ( vavPosition == null ) continue ;
            if ( spaceBox.Max.X > vavPosition.Point.X && spaceBox.Max.Y > vavPosition.Point.Y && spaceBox.Max.Z > vavPosition.Point.Z && spaceBox.Min.X < vavPosition.Point.X && spaceBox.Min.Y < vavPosition.Point.Y && spaceBox.Min.Z < vavPosition.Point.Z ) {
              vav = fi ;
              break ;
            }
          }

          if ( result.ContainsKey( key ) ) {
            result[ key ].Add( vav ) ;
          }
          else {
            result.Add( key, new List<FamilyInstance>() { vav } ) ;
          }          
        }
      }

      return result ;
    }
    
    private static ( IReadOnlyList<Element> grandParentSpaces, Dictionary<int, List<Element>> parentSpaces ) GetGroupSpace( IList<Element> spaceBoxes, XYZ ahuPosition )
    {
      List<Element> grandParentSpaces = new() ;
      var maxDistance = double.MinValue ;      
      Dictionary<int, List<Element>> parentSpaces = new() ;
      foreach ( Element space in spaceBoxes ) {
        var branchNumber = space.GetSpaceBranchNumber() ;
        if(branchNumber == (int) SpaceType.Invalid) continue;
        if ( branchNumber == (int)SpaceType.GrandParent ) {
          grandParentSpaces.Add( space ) ;
          var spacePosition = space.Location as LocationPoint ;
          var distance = ahuPosition.DistanceTo( spacePosition!.Point ) ;
          if ( ! ( distance > maxDistance ) ) continue ;
          maxDistance = distance ;
        }
        else {
          if ( parentSpaces.ContainsKey( branchNumber ) ) {
            parentSpaces[ branchNumber ].Add( space ) ;
          }
          else {
            parentSpaces.Add( branchNumber, new List<Element>() { space } ) ;
          }
        }
      }
      grandParentSpaces.Sort( Compare ) ;
      foreach ( var (key, value) in parentSpaces ) {
        parentSpaces[key].Sort( CompareY ) ;
      }
      return ( grandParentSpaces, parentSpaces ) ;
    }

    private static int Compare( Element a, Element b )
    {
      if ( a.Location is not LocationPoint aPos || b.Location is not LocationPoint bPos ) return default ;
      return aPos.Point.X.CompareTo( bPos.Point.X ) ;
    }
    
    private static int CompareY( Element a, Element b )
    {
      if ( a.Location is not LocationPoint aPos || b.Location is not LocationPoint bPos ) return default ;
      return aPos.Point.Y.CompareTo( bPos.Point.Y ) ;
    }        

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult, MEPSystemType? systemType )
    {
      if ( ( fromPickResult.SubRoute ?? toPickResult.SubRoute )?.Route.GetSystemClassificationInfo() is { } routeSystemClassificationInfo ) return routeSystemClassificationInfo ;

      if ( ( fromPickResult.PickedConnector ?? toPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    private RoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, Element toPickElement )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;
      var toLevelId = toPickElement.LevelId ;

      if ( ( fromPickResult.PickedConnector ?? toPickElement.GetConnectors().FirstOrDefault() ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }

    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }

    protected virtual RoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static RoutePropertyDialog ShowDialog( Document document, AddInType addInType, ElementId fromLevelId, ElementId toLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, addInType, fromLevelId, toLevelId ) ;
      var sv = new RoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, routeChoiceSpec ) ) ;
      sv.ShowDialog() ;

      return sv ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var selectState = state as SelectState ?? throw new InvalidOperationException() ;
      var (ahuConnector, grandParentConnectors, parentConnectors, routeProperty, classificationInfo, pipeSpec) = selectState ;

      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
      var sensorFixedHeight = routeProperty.GetFromFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var diameter = routeProperty.GetDiameter() ;
      var radius = diameter * 0.5 ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( RouteCache.Get( document ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter
      var result = new List<(string RouteName, RouteSegment Segment)>( 2 ) ;

      // Grand parent routes
      var ahuConnectorEndPoint = new ConnectorEndPoint( ahuConnector!, radius ) ;

      // Main routes
      var vavConnectorEndPoint = new ConnectorEndPoint( grandParentConnectors.Last().GetConnectors().First( c => c.Id != VavConnectorId ), radius ) ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, ahuConnectorEndPoint, vavConnectorEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft().GetValidId() ) ) ) ;
      
      // Branch routes
      var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
      var branchEndPoint = new RouteEndPoint( document, routeName, 0 ) ;
      var connectorEndPoint = new ConnectorEndPoint( parentConnectors[1].Last().GetConnectors().First( c => c.Id != VavConnectorId ), radius ) ;          
      var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameter, false, sensorFixedHeight, sensorFixedHeight, avoidType, ElementId.InvalidElementId ) ;
      result.Add( ( subRouteName, segment ) );
      
      return result ;
    }

    private static (IReadOnlyList<FamilyInstance>, Dictionary<int, List<FamilyInstance>> ) CreatePassPoints( string routeName, Connector ahuConnector, IReadOnlyCollection<FamilyInstance> grandParentConnectors, Dictionary<int, List<FamilyInstance>> parentConnectors, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo, MEPSystemPipeSpec pipeSpec )
    {
      var document = grandParentConnectors.FirstOrDefault()!.Document ;
      var levelId = grandParentConnectors.FirstOrDefault()!.LevelId ;
      var diameter = routeProperty.GetDiameter() ;
      var bendingRadius = pipeSpec.GetLongElbowSize( diameter.DiameterValueToPipeDiameter() ) ;
      var forcedFixedHeight = PassPointEndPoint.GetForcedFixedHeight( document, routeProperty.GetFromFixedHeight(), levelId ) ;
      var grandParentWithoutLast = grandParentConnectors.Take( grandParentConnectors.Count - 1 ).ToReadOnlyCollection( grandParentConnectors.Count - 1 ) ;
      var (passPointPositions, childPpPositions) = GetPassPointPositions( ahuConnector, grandParentWithoutLast, parentConnectors, forcedFixedHeight, bendingRadius ) ;
      var passPointDirection = new XYZ( 1, 0, 0 ) ;
      var passPoints = passPointPositions.ConvertAll( pos => document.AddPassPoint( routeName, pos, passPointDirection, diameter * 0.5, levelId ) ) ;

      var childPassPoints = new Dictionary<int, List<FamilyInstance>>() ;
      var passPointChildDir = new XYZ( 0, 1, 0 ) ;
      foreach ( var (key, value) in childPpPositions ) {
        childPassPoints[ key ] = value.ConvertAll( pos => document.AddPassPoint( routeName, pos, passPointChildDir, diameter * 0.5, levelId ) ) ;
      }

      return ( passPoints, childPassPoints ) ;

      static (IReadOnlyList<XYZ>, Dictionary<int, List<XYZ>>) GetPassPointPositions( Connector ahuConnector, IReadOnlyCollection<FamilyInstance> grandParentConnectors, Dictionary<int, List<FamilyInstance>> parentConnectors, double? forcedFixedHeight, double bendingRadius )
      {
        var ahuPosition = ahuConnector.Origin ;
        var grandParentPositions = grandParentConnectors.ConvertAll( connector => connector.GetTopConnectorOfConnectorFamily().Origin ) ;
        var fixedHeight = forcedFixedHeight ?? GetPreferredRouteHeight( ahuPosition, grandParentPositions, grandParentPositions.Last(), bendingRadius ) ;
        var passPoints = new List<XYZ>() ;
        
        // Add grand parent pass point for grand parent group
        foreach ( var grandParentPosition in grandParentPositions ) {
          var parentPassPoint = new XYZ( grandParentPosition.X-5, ahuPosition.Y, fixedHeight ) ;
          passPoints.Add( parentPassPoint ) ;
        }
        
        // Add grand parent pass point for other group
        Dictionary<int, List<XYZ>> childPassPoints = new() ;
        foreach ( var (key, value) in parentConnectors ) {
          var entryCollection = value.ToReadOnlyCollection(value.Count) ;
          var parentPositions = entryCollection.ConvertAll( connector => connector.GetTopConnectorOfConnectorFamily().Origin ).ToList() ;
          var farthestParent = parentPositions.Last() ;
          var parentPassPoint = new XYZ( farthestParent.X - 10, ahuPosition.Y, fixedHeight ) ;
          passPoints.Add( parentPassPoint );
          
          // Add child pass point
          foreach ( var vav in parentPositions.Take( parentPositions.Count - 1 ) ) {
            var childPassPoint = new XYZ( farthestParent.X - 5, vav.Y - 5, fixedHeight ) ;
            if ( childPassPoints.ContainsKey( key ) ) {
              childPassPoints[ key ].Add( childPassPoint ) ;
            }
            else {
              childPassPoints.Add( key, new List<XYZ>() { childPassPoint } ) ;
            }               
          }

          var orderedPassPoints = childPassPoints[ key ].OrderBy( pp => pp.Y ).ToList() ;
          childPassPoints[ key ] = orderedPassPoints ;
        }
        var orderedEnumerable = passPoints.OrderBy( pp => pp.X ).ToList() ;
     
        return ( orderedEnumerable, childPassPoints ) ;
      }
    }

    private static double GetPreferredRouteHeight( XYZ ahuPosition, IEnumerable<XYZ> grandParentPositions, XYZ lastGrandParentPosition, double bendingRadius )
    {
      var sensorHeight = grandParentPositions.Append( lastGrandParentPosition ).Max( pos => pos.Z ) ;
      var powerHeight = ahuPosition.Z ;
      if ( powerHeight < sensorHeight + bendingRadius ) {
        
        return powerHeight + bendingRadius ;
      }
      else {
        return sensorHeight + bendingRadius ;
      }
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;
  }
}