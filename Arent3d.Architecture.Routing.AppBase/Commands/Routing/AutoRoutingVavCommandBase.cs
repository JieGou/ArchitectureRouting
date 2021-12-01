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
    private const int DefaultSubRouteIndex = 0 ;
    private const string ErrorMessageNoVav = "No VAV on the AHU level." ;
    private const string ErrorMessageNoAhu = "No AHU are selected." ;
    private const string ErrorMessageNoSpace = "Find space cannot be found." ;
    private const string ErrorMessageNoGrandParentVav = "No VAV on the space group 0" ;

    protected abstract AddInType GetAddInType() ;

    private record SelectState( Connector? AhuConnector, IReadOnlyList<FamilyInstance> GrandParentConnectors, Dictionary<int, List<FamilyInstance>> ParentConnectors, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      var (fromPickResult, grandParentConnectors, parentConnectors, errorMessage) = SelectionAhuAndFindVav( uiDocument, routingExecutor, GetAddInType() ) ;
      if ( null != errorMessage ) return ( false, errorMessage ) ;
      var property = ShowPropertyDialog( uiDocument.Document, fromPickResult, grandParentConnectors.First() ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;
      if ( GetMEPSystemClassificationInfoFromSystemType( property.GetSystemType() ) is { } classificationInfo ) return ( true, new SelectState( fromPickResult.PickedConnector, grandParentConnectors, parentConnectors, property, classificationInfo ) ) ;
      return ( false, null ) ;
    }

    private static ( ConnectorPicker.IPickResult fromPickResult, IReadOnlyList<FamilyInstance> grandParentConnectors, Dictionary<int, List<FamilyInstance>> parentConnectors, string? ErrorMessage ) SelectionAhuAndFindVav( UIDocument uiDocument, RoutingExecutor routingExecutor, AddInType addInType )
    {
      var doc = uiDocument.Document ;

      // Select AHU
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, addInType ) ;
      if ( fromPickResult.PickedConnector == null ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoAhu ) ;
      // var ahuPosition = fromPickResult.PickedConnector.Origin ;

      // Get all vav
      var dampers = doc.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ) ;
      var dampersInstances = dampers as FamilyInstance[] ?? dampers.ToArray() ;
      if ( ! dampersInstances.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoVav ) ;

      // Get all space box
      var spaceBoxes = GetAllSpaces( doc ) ;
      if ( ! spaceBoxes.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoSpace ) ;

      // Get group space
      var (grandParentSpaces, parentSpaces) = GetGroupSpace( spaceBoxes ) ;

      var grandParentConnectors = grandParentSpaces.ConvertAll( space => GetVavFromSpace( doc, dampersInstances, space ) ) ;
      var parentConnectors = GetVavFromSpace( doc, dampersInstances, parentSpaces ) ;

      if ( ! grandParentConnectors.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoGrandParentVav ) ;

      return ( fromPickResult, grandParentConnectors, parentConnectors, null ) ;
    }

    /// <summary>
    /// Get one Vav from one space
    /// </summary>
    private static FamilyInstance GetVavFromSpace( Document doc, IEnumerable<FamilyInstance> dampersInstances, Element space )
    {
      BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;

      return ( from fi in dampersInstances let vavPosition = fi.Location as LocationPoint where vavPosition != null where IsInSpace( spaceBox, vavPosition.Point ) select fi ).FirstOrDefault()! ;
    }

    /// <summary>
    /// Get multi Vav from multi space 
    /// </summary>
    private static Dictionary<int, List<FamilyInstance>> GetVavFromSpace( Document doc, FamilyInstance[] dampersInstances, Dictionary<int, List<Element>> parentSpaces )
    {
      var result = new Dictionary<int, List<FamilyInstance>>() ;
      foreach ( var (key, value) in parentSpaces ) {
        foreach ( var vav in from space in value select space.get_BoundingBox( doc.ActiveView ) into spaceBox select ( from fi in dampersInstances let vavPosition = fi.Location as LocationPoint where vavPosition != null where IsInSpace( spaceBox, vavPosition.Point ) select fi ).FirstOrDefault()! ) {
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

    private static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ vavPosition )
    {
      var (x, y, z) = vavPosition ;
      return spaceBox.Max.X > x && spaceBox.Max.Y > y && spaceBox.Max.Z > z && spaceBox.Min.X < x && spaceBox.Min.Y < y && spaceBox.Min.Z < z ;
    }

    private static ( IReadOnlyList<Element> grandParentSpaces, Dictionary<int, List<Element>> parentSpaces ) GetGroupSpace( IEnumerable<Element> spaceBoxes )
    {
      List<Element> grandParentSpaces = new() ;
      Dictionary<int, List<Element>> parentSpaces = new() ;
      foreach ( Element space in spaceBoxes ) {
        var branchNumber = space.GetSpaceBranchNumber() ;
        switch ( branchNumber ) {
          case (int)SpaceType.Invalid :
            continue ;
          case (int)SpaceType.GrandParent :
          {
            grandParentSpaces.Add( space ) ;
            break ;
          }
          default :
          {
            if ( parentSpaces.ContainsKey( branchNumber ) ) {
              parentSpaces[ branchNumber ].Add( space ) ;
            }
            else {
              parentSpaces.Add( branchNumber, new List<Element>() { space } ) ;
            }

            break ;
          }
        }
      }

      grandParentSpaces.Sort( Compare ) ;
      foreach ( var (key, value) in parentSpaces ) {
        parentSpaces[ key ].Sort( CompareY ) ;
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

    protected static RoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
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
      var (ahuConnector, grandParentConnectors, parentConnectors, routeProperty, classificationInfo) = selectState ;

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
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      // Main routes
      var ahuConnectorEndPoint = new ConnectorEndPoint( ahuConnector!, radius ) ;
      var vavConnectorEndPoint = new ConnectorEndPoint( grandParentConnectors.Last().GetConnectors().First( c => c.Id != VavConnectorId ), radius ) ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, ahuConnectorEndPoint, vavConnectorEndPoint, diameter, routeProperty.GetRouteOnPipeSpace(), routeProperty.GetFromFixedHeight(), sensorFixedHeight, avoidType, routeProperty.GetShaft().GetValidId() ) ) ) ;

      // Branch routes
      foreach ( var vav in grandParentConnectors.Take( grandParentConnectors.Count - 1 ) ) {
        var diameterChild = grandParentConnectors.Last().LookupParameter( "ダクト径" ).AsDouble() ;
        var radiusChild = diameterChild * 0.5 ;
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Id != VavConnectorId ), radiusChild ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameterChild, false, sensorFixedHeight, sensorFixedHeight, avoidType, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
      }

      foreach ( var (_, value) in parentConnectors ) {
        var diameterChild = value.Last().LookupParameter( "ダクト径" ).AsDouble() * 2 ;
        var radiusChild = diameterChild * 0.5 ;
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( value.Last().GetConnectors().First( c => c.Id != VavConnectorId ), radiusChild ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, diameterChild, false, sensorFixedHeight, sensorFixedHeight, avoidType, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
        foreach ( var vav in value.Take( value.Count - 1 ) ) {
          diameterChild = vav.LookupParameter( "ダクト径" ).AsDouble() ;
          radiusChild = diameterChild * 0.5 ;
          var subChildRouteName = nameBase + "_" + ( ++nextIndex ) ;
          var branchChildEndPoint = new RouteEndPoint( document, subRouteName, DefaultSubRouteIndex ) ;
          var connectorChildEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Id != VavConnectorId ), radiusChild ) ;
          var segmentChild = new RouteSegment( classificationInfo, systemType, curveType, branchChildEndPoint, connectorChildEndPoint, diameterChild, false, sensorFixedHeight, sensorFixedHeight, avoidType, ElementId.InvalidElementId ) ;
          result.Add( ( subChildRouteName, segmentChild ) ) ;
        }
      }

      return result ;
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