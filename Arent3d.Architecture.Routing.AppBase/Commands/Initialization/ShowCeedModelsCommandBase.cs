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
  public abstract class ShowCeedModelsCommandBase : IExternalCommand
  {
    private const string ConditionTextNoteTypeName = "1.5 mm" ;
    private const string DefaultConstructionItem = "未設定" ;

    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgCeedModel = new CeedModelDialog( commandData.Application ) ;

      dlgCeedModel.ShowDialog() ;
      if ( ! ( dlgCeedModel.DialogResult ?? false ) ) return Result.Cancelled ;
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      if ( string.IsNullOrEmpty( dlgCeedModel.SelectedDeviceSymbol ) ) return Result.Succeeded ;
      var result = doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
      {
        var uiDoc = commandData.Application.ActiveUIDocument ;

        var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
        var level = uiDoc.ActiveView.GenLevel ;
        var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        var element = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level, dlgCeedModel.SelectedFloorPlanType ) ;
        var ceedCode = dlgCeedModel.SelectedCeedCode + "-" + dlgCeedModel.SelectedDeviceSymbol + "-" + dlgCeedModel.SelectedModelNumber ;
        if ( element is FamilyInstance familyInstance ) {
          element.SetProperty( ConnectorFamilyParameter.CeedCode, ceedCode ) ;
          element.SetProperty( RoutingFamilyLinkedParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
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

        var txtPosition = new XYZ( originX - 2, originY + 3, heightOfConnector ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, dlgCeedModel.SelectedDeviceSymbol, opts ) ;

        // create group of selected element and new text note
        groupIds.Add( element.Id ) ;
        groupIds.Add( textNote.Id ) ;
        if ( ! string.IsNullOrEmpty( dlgCeedModel.SelectedCondition ) ) {
          if ( dlgCeedModel.SelectedCondition.Length > 6 ) noteWidth += ( dlgCeedModel.SelectedCondition.Length - 6 ) * 0.007 ;
          var txtConditionPosition = new XYZ( originX - 2, originY + 1.5, heightOfConnector ) ;
          var conditionTextNote = TextNote.Create( doc, doc.ActiveView.Id, txtConditionPosition, noteWidth, dlgCeedModel.SelectedCondition, opts ) ;

          var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( ConditionTextNoteTypeName, tt.Name ) ) ;
          if ( textNoteType == null ) {
            Element ele = conditionTextNote.TextNoteType.Duplicate( ConditionTextNoteTypeName ) ;
            textNoteType = ( ele as TextNoteType )! ;
            TextElementType textType = conditionTextNote.Symbol ;
            const BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE ;
            Parameter textSize = textNoteType.get_Parameter( paraIndex ) ;
            textSize.Set( .005 ) ;
          }

          conditionTextNote.ChangeTypeId( textNoteType.Id ) ;
          groupIds.Add( conditionTextNote.Id ) ;
        }

        return Result.Succeeded ;
      } ) ;

      if ( ! groupIds.Any() ) return result ;
      using Transaction t = new Transaction( doc, "Create connector group." ) ;
      t.Start() ;
      doc.Create.NewGroup( groupIds ) ;
      t.Commit() ;

      return result ;
    }

    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, string floorPlanType )
    {
      if ( string.IsNullOrEmpty( floorPlanType ) ) {
        var routingSymbol = ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
        return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      }

      var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
      var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
      return symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private ConnectorOneSideFamilyType GetConnectorFamilyType( string floorPlanType )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      if ( string.IsNullOrEmpty( floorPlanType ) ) return connectorOneSideFamilyType ;
      foreach ( var item in (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ) {
        if ( floorPlanType == item.GetFieldName() ) connectorOneSideFamilyType = item ;
      }

      return connectorOneSideFamilyType ;
    }
  }
}