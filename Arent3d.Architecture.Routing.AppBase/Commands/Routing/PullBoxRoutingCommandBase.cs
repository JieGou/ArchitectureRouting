using System.Collections.Generic ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.EndPoints ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using System ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.UI ;
using Arent3d.Architecture.Routing.StorableCaches ;
using Autodesk.Revit.DB.Structure ;



namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    public record PickState(Route Route, IEndPoint FromEndPoint, IEndPoint ToEndPoint, FamilyInstance PullBox, double Height) ;
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    
    private const string DefaultConstructionItem = "未設定" ;
    protected virtual ConnectorFamilyType? ConnectorType => null ;
    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    
    protected abstract string GetNameBase( MEPSystemType? systemType, MEPCurveType curveType ) ;
    
    protected override OperationResult<PickState> OperateUI( ExternalCommandData commandData, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      var routingExecutor = GetRoutingExecutor() ;
      var route = PointOnRoutePicker.PickRoute( uiDocument, false, "Dialog.Commands.Routing.ReplaceFromTo.Pick".GetAppStringByKeyOrDefault( null ), GetAddInType() ) ;
      var sv = new PullBoxDialog() ;
      sv.ShowDialog() ;
      if ( true != sv?.DialogResult ) return OperationResult<PickState>.Cancelled ;
      
      using Transaction t = new Transaction( document, "Create connector" ) ;
      t.Start() ;
      var (originX, originY, originZ) = route.Position ;
      var level = uiDocument.ActiveView.GenLevel ;
      var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
      var connector = GenerateConnector( uiDocument, originX, originY,  sv.HeightConnector.MillimetersToRevitUnits() , level ) ;
      t.Commit() ;
      //var powerConnectorEndPoint = new ConnectorEndPoint( connector.GetTopConnectorOfConnectorFamily(), 1 ) ;
      var (fromEndPoint, toEndPoint) = GetChangingEndPoint( uiDocument, route.Route ) ;

      // var property = CreateRouteProperties( document, route.Route, 100 ) ;
      // if ( property == null ) return OperationResult<PickState>.Cancelled ;
      
      return new OperationResult<PickState>(new PickState( route.Route, fromEndPoint, toEndPoint, connector, 100 )) ;
    }
    
     protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState)
     {
       var (route, fromEndPoint, toEndPoint, pullBox, height) = pickState ;
       var diameter = route.UniqueDiameter ;
       var classificationInfo = route.GetSystemClassificationInfo() ;
       var systemType = route.GetMEPSystemType() ;
       var curveType = route.UniqueCurveType ;
       var radius = diameter * 0.5 ;
       var isRoutingOnPipeSpace = route.UniqueIsRoutingOnPipeSpace ?? false ;
       var fromFixedHeight = route.UniqueFromFixedHeight ;
       var toFixedHeight = route.UniqueToFixedHeight ;
       var avoidType = route.UniqueAvoidType ?? AvoidType.Whichever ;
       var shaftElementUniqueId = route.UniqueShaftElementUniqueId ;
       
       var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
       var nameBase = GetNameBase( systemType, curveType! ) ;
       var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
       var name = nameBase + "_" + nextIndex ;
       routes.FindOrCreate( name ) ;
       
       var con1 = route.FirstFromConnector() ;
       var con2 = route.FirstToConnector() ;
       
       var level = document.ActiveView.GenLevel ;
       
       var pullBoxEndPoint = new ConnectorEndPoint( pullBox.GetTopConnectorOfConnectorFamily(), radius ) ;
       
       var pullBoxEndPoint2 = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily(), radius ) ;
       
       var result = new List<(string RouteName, RouteSegment Segment)>() ;
       
       var segment = new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, pullBoxEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
       result.Add( (name,segment) );
       
       var segment2 = new RouteSegment( classificationInfo, systemType, curveType, pullBoxEndPoint2, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeight, toFixedHeight, avoidType, shaftElementUniqueId ) ;
       result.Add( (nameBase + "_" + (nextIndex  + 1),segment2) );
       
       List<string> listNameRoute = new List<string> { route.Name } ;
       RouteGenerator.EraseRoutes( document,  listNameRoute , true ) ;
       
       return result ;
     }
     
     private IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetSelectedRouteSegments( Document document, IEnumerable<Route> pickedRoutes )
     {
       var selectedRoutes = Route.CollectAllDescendantBranches( pickedRoutes ) ;

       var recreatedRoutes = Route.GetAllRelatedBranches( selectedRoutes ) ;
       recreatedRoutes.ExceptWith( selectedRoutes ) ;
       RouteGenerator.EraseRoutes( document, selectedRoutes.ConvertAll( route => route.RouteName ), true ) ;

       // Returns affected but not deleted routes to recreate them.
       return recreatedRoutes.ToSegmentsWithName().EnumerateAll() ;
     }
     
    private static (IEndPoint fromEndPoint,IEndPoint toEndPoint) GetChangingEndPoint( UIDocument uiDocument, Route route )
    {
      using var _ = new TempZoomToFit( uiDocument ) ;

      var message = "Dialog.Commands.Routing.ReplaceFromTo.SelectFromTo".GetAppStringByKeyOrDefault( "Select which end is to be changed." ) ;

      var array = route.RouteSegments.SelectMany( GetReplaceableEndPoints ).ToArray() ;
      // TODO: selection ui

      return (array[ 0 ], array [1]) ;
    }

    private static IEnumerable<IEndPoint> GetReplaceableEndPoints( RouteSegment segment )
    {
      if ( segment.FromEndPoint.IsReplaceable ) yield return segment.FromEndPoint ;
      if ( segment.ToEndPoint.IsReplaceable ) yield return segment.ToEndPoint ;
    }
    
    private FamilyInstance GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      if ( instance.HasParameter( ElectricalRoutingElementParameter.ConstructionItem) ) 
        instance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
       
      //Set value for isEcoMode property from default value in DB
      if ( instance.HasParameter( ElectricalRoutingElementParameter.IsEcoMode ) )  
        instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetEcoSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
      
      instance.SetConnectorFamilyType( ConnectorType ?? ConnectorFamilyType.Sensor ) ;
      
      return instance ;
    }
    
    private static int GetRouteNameIndex( RouteCache routes, string? targetName )
    {
      string pattern = @"^" + Regex.Escape( targetName ?? string.Empty ) + @"_(\d+)$" ;
      var regex = new Regex( pattern ) ;

      var lastIndex = routes.Keys.Select( k => regex.Match( k ) ).Where( m => m.Success ).Select( m => int.Parse( m.Groups[ 1 ].Value ) ).Append( 0 ).Max() ;

      return lastIndex + 1 ;
    }
    
    private RouteProperties CreateRouteProperties( Document document, Route route,  double height )
    {
      MEPSystemType? mepSystemType = route.GetMEPSystemType() ;
      MEPCurveType? mepCurveType = route.UniqueCurveType ;
      Opening? shaft = null ;
      if ( route.UniqueShaftElementUniqueId is not { } shaftElementId ||
           false == string.IsNullOrEmpty( shaftElementId ) ) shaft = null ;
      else  shaft =  route.Document.GetElementById<Opening>( shaftElementId ) ;
      
      var routeProperty = new RouteProperties( document, mepSystemType , mepCurveType, route.UniqueDiameter, route.UniqueIsRoutingOnPipeSpace, true, FixedHeight.CreateOrNull( FixedHeightType.Ceiling, height ), null, null, route.UniqueAvoidType, shaft ) ;
      
      return routeProperty ;
    }
  }
}