using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.DB.Plumbing ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using MathLib ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class LeakRoutingCommandBase : RoutingCommandBase<LeakRoutingCommandBase.LeakState>
  {
    private const string StatusPrompt = "配置場所を選択して下さい。" ;

    private UIDocument? uiDoc ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    public record LeakState(ConnectorPicker.IPickResult PickConnectorResult,List<XYZ> PickPoints, int Height, int Type, IRouteProperty PropertyDialog, MEPSystemClassificationInfo ClassificationInfo ) ;
    
    protected record DialogInitValues( MEPSystemClassificationInfo ClassificationInfo, MEPSystemType? SystemType, MEPCurveType CurveType, double Diameter ) ;
    
    protected abstract MEPSystemClassificationInfo? GetMEPSystemClassificationInfoFromSystemType( MEPSystemType? systemType ) ;

    protected abstract AddInType GetAddInType() ;
    
    protected abstract DialogInitValues? CreateSegmentDialogDefaultValuesWithConnector( Document document, Connector connector, MEPSystemClassificationInfo classificationInfo ) ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override OperationResult<LeakState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      UIDocument  uiDocument = commandData.Application.ActiveUIDocument ;
      uiDoc = uiDocument ;
      var routingExecutor = GetRoutingExecutor() ;
      
      var sv = new LeakRouteDialog() ;
      sv.ShowDialog() ;
      if ( true != sv?.DialogResult ) return OperationResult<LeakState>.Cancelled ;
      var pickConnectorResult = ConnectorPicker.GetConnector( uiDocument, routingExecutor, true, "Dialog.Commands.Routing.PickRouting.PickFirst".GetAppStringByKeyOrDefault( null ), null, GetAddInType() ) ;
      
      bool endWhile = false;
      var pickPoints = new List<XYZ>() ;

      while (endWhile == false)
      {
        try
        {
          XYZ XYZ = uiDocument.Selection.PickPoint( "Pick points then press escape to cause an exception ahem...exit selection" ) ;
          pickPoints.Add( XYZ ) ;
        }
        catch
        {
          endWhile = true;
          break; // TODO: might not be correct. Was : Exit While
        }
      }
     
      var height = sv.height ;
      var creationMode = sv.createMode ;
      
      
      var property = ShowPropertyDialog( uiDocument.Document, pickConnectorResult, pickConnectorResult ) ;
      
      if ( true != property?.DialogResult ) return OperationResult<LeakState>.Cancelled ;
      
      if ( GetMEPSystemClassificationInfo( pickConnectorResult, property.GetSystemType() ) is not { } classificationInfo ) return OperationResult<LeakState>.Cancelled ;
      
      return new OperationResult<LeakState>( new LeakState( pickConnectorResult, pickPoints, height,creationMode, property, classificationInfo) ) ;
    }
    
    private MEPSystemClassificationInfo? GetMEPSystemClassificationInfo( ConnectorPicker.IPickResult fromPickResult, MEPSystemType? systemType )
    {
      if ( ( fromPickResult.SubRoute )?.Route.GetSystemClassificationInfo() is { } routeSystemClassificationInfo ) return routeSystemClassificationInfo ;

      if ( ( fromPickResult.PickedConnector ) is { } connector && MEPSystemClassificationInfo.From( connector ) is { } connectorClassificationInfo ) return connectorClassificationInfo ;

      return GetMEPSystemClassificationInfoFromSystemType( systemType ) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)>
      GetRouteSegments( Document document, LeakState leakState )
    {
      var (pickConnectorResult, pickPoints, height,creationMode, routeProperty, classificationInfo) = leakState ;

      RouteGenerator.CorrectEnvelopes( document ) ;
      
      return CreateNewSegmentList( document, pickConnectorResult , pickPoints, height, creationMode, routeProperty, classificationInfo ) ;
    }
    
    private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> CreateNewSegmentList( Document document, ConnectorPicker.IPickResult fromPickResult, List<XYZ> pickPoints, int height, int creationMode, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var useConnectorDiameter = UseConnectorDiameter() ;
      var fromEndPoint = PickCommandUtil.GetEndPoint( fromPickResult, fromPickResult, useConnectorDiameter ) ;
      var fromOrigin = fromPickResult.GetOrigin() ;
      var fromConnectorId = fromPickResult.PickedElement.UniqueId ;
      var levelId = fromPickResult.GetLevelId() ;

      var routeSegments = CreateSegmentOfNewRoute( document, levelId, fromEndPoint, pickPoints, fromOrigin, fromConnectorId, routeProperty, classificationInfo ) ;

      return routeSegments ;
    }

    private List<(string RouteName, RouteSegment Segment)> CreateSegmentOfNewRoute( Document document, ElementId leveId ,IEndPoint fromEndPoint, List<XYZ> pickPoints, XYZ fromOrigin, string fromConnectorId, IRouteProperty routeProperty, MEPSystemClassificationInfo classificationInfo )
    {
      var systemType = routeProperty.GetSystemType() ;
      var curveType = routeProperty.GetCurveType() ;
     

      var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
      var nameBase = GetNameBase( systemType, curveType ) ;
      var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
      var name = nameBase + "_" + nextIndex ;
      routes.FindOrCreate( name ) ;

      var diameter = routeProperty.GetDiameter() ;
      var isRoutingOnPipeSpace = routeProperty.GetRouteOnPipeSpace() ;
      var fromFixedHeight = routeProperty.GetFromFixedHeight() ;
      var toFixedHeight = routeProperty.GetToFixedHeight() ;
      var avoidType = routeProperty.GetAvoidType() ;
      var shaftElementUniqueId = routeProperty.GetShaft()?.UniqueId ;

      List<(string RouteName, RouteSegment Segment)> routeSegments = new List<(string RouteName, RouteSegment Segment)>() ;
      Vector3d direction = new Vector3d( 1, 0, 0 ) ;
      
      var passPoints = new List<FamilyInstance>() ;
      var passPoint = document.AddPassPoint( name, fromOrigin, direction.normalized.ToXYZRaw(), diameter / 2, leveId! ) ;
      passPoints.Add( passPoint ) ;
      
      var passPoint2 = document.AddPassPoint( name, pickPoints[0], direction.normalized.ToXYZRaw(), diameter / 2, leveId! ) ;
      passPoints.Add( passPoint2 ) ;
      
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, new PassPointEndPoint(passPoints[0]), diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
      routeSegments.Add( ( name, new RouteSegment( classificationInfo, systemType, curveType, new PassPointEndPoint(passPoints[0]), new PassPointEndPoint(passPoints[1]), diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ) ) ;
      
      return routeSegments ;
    }

    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }

    private IRoutePropertyDialog? ShowPropertyDialog( Document document, ConnectorPicker.IPickResult fromPickResult, ConnectorPicker.IPickResult toPickResult )
    {
      var fromLevelId = GetTrueLevelId( document, fromPickResult ) ;
      var toLevelId = GetTrueLevelId( document, toPickResult ) ;
      
      if ( ( fromPickResult.PickedConnector ) is { } connector ) {
        if ( MEPSystemClassificationInfo.From( connector ) is not { } classificationInfo ) return null ;

        if ( CreateSegmentDialogDefaultValuesWithConnector( document, connector, classificationInfo ) is not { } initValues ) return null ;

        return ShowDialog( document, initValues, fromLevelId, toLevelId ) ;
      }

      return ShowDialog( document, GetAddInType(), fromLevelId, toLevelId ) ;
    }
    
    protected virtual IRoutePropertyDialog ShowDialog( Document document, LeakRoutingCommandBase.DialogInitValues initValues, ElementId fromLevelId, ElementId toLevelId )
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
    
    private static ElementId GetTrueLevelId( Document document, ConnectorPicker.IPickResult pickResult )
    {
      var levelId = pickResult.GetLevelId() ;
      if ( ElementId.InvalidElementId != levelId ) return levelId ;

      return document.GuessLevel( pickResult.GetOrigin() ).Id ;
    }
  }
}