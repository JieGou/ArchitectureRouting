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
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class AutoRoutingVavCommandBase : RoutingCommandBase
  {
    private const int DefaultSubRouteIndex = 0 ;
    private const string ErrorMessageNoVav = "No VAV on the Root Connector level." ;
    private const string ErrorMessageNoRootConnector = "No RootConnector are selected." ;
    private const string ErrorMessageNoSpace = "Find space cannot be found." ;
    private const string ErrorMessageNoParentVav = "No VAV on the space group 0" ;
    private const string ErrorMessageVavNoInConnector = "VAVの流れの方向[イン]が設定されていないため、処理に失敗しました。" ;
    private const string ErrorMessageVavNoOutConnector = "VAVの流れの方向[アウト]が設定されていないため、処理に失敗しました。" ;

    private Level _rootLevel = null! ;
    
    protected abstract AddInType GetAddInType() ;

    private record SelectState( Connector? RootConnector, IReadOnlyList<FamilyInstance> ParentVavs, Dictionary<int, List<FamilyInstance>> ChildVavs, AutoVavRoutePropertyDialog PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;

    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;

    protected override (bool Result, object? State) OperateUI( UIDocument uiDocument, RoutingExecutor routingExecutor )
    {
      if ( null == GetRoundDuctTypeWhosePreferredJunctionTypeIsTee( uiDocument.Document ) ) {
        return ( false, "Round duct type whose preferred junction type is tee is not found" ) ;
      }
      
      var (fromPickResult, parentVavs, childVavs, errorMessage) = SelectRootConnectorAndFindVavs( uiDocument, routingExecutor, GetAddInType() ) ;
      if ( null != errorMessage ) return ( false, errorMessage ) ;
      _rootLevel = uiDocument.Document.GuessLevel( fromPickResult.GetOrigin() ) ;
      var property = ShowPropertyDialog( uiDocument.Document, fromPickResult ) ;
      if ( true != property?.DialogResult ) return ( false, null ) ;
      if ( GetMEPSystemClassificationInfoFromSystemType( property.GetSystemType() ) is { } classificationInfo ) return ( true, new SelectState( fromPickResult.PickedConnector, parentVavs, childVavs, property, classificationInfo ) ) ;
      return ( false, null ) ;
    }

    private static (ConnectorPicker.IPickResult fromPickResult, IReadOnlyList<FamilyInstance> parentVavs, Dictionary<int, List<FamilyInstance>> childVavs, string? ErrorMessage) SelectRootConnectorAndFindVavs( UIDocument uiDocument, RoutingExecutor routingExecutor, AddInType addInType )
    {
      var doc = uiDocument.Document ;

      // Select Root Connector
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.AutoRouteVavs.PickRootConnector".GetAppStringByKeyOrDefault( null ), null, addInType ) ;
      if ( fromPickResult.PickedConnector == null ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoRootConnector ) ;

      // Get all vav
      var vavs = doc.GetAllFamilyInstances( RoutingFamilyType.TTE_VAV_140 ) ;
      var vavInstances = vavs as FamilyInstance[] ?? vavs.ToArray() ;
      if ( ! vavInstances.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoVav ) ;

      // Check IN/OUT connectors for all VAV instances
      foreach ( var vavInstance in vavInstances ) {
        var vavInConnectorExists = vavInstance.GetConnectors().Any( c => c.Direction == FlowDirectionType.In ) ;
        if ( ! vavInConnectorExists ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageVavNoInConnector ) ;
        var vavOutConnectorExists = vavInstance.GetConnectors().Any( c => c.Direction == FlowDirectionType.Out ) ;
        if ( ! vavOutConnectorExists ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageVavNoOutConnector ) ;
      }
      
      // Get all space
      var spaces = GetAllSpaces( doc ) ;
      if ( ! spaces.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoSpace ) ;

      // Get group space
      var (parentSpaces, childSpacesGroupedByBranchNum) = GetSortedSpaceGroups( spaces, fromPickResult.PickedConnector ) ;

      var parentVavs = parentSpaces.ConvertAll( space => GetVavFromSpace( doc, vavInstances, space ) ) ;
      var childVavs = GetVavsFromSpaces( doc, vavInstances, childSpacesGroupedByBranchNum ) ;

      if ( ! parentVavs.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoParentVav ) ;

      return ( fromPickResult, parentVavs, childVavs, null ) ;
    }

    /// <summary>
    /// Get one Vav from one space
    /// </summary>
    private static FamilyInstance GetVavFromSpace( Document doc, IEnumerable<FamilyInstance> vavInstances, Element space )
    {
      BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
      foreach ( var vavInstance in vavInstances ) {
        var vavPosition = vavInstance.Location as LocationPoint ;
        if ( vavPosition == null || ( ! IsInSpace( spaceBox, vavPosition.Point ) ) ) continue ;
        return vavInstance ;
      }

      return null! ;
    }

    /// <summary>
    /// Get multi Vav from multi space 
    /// </summary>
    private static Dictionary<int, List<FamilyInstance>> GetVavsFromSpaces( Document doc, FamilyInstance[] vavInstances, Dictionary<int, List<Element>> childSpacesGroupedByBranchNum )
    {
      var result = new Dictionary<int, List<FamilyInstance>>() ;
      foreach ( var (branchNum, spaces) in childSpacesGroupedByBranchNum ) {
        foreach ( var space in spaces ) {
          BoundingBoxXYZ spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
          foreach ( var vavInstance in vavInstances ) {
            var vavPosition = vavInstance.Location as LocationPoint ;
            if ( vavPosition == null ) continue ;
            if ( IsInSpace( spaceBox, vavPosition.Point ) ) {
              if ( result.ContainsKey( branchNum ) ) {
                result[ branchNum ].Add( vavInstance ) ;
              }
              else {
                result.Add( branchNum, new List<FamilyInstance>() { vavInstance } ) ;
              }

              break ;
            }
          }
        }
      }

      return result ;
    }

    private static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ vavPosition )
    {
      return spaceBox.ToBox3d().Contains( vavPosition.To3dPoint(), 0.0 ) ;
    }

    private static (IReadOnlyList<Element> parentSpaces, Dictionary<int, List<Element>> childSpacesGroupedByBranchNum) GetSortedSpaceGroups( IEnumerable<Element> spaceBoxes, Connector rootConnector )
    {
      List<Element> parentSpaces = new() ;
      Dictionary<int, List<Element>> childSpacesGroupedByBranchNum = new() ;
      foreach ( Element space in spaceBoxes ) {
        var branchNumber = space.GetSpaceBranchNumber() ;
        switch ( branchNumber ) {
          case (int)SpaceType.Invalid :
            continue ;
          case (int)SpaceType.Parent :
          {
            parentSpaces.Add( space ) ;
            break ;
          }
          default :
          {
            if ( childSpacesGroupedByBranchNum.ContainsKey( branchNumber ) ) {
              childSpacesGroupedByBranchNum[ branchNumber ].Add( space ) ;
            }
            else {
              childSpacesGroupedByBranchNum.Add( branchNumber, new List<Element>() { space } ) ;
            }

            break ;
          }
        }
      }

      parentSpaces.Sort( ( a, b ) => CompareDistanceBasisZ( rootConnector, a, b, false ) ) ;

      foreach ( var (key, value) in childSpacesGroupedByBranchNum ) {
        childSpacesGroupedByBranchNum[ key ].Sort( ( a, b ) => CompareDistanceBasisZ( rootConnector, a, b, true ) ) ;
      }

      return ( parentSpaces, childSpacesGroupedByBranchNum ) ;
    }

    private static int CompareDistanceBasisZ( IConnector rootConnector, Element a, Element b, bool isRotate90 )
    {
      if ( a.Location is not LocationPoint aPos || b.Location is not LocationPoint bPos ) return default ;

      return DistanceFromRoot( rootConnector, aPos, isRotate90 ).CompareTo( DistanceFromRoot( rootConnector, bPos, isRotate90 ) ) ;
    }

    private static double DistanceFromRoot( IConnector rootConnector, LocationPoint targetConnectorPos, bool isRotate90 )
    {
      var rootConnectorPosXyz = rootConnector.Origin ;
      var rootConnectorPos2d = rootConnectorPosXyz.To3dPoint().To2d() ;
      var targetConnector = targetConnectorPos.Point.To3dPoint().To2d() ;

      var rootConnectorBasisZ = rootConnector.CoordinateSystem.BasisZ.To3dPoint().To2d() ;
      var calculateDir = isRotate90 ? new Vector2d( -rootConnectorBasisZ.y, rootConnectorBasisZ.x ) : rootConnectorBasisZ ;
      var rootToVavVector = targetConnector - rootConnectorPos2d ;
      var angle = GetAngleBetweenVector( calculateDir, rootToVavVector ) ;

      return Math.Abs( Math.Cos( angle ) * rootToVavVector.magnitude ) ;
    }

    // Get the angle between two vectors
    private static double GetAngleBetweenVector( Vector2d rootVec, Vector2d otherVector )
    {
      // return the angle (in radian)
      return Math.Acos( Vector2d.Dot( rootVec, otherVector ) / ( rootVec.magnitude * otherVector.magnitude ) ) ;
    }

    private static IList<Element> GetAllSpaces( Document document )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().ToElements() ;
      return spaces ;
    }

    private AutoVavRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult )
    {
      if ( fromPickResult.PickedConnector == null ) return null ;
      if ( MEPSystemClassificationInfo.From( fromPickResult.PickedConnector ) is not { } classificationInfo ) return null ;
      return CreateSegmentDialogDefaultValuesWithConnector( document, fromPickResult.PickedConnector, classificationInfo ) is not { } initValues ? null : ShowDialog( document, initValues, _rootLevel.Id ) ;
    }

    protected static AutoVavRoutePropertyDialog ShowDialog( Document document, DialogInitValues initValues, ElementId fromLevelId )
    {
      var routeChoiceSpec = new RoutePropertyTypeList( document, initValues.ClassificationInfo, fromLevelId, fromLevelId ) ;
      var sv = new AutoVavRoutePropertyDialog( document, routeChoiceSpec, new RouteProperties( document, initValues.ClassificationInfo, initValues.SystemType, initValues.CurveType, routeChoiceSpec.StandardTypes?.FirstOrDefault(), initValues.Diameter ) ) ;

      sv.ShowDialog() ;

      return sv ;
    }

    private static MEPCurveType? GetRoundDuctTypeWhosePreferredJunctionTypeIsTee(Document document)
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( 
        type  => type.PreferredJunctionType == JunctionType.Tee 
                 && type is DuctType
                 && type.Shape == ConnectorProfileType.Round
      ) ;
    }
    
    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, object? state )
    {
      var selectState = state as SelectState ?? throw new InvalidOperationException() ;
      var (rootConnector, parentVavs, childVavs, routeProperty, classificationInfo) = selectState ;
      if ( rootConnector == null ) throw new InvalidOperationException() ;
      var systemType = routeProperty.GetSystemType() ;
      var curveType = GetRoundDuctTypeWhosePreferredJunctionTypeIsTee( document )! ; // 取得できることはこれより前に確認済み.
      
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
      var rootConnectorEndPoint = new ConnectorEndPoint( rootConnector, null ) ;
      var vavConnectorEndPoint = new ConnectorEndPoint( parentVavs.Last().GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
      var mainRouteHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, rootConnector.Origin.Z - _rootLevel.Elevation ) ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, rootConnectorEndPoint, vavConnectorEndPoint, diameter, false, mainRouteHeight, mainRouteHeight, avoidType, ElementId.InvalidElementId ) ) ) ;

      // Branch routes
      foreach ( var vav in parentVavs.Take( parentVavs.Count - 1 ) ) {
        // メインダクト - VAV
        var childDiameter = parentVavs.Last().LookupParameter( "ダクト径" ).AsDouble() ;
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, childDiameter, false, null, null, avoidType, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
      }

      foreach ( var (_, childVav) in childVavs ) {
        // サブメインダクト
        var childDiameter = childVav.Last().LookupParameter( "ダクト径" ).AsDouble() * 2 ;
        var subRouteName = nameBase + "_" + ( ++nextIndex ) ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( childVav.Last().GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, childDiameter, false, sensorFixedHeight, null, avoidType, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
        foreach ( var vav in childVav.Take( childVav.Count - 1 ) ) {
          // サブメインダクト - VAV
          childDiameter = vav.LookupParameter( "ダクト径" ).AsDouble() ;
          var subChildRouteName = nameBase + "_" + ( ++nextIndex ) ;
          var branchChildEndPoint = new RouteEndPoint( document, subRouteName, DefaultSubRouteIndex ) ;
          var connectorChildEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
          var childSegment = new RouteSegment( classificationInfo, systemType, curveType, branchChildEndPoint, connectorChildEndPoint, childDiameter, false, null, null, avoidType, ElementId.InvalidElementId ) ;
          result.Add( ( subChildRouteName, childSegment ) ) ;
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