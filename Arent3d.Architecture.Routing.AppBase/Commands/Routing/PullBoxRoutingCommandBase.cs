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
    public record PickState(Route Route, IEndPoint FromEndPoint, IEndPoint ToEndPoint, FamilyInstance PullBox, double HeightConnector, double HeightWire) ;
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
      var route = PointOnRoutePicker.PickRoute( uiDocument, false, "Pick point on Route", GetAddInType() ) ;
      var sv = new PullBoxDialog() ;
      sv.ShowDialog() ;
      if ( true != sv?.DialogResult ) return OperationResult<PickState>.Cancelled ;
      var heightConnector = sv.HeightConnector ;
      var heightWire = sv.HeightWire ;
      
      using Transaction t = new Transaction( document, "Create connector" ) ;
      t.Start() ;
      var (originX, originY, originZ) = route.Position ;
      var level = uiDocument.ActiveView.GenLevel ;
      var connector = GenerateConnector( uiDocument, originX, originY,  heightConnector.MillimetersToRevitUnits(), level ) ;
      t.Commit() ;
      var (fromEndPoint, toEndPoint) = GetChangingEndPoint( uiDocument, route.Route ) ;

      return new OperationResult<PickState>(new PickState( route.Route, fromEndPoint, toEndPoint, connector,heightConnector, heightWire )) ;
    }
    
     protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState)
     {
       var (route, fromEndPoint, toEndPoint, pullBox, heightConnector, heightWire) = pickState ;
       var diameter = route.UniqueDiameter ;
       var classificationInfo = route.GetSystemClassificationInfo() ;
       var systemType = route.GetMEPSystemType() ;
       var curveType = route.UniqueCurveType ;
       var radius = diameter * 0.5 ;
       var isRoutingOnPipeSpace = route.UniqueIsRoutingOnPipeSpace ?? false ;
       var toFixedHeight = route.UniqueToFixedHeight ;
       var avoidType = route.UniqueAvoidType ?? AvoidType.Whichever ;
       var shaftElementUniqueId = route.UniqueShaftElementUniqueId ;
       var fromFixedHeightFirst = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightConnector.MillimetersToRevitUnits() + (250.0).MillimetersToRevitUnits()) ;
       var fromFixedHeightSecond = FixedHeight.CreateOrNull( FixedHeightType.Ceiling, heightWire.MillimetersToRevitUnits() ) ;
       
       var routes = RouteCache.Get( DocumentKey.Get( document ) ) ;
       var nameBase = GetNameBase( systemType, curveType! ) ;
       var nextIndex = GetRouteNameIndex( routes, nameBase ) ;
       var name = nameBase + "_" + nextIndex ;
       routes.FindOrCreate( name ) ;

       var pullBoxEndPointTop = new ConnectorEndPoint( pullBox.GetTopConnectorOfConnectorFamily(), radius ) ;
       var pullBoxEndPointBottom = new ConnectorEndPoint( pullBox.GetBottomConnectorOfConnectorFamily(), radius ) ;
       
       var result = new List<(string RouteName, RouteSegment Segment)>() ;
       
       var segment = new RouteSegment( classificationInfo, systemType, curveType, fromEndPoint, pullBoxEndPointTop, diameter, isRoutingOnPipeSpace, fromFixedHeightFirst, toFixedHeight, avoidType, shaftElementUniqueId ) ;
       result.Add( (name,segment) );
       
       var segment2 = new RouteSegment( classificationInfo, systemType, curveType, pullBoxEndPointBottom, toEndPoint, diameter, isRoutingOnPipeSpace, fromFixedHeightSecond, toFixedHeight, avoidType, shaftElementUniqueId ) ;
       result.Add( (nameBase + "_" + (nextIndex + 1), segment2) );
       
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
      
      var array = route.RouteSegments.SelectMany( GetReplaceableEndPoints ).ToArray() ;

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
  }
}