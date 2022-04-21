using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;


namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public abstract class ShowRegistrationOfBoardDataCommandBase : IExternalCommand
  {
    private const string DefaultConstructionItem = "未設定" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgRegistrationOfBoardDataModel = new RegistrationOfBoardDataDialog( commandData.Application ) ;

      dlgRegistrationOfBoardDataModel.ShowDialog() ;
      if ( ! ( dlgRegistrationOfBoardDataModel.DialogResult ?? false ) ) return Result.Cancelled ;
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      if ( string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedSignalDestination ) && string.IsNullOrEmpty( dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel ) ) return Result.Succeeded ;
      Element? elementFromToPower = null ;
      Element? elementConnectorPower = null ;


      var result = doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
      {
        var uiDoc = commandData.Application.ActiveUIDocument ;

        var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
        var level = uiDoc.ActiveView.GenLevel ;
        var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

        elementFromToPower = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level, dlgRegistrationOfBoardDataModel.IsFromPowerConnector ) ;
        elementConnectorPower = GeneratePowerConnector( uiDoc, originX, originY - 0.5, heightOfConnector + 100.0.MillimetersToRevitUnits(), level ) ;

        var registrationCode = dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel + "-" + dlgRegistrationOfBoardDataModel.SelectedSignalDestination ;

        if ( elementFromToPower is FamilyInstance familyInstanceFromToPower ) {
          familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
          familyInstanceFromToPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstanceFromToPower.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
          var elevationParameter = elementFromToPower.LookupParameter( "Elevation from Level" ) ;
          elevationParameter?.Set( 0.0 ) ;
        }

        if ( elementConnectorPower is FamilyInstance familyInstanceConnectorPower ) {
          familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.CeedCode, registrationCode ) ;
          familyInstanceConnectorPower.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstanceConnectorPower.SetConnectorFamilyType( ConnectorFamilyType.Power ) ;
        }

        ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
        var noteWidth = .05 ;
        // make sure note width works for the text type
        var minWidth = TextElement.GetMinimumAllowedWidth( doc, defaultTextTypeId ) ;
        var maxWidth = TextElement.GetMaximumAllowedWidth( doc, defaultTextTypeId ) ;
        if ( noteWidth < minWidth ) {
          noteWidth = minWidth ;
        }
        else if ( noteWidth > maxWidth ) {
          noteWidth = maxWidth ;
        }

        TextNoteOptions opts = new( defaultTextTypeId ) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

        var text = dlgRegistrationOfBoardDataModel.IsFromPowerConnector ? dlgRegistrationOfBoardDataModel.SelectedAutoControlPanel : dlgRegistrationOfBoardDataModel.SelectedSignalDestination ;
        var txtPosition = new XYZ( originX - 2, originY + 4, heightOfConnector ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, text, opts ) ;

        // create group of selected element and new text note
        groupIds.Add( elementFromToPower.Id ) ;
        groupIds.Add( elementConnectorPower.Id ) ;
        groupIds.Add( textNote.Id ) ;

        return Result.Succeeded ;
      } ) ;

      if ( ! groupIds.Any() ) return result ;
      using Transaction t = new( doc, "Create connector group." ) ;
      t.Start() ;
      doc.Create.NewGroup( groupIds ) ;
      t.Commit() ;

      return result ;
    }

    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, bool isFromPowerConnector )
    {
      var symbol = isFromPowerConnector ? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.FromPowerConnector ).FirstOrDefault() ) : ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ToPowerConnector ).FirstOrDefault() ) ;
      var routingSymbol = symbol ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private Element GeneratePowerConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level )
    {
      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType.ConnectorOneSide ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }
  }
}