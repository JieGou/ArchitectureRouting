using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowRegistrationOfBoardDataCommandBase : IExternalCommand
  {
     private const string StatusPrompt = "配置場所を選択して下さい。" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var uiDocument = commandData.Application.ActiveUIDocument ;
      var defaultConstructionItem = uiDocument.Document.GetDefaultConstructionItem() ;
      try {
        var viewModel = new RegistrationOfBoardDataViewModel( uiDocument.Document ) ;
        var dlgRegistrationOfBoardDataModel = new RegistrationOfBoardDataDialog( viewModel ) ;

        dlgRegistrationOfBoardDataModel.ShowDialog() ;
        if ( ! ( dlgRegistrationOfBoardDataModel.DialogResult ?? false ) )
          return Result.Cancelled ;
        
        if ( string.IsNullOrEmpty( viewModel.CellSelectedAutoControlPanel ) && string.IsNullOrEmpty( viewModel.CellSelectedSignalDestination ) )
          return Result.Succeeded ;

        var result = uiDocument.Document.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
        {
          var point = uiDocument.Selection.PickPoint( StatusPrompt ) ;
          var level = uiDocument.ActiveView.GenLevel ;
          var heightOfConnector = uiDocument.Document.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
          var elementFromToPower = GenerateConnector( uiDocument, point.X, point.Y, heightOfConnector, level, viewModel.IsFromPowerConnector ) ;
          var elementConnectorPower = GeneratePowerConnector( uiDocument, point.X, point.Y - 0.5, heightOfConnector + 100.0.MillimetersToRevitUnits(), level ) ;

          var registrationCode = viewModel.IsFromPowerConnector ? viewModel.CellSelectedAutoControlPanel! : viewModel.CellSelectedSignalDestination! ;
          var deviceSymbol = viewModel.IsFromPowerConnector ? viewModel.CellSelectedAutoControlPanel : viewModel.CellSelectedSignalDestination ;

          if ( elementFromToPower is FamilyInstance familyInstanceFromToPower ) {
            familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
            familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;
            familyInstanceFromToPower.SetProperty(ElectricalRoutingElementParameter.SymbolContent, deviceSymbol ?? string.Empty);
            var elevationParameter = elementFromToPower.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ) ;
            elevationParameter?.Set( 0.0 ) ;
          }

          if ( elementConnectorPower is FamilyInstance familyInstanceConnectorPower ) {
            familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
            familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, defaultConstructionItem ) ;
            familyInstanceConnectorPower.SetProperty(ElectricalRoutingElementParameter.SymbolContent, deviceSymbol ?? string.Empty);
            familyInstanceConnectorPower.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
            
            var deviceSymbolTagType = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.SymbolContentTag ).FirstOrDefault() ?? throw new InvalidOperationException() ;
            IndependentTag.Create( uiDocument.Document, deviceSymbolTagType.Id, uiDocument.Document.ActiveView.Id, new Reference( familyInstanceConnectorPower ), false, TagOrientation.Horizontal, new XYZ(point.X, point.Y + 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * uiDocument.ActiveView.Scale, heightOfConnector) ) ;
          }
          
          return Result.Succeeded ;
        } ) ;

        return result ;
      }
      catch ( Exception ex ) {
        message = ex.Message ;
        return Result.Cancelled ;
      }
    }

    private static Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, bool isFromPowerConnector )
    {
      var symbol = isFromPowerConnector
        ? uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.FromPowerEquipment ).FirstOrDefault()
        : uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ToPowerEquipment ).FirstOrDefault() ;
      var routingSymbol = symbol ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private static Element GeneratePowerConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide )
          .FirstOrDefault() ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }
  }
}