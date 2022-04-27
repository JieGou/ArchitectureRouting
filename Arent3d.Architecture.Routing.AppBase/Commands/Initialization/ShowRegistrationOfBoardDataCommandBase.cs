using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
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
    private const string DefaultConstructionItem = "未設定" ;
    private const string StatusPrompt = "配置場所を選択して下さい。" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgRegistrationOfBoardDataModel = new RegistrationOfBoardDataDialog( commandData.Application ) ;

      dlgRegistrationOfBoardDataModel.ShowDialog() ;
      if ( ! ( dlgRegistrationOfBoardDataModel.DialogResult ?? false ) ) return Result.Cancelled ;
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      if ( string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedSignalDestination ) && string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel ) ) return Result.Succeeded ;


      var result = doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
      {
        var uiDoc = commandData.Application.ActiveUIDocument ;

        var (originX, originY, _) = uiDoc.Selection.PickPoint( "配置場所を選択して下さい。" ) ;
        var level = uiDoc.ActiveView.GenLevel ;
        var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

        var elementFromToPower = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level, dlgRegistrationOfBoardDataModel.IsFromPowerConnector ) ;
        var elementConnectorPower = GeneratePowerConnector( uiDoc, originX, originY - 0.5, heightOfConnector + 100.0.MillimetersToRevitUnits(), level ) ;

        var registrationCode = viewModel.IsFromPowerConnector
          ? viewModel.CellSelectedAutoControlPanel!
          : viewModel.CellSelectedSignalDestination! ;

        if ( elementFromToPower is FamilyInstance familyInstanceFromToPower ) {
          familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
          familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstanceFromToPower.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
          var elevationParameter = elementFromToPower.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ) ;
          elevationParameter?.Set( 0.0 ) ;
        }

        if ( elementConnectorPower is FamilyInstance familyInstanceConnectorPower ) {
          familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
          familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstanceConnectorPower.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
        }

        var defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
        var noteWidth = .12 ;
        // make sure note width works for the text type
        var minWidth = TextElement.GetMinimumAllowedWidth( doc, defaultTextTypeId ) ;
        var maxWidth = TextElement.GetMaximumAllowedWidth( doc, defaultTextTypeId ) ;
        if ( noteWidth < minWidth ) {
          noteWidth = minWidth ;
        }
        else if ( noteWidth > maxWidth ) {
          noteWidth = maxWidth ;
        }

        TextNoteOptions opts = new(defaultTextTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

        var text = viewModel.IsFromPowerConnector 
          ? viewModel.CellSelectedAutoControlPanel 
          : viewModel.CellSelectedSignalDestination ;
        var txtPosition = new XYZ( originX - 2, originY + 2.5, heightOfConnector ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, text, opts ) ;

        // create group of selected element and new text note
        groupIds.Add( elementFromToPower.Id ) ;
        groupIds.Add( elementConnectorPower.Id ) ;
        groupIds.Add( textNote.Id ) ;

        return Result.Succeeded ;
      } ) ;

      if ( ! groupIds.Any() ) return result ;
      using Transaction t = new(doc, "Create connector group.") ;
      t.Start() ;
      doc.Create.NewGroup( groupIds ) ;
      t.Commit() ;

      return result ;
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