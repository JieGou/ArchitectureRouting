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
using Autodesk.Revit.DB.Structure ;



namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing
{
  public abstract class PullBoxRoutingCommandBase : RoutingCommandBase<PullBoxRoutingCommandBase.PickState>
  {
    public record PickState( string Height) ;
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    
    private const string DefaultConstructionItem = "未設定" ;
    protected virtual ConnectorFamilyType? ConnectorType => null ;
    protected abstract AddInType GetAddInType() ;
    private bool UseConnectorDiameter() => ( AddInType.Electrical != GetAddInType() ) ;
    
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
      GenerateConnector( uiDocument, originX, originY,  sv.HeightConnector.MillimetersToRevitUnits() , level ) ;

      t.Commit() ;
      
      return new OperationResult<PickState>(new PickState( "height" )) ;
    }

    protected override IReadOnlyCollection<(string RouteName, RouteSegment Segment)> GetRouteSegments( Document document, PickState pickState )
    {
      return new List<(string RouteName, RouteSegment Segment)>() ;
    }
    
    private void GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
       
      //Set value for isEcoMode property from default value in DB
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetEcoSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
 
      if ( ConnectorType == null ) return ;
      instance.SetConnectorFamilyType( ConnectorType ?? ConnectorFamilyType.Sensor ) ;
    }
  }
}