using System ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Routing.Connectors
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Routing.NewPowerConnectorCommand", DefaultString = "New Power Connector" )]
  [Image( "resources/new_connector_02.png" )]
  public class NewPowerConnectorCommand : IExternalCommand
  {
    private ElectricalRoutingFamilyType ElectricalRoutingFamilyType => ElectricalRoutingFamilyType.ConnectorOneSide ;
    private const string DefaultConstructionItem = "未設定" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var document = uiDocument.Document ;
      try {
        var (originX, originY, _) = uiDocument.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;

        var result = document.Transaction( "TransactionName.Commands.Routing.NewPowerConnectorCommand", _ =>
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
      instance.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
    }
  }
}