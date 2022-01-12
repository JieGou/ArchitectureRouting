using Arent3d.Architecture.Routing.AppBase ;
using Arent3d.Architecture.Routing.AppBase.Commands.Routing ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Mechanical ;
using Autodesk.Revit.UI ;
using MathLib ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using ImageType = Arent3d.Revit.UI.ImageType ;

namespace Arent3d.Architecture.Routing.Mechanical.App.Commands.Routing
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Mechanical.App.Commands.Routing.AutoRoutingVavCommand", DefaultString = "Auto \nRouting VAVs" )]
  [Image( "resources/Initialize-32.bmp", ImageType = ImageType.Large )]
  public class AutoRoutingVavCommand : RoutingCommandBase<AutoRoutingVavCommand.SelectState>
  {
    private const int DefaultSubRouteIndex = 0 ;
    private const string ErrorMessageNoVav = "No VAV on the Root Connector level." ;
    private const string ErrorMessageNoRootConnector = "No RootConnector are selected." ;
    private const string ErrorMessageNoSpace = "Find space cannot be found." ;
    private const string ErrorMessageNoParentVav = "No VAV on the space group 0" ;
    private const string ErrorMessageVavNoInConnector = "VAVの流れの方向[イン]が設定されていないため、処理に失敗しました。" ;
    private const string ErrorMessageVavNoOutConnector = "VAVの流れの方向[アウト]が設定されていないため、処理に失敗しました。" ;
    private const string ErrorMessageAhuNumberCannotBeFound = "AHUNumber cannot be found from the picked connector.";

    private Level _rootLevel = null! ;

    protected override string GetTransactionNameKey()
    {
      return "TransactionName.Commands.Routing.AutoRoutingVav" ;
    }

    private static AddInType GetAddInType()
    {
      return AppCommandSettings.AddInType ;
    }

    protected override RoutingExecutor CreateRoutingExecutor( Document document, View view )
    {
      return AppCommandSettings.CreateRoutingExecutor( document, view ) ;
    }

    private static string GetNameBase( Element? systemType, Element curveType )
    {
      return systemType?.Name ?? curveType.Category.Name ;
    }

    protected override OperationResult<SelectState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      if ( null == GetRoundDuctTypeWhosePreferredJunctionTypeIsTee( uiDocument.Document ) ) return OperationResult<SelectState>.FailWithMessage( "Round duct type whose preferred junction type is tee not found" ) ;

      var (fromPickResult, parentVavs, childVavs, errorMessage) = SelectRootConnectorAndFindVavs( uiDocument, GetRoutingExecutor(), GetAddInType() ) ;
      if ( null != errorMessage ) return OperationResult<SelectState>.FailWithMessage( errorMessage ) ;
      _rootLevel = uiDocument.Document.GuessLevel( fromPickResult.GetOrigin() ) ;
      if ( fromPickResult.PickedConnector != null && MEPSystemClassificationInfo.From( fromPickResult.PickedConnector ) is { } classificationInfo ) return new OperationResult<SelectState>( new SelectState( fromPickResult.PickedConnector, parentVavs, childVavs, classificationInfo ) ) ;

      return OperationResult<SelectState>.Cancelled ;
    }

    private static (ConnectorPicker.IPickResult fromPickResult, IReadOnlyList<FamilyInstance> parentVavs, Dictionary<int, List<FamilyInstance>> childVavs, string? ErrorMessage) SelectRootConnectorAndFindVavs( UIDocument uiDocument, RoutingExecutor routingExecutor, AddInType addInType )
    {
      var doc = uiDocument.Document ;

      // Select Root Connector
      var fromPickResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.AutoRouteVavs.PickRootConnector".GetAppStringByKeyOrDefault( null ), null, addInType ) ;
      if ( fromPickResult.PickedConnector == null ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoRootConnector ) ;

      // Get AHUNumber
      var ahuNumber = TTEUtil.GetAhuNumberByPickedConnector( fromPickResult.PickedConnector! ) ;
      if ( ! ahuNumber.HasValue ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageAhuNumberCannotBeFound ) ;

      // Get all space has specified AHUNumber
      var spaces = GetAllSpacesHasSpecifiedAhuNumber( doc, ahuNumber.Value ) ;
      if ( ! spaces.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoSpace ) ;

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

      // Get group space
      var (parentSpaces, childSpacesGroupedByBranchNum) = GetSortedSpaceGroups( spaces, fromPickResult.PickedConnector ) ;

      var parentVavs = parentSpaces.ConvertAll( space => GetVavFromSpace( doc, vavInstances, space ) ) ;
      var childVavs = GetVavsFromSpaces( doc, vavInstances, childSpacesGroupedByBranchNum ) ;

      if ( ! parentVavs.Any() ) return ( null!, Array.Empty<FamilyInstance>(), new Dictionary<int, List<FamilyInstance>>(), ErrorMessageNoParentVav ) ;

      return ( fromPickResult, parentVavs, childVavs, null ) ;
    }

    /// <summary>
    ///   Get one Vav from one space
    /// </summary>
    private static FamilyInstance GetVavFromSpace( Document doc, IEnumerable<FamilyInstance> vavInstances, Element space )
    {
      var spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
      foreach ( var vavInstance in vavInstances ) {
        var vavPosition = vavInstance.Location as LocationPoint ;
        if ( vavPosition == null || ! IsInSpace( spaceBox, vavPosition.Point ) ) continue ;
        return vavInstance ;
      }

      return null! ;
    }

    /// <summary>
    ///   Get multi Vav from multi space
    /// </summary>
    private static Dictionary<int, List<FamilyInstance>> GetVavsFromSpaces( Document doc, FamilyInstance[] vavInstances, Dictionary<int, List<Element>> childSpacesGroupedByBranchNum )
    {
      var result = new Dictionary<int, List<FamilyInstance>>() ;
      foreach ( var (branchNum, spaces) in childSpacesGroupedByBranchNum )
      foreach ( var space in spaces ) {
        var spaceBox = space.get_BoundingBox( doc.ActiveView ) ;
        foreach ( var vavInstance in vavInstances ) {
          var vavPosition = vavInstance.Location as LocationPoint ;
          if ( vavPosition == null ) continue ;
          if ( IsInSpace( spaceBox, vavPosition.Point ) ) {
            if ( result.ContainsKey( branchNum ) )
              result[ branchNum ].Add( vavInstance ) ;
            else
              result.Add( branchNum, new List<FamilyInstance> { vavInstance } ) ;

            break ;
          }
        }
      }

      return result ;
    }

    private static bool IsInSpace( BoundingBoxXYZ spaceBox, XYZ vavPosition )
    {
      return spaceBox.ToBox3d().Contains( vavPosition.To3dPoint(), 0.0 ) ;
    }

    private static (IReadOnlyList<Element> parentSpaces, Dictionary<int, List<Element>> childSpacesGroupedByBranchNum) GetSortedSpaceGroups( IEnumerable<Element> spaceBoxes, IConnector rootConnector )
    {
      List<Element> parentSpaces = new() ;
      Dictionary<int, List<Element>> childSpacesGroupedByBranchNum = new() ;
      foreach ( var space in spaceBoxes ) {
        var branchNumber = space.GetSpaceBranchNumber() ;
        if ( ! TTEUtil.IsValidBranchNumber( branchNumber ) ) continue ;
        
        switch ( branchNumber ) {
          case (int) SpaceType.Invalid :
            continue ;
          case (int) SpaceType.Parent :
          {
            parentSpaces.Add( space ) ;
            break ;
          }
          default :
          {
            if ( childSpacesGroupedByBranchNum.ContainsKey( branchNumber ) )
              childSpacesGroupedByBranchNum[ branchNumber ].Add( space ) ;
            else
              childSpacesGroupedByBranchNum.Add( branchNumber, new List<Element> { space } ) ;

            break ;
          }
        }
      }

      parentSpaces.Sort( ( a, b ) => CompareDistanceBasisZ( rootConnector, a, b, false ) ) ;

      foreach ( var (key, _) in childSpacesGroupedByBranchNum ) childSpacesGroupedByBranchNum[ key ].Sort( ( a, b ) => CompareDistanceBasisZ( rootConnector, a, b, true ) ) ;

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

    private static IList<Element> GetAllSpacesHasSpecifiedAhuNumber( Document document, int ahuNumber )
    {
      ElementCategoryFilter filter = new(BuiltInCategory.OST_MEPSpaces) ;
      FilteredElementCollector collector = new(document) ;
      IList<Element> spaces = collector.WherePasses( filter ).WhereElementIsNotElementType().Where( TTEUtil.HasValidBranchNumber ).Where( space => TTEUtil.HasSpecifiedAhuNumber( space, ahuNumber ) ).ToList() ;
      return spaces ;
    }

    private static MEPCurveType? GetRoundDuctTypeWhosePreferredJunctionTypeIsTee( Document document )
    {
      return document.GetAllElements<MEPCurveType>().FirstOrDefault( type => type.PreferredJunctionType == JunctionType.Tee && type is DuctType && type.Shape == ConnectorProfileType.Round ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, SelectState selectState )
    {
      var (rootConnector, parentVavs, childVavs, classificationInfo) = selectState ;
      if ( rootConnector == null ) throw new InvalidOperationException() ;
      var systemType = document.GetAllElements<MEPSystemType>().Where( classificationInfo.IsCompatibleTo ).FirstOrDefault() ;
      var curveType = GetRoundDuctTypeWhosePreferredJunctionTypeIsTee( document )! ; // 取得できることはこれより前に確認済み.
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( RouteCache.Get( document ), nameBase ) ;
      var routeName = nameBase + "_" + nextIndex ;
      document.Regenerate() ; // Apply Arent-RoundDuct-Diameter
      var result = new List<(string RouteName, RouteSegment Segment)>() ;

      // Main routes
      var rootConnectorEndPoint = new ConnectorEndPoint( rootConnector, null ) ;
      var vavConnectorEndPoint = new ConnectorEndPoint( parentVavs.Last().GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
      var mainRouteHeight = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, rootConnector.Origin.Z - _rootLevel.Elevation ) ;
      result.Add( ( routeName, new RouteSegment( classificationInfo, systemType, curveType, rootConnectorEndPoint, vavConnectorEndPoint, null, false, mainRouteHeight, mainRouteHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ) ) ;

      // Branch routes
      foreach ( var vav in parentVavs.Take( parentVavs.Count - 1 ) ) {
        // メインダクト - VAV
        var childDiameter = parentVavs.Last().LookupParameter( "ダクト径" ).AsDouble() ;
        var subRouteName = nameBase + "_" + ++nextIndex ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, childDiameter, false, null, null, AvoidType.Whichever, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
      }

      foreach ( var (_, childVav) in childVavs ) {
        // サブメインダクト
        var childDiameter = childVav.Last().LookupParameter( "ダクト径" ).AsDouble() * 2 ;
        var subRouteName = nameBase + "_" + ++nextIndex ;
        var branchEndPoint = new RouteEndPoint( document, routeName, DefaultSubRouteIndex ) ;
        var connectorEndPoint = new ConnectorEndPoint( childVav.Last().GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
        var segment = new RouteSegment( classificationInfo, systemType, curveType, branchEndPoint, connectorEndPoint, childDiameter, false, mainRouteHeight, mainRouteHeight, AvoidType.Whichever, ElementId.InvalidElementId ) ;
        result.Add( ( subRouteName, segment ) ) ;
        foreach ( var vav in childVav.Take( childVav.Count - 1 ) ) {
          // サブメインダクト - VAV
          childDiameter = vav.LookupParameter( "ダクト径" ).AsDouble() ;
          var subChildRouteName = nameBase + "_" + ++nextIndex ;
          var branchChildEndPoint = new RouteEndPoint( document, subRouteName, DefaultSubRouteIndex ) ;
          var connectorChildEndPoint = new ConnectorEndPoint( vav.GetConnectors().First( c => c.Direction != FlowDirectionType.Out ), null ) ;
          var childSegment = new RouteSegment( classificationInfo, systemType, curveType, branchChildEndPoint, connectorChildEndPoint, childDiameter, false, null, null, AvoidType.Whichever, ElementId.InvalidElementId ) ;
          result.Add( ( subChildRouteName, childSegment ) ) ;
        }
      }

      return result ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      var pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    public record SelectState( Connector? RootConnector, IReadOnlyList<FamilyInstance> ParentVavs, Dictionary<int, List<FamilyInstance>> ChildVavs, MEPSystemClassificationInfo ClassificationInfo ) ;
  }
}