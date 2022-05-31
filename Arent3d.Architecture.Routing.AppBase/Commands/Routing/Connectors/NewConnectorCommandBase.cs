using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Routing.Connectors
{
  public abstract class NewConnectorCommandBase : IExternalCommand
  {
    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }
    private const string DefaultConstructionItem = "未設定" ;
    protected virtual ConnectorFamilyType? ConnectorType => null ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var (originX, originY, originZ) = uiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;

        var result = document.Transaction( "TransactionName.Commands.Rack.Import".GetAppStringByKeyOrDefault( "Import Pipe Spaces" ), _ =>
        {
          var level = uiDocument.ActiveView.GenLevel ;
          var heightOfConnector = document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
          GenerateConnector( uiDocument, originX, originY, heightOfConnector, level ) ;

          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception e ) {
        CommandUtils.DebugAlertException( e ) ;
        return Result.Failed ;
      }
    }

    private void GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      var instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.ConstructionItem, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
       
      //Set value for isEcoMode property from default value in DB
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetDefaultSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
 
      if ( ConnectorType == null ) return ;
      instance.SetConnectorFamilyType( ConnectorType ?? ConnectorFamilyType.Sensor ) ;
    }
  }
}