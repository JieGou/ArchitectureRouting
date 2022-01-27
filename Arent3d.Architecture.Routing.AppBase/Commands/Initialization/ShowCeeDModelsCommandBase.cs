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
  public abstract class ShowCeeDModelsCommandBase : IExternalCommand
  {
    private const string ConditionTextNoteTypeName = "1.5 mm" ;

    protected abstract RoutingFamilyType RoutingFamilyType { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      var dlgCeeDModel = new CeeDModelDialog( doc ) ;

      dlgCeeDModel.ShowDialog() ;
      if ( ! ( dlgCeeDModel.DialogResult ?? false ) ) return Result.Cancelled ;
      if ( ! string.IsNullOrEmpty( dlgCeeDModel.SelectedDeviceSymbol ) ) {
        return doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
        {
          var uiDoc = commandData.Application.ActiveUIDocument ;

          var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
          var level = uiDoc.ActiveView.GenLevel ;
          var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
          var connectorOneSideFamilyType = GetConnectorFamilyType( doc, dlgCeeDModel.SelectedFamilyType ) ;
          var element = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level, connectorOneSideFamilyType ) ;
          var ceeDCode = dlgCeeDModel.SelectedCeeDCode + "-" + dlgCeeDModel.SelectedDeviceSymbol + "-" + dlgCeeDModel.SelectedModelNumber ;
          element.SetProperty( ConnectorFamilyParameter.CeeDCode, ceeDCode ) ;
          if ( element is FamilyInstance familyInstance ) familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;

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
          var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, dlgCeeDModel.SelectedDeviceSymbol, opts ) ;

          // create group of selected element and new text note
          ICollection<ElementId> groupIds = new List<ElementId>() ;
          groupIds.Add( element.Id ) ;
          groupIds.Add( textNote.Id ) ;
          if ( ! string.IsNullOrEmpty( dlgCeeDModel.SelectedCondition ) ) {
            if ( dlgCeeDModel.SelectedCondition.Length > 6 ) noteWidth += ( dlgCeeDModel.SelectedCondition.Length - 6 ) * 0.007 ;
            var txtConditionPosition = new XYZ( originX - 2, originY + 1.5, heightOfConnector ) ;
            var conditionTextNote = TextNote.Create( doc, doc.ActiveView.Id, txtConditionPosition, noteWidth, dlgCeeDModel.SelectedCondition, opts ) ;

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

          doc.Create.NewGroup( groupIds ) ;

          return Result.Succeeded ;
        } ) ;
      }

      return Result.Succeeded ;
    }

    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, ConnectorOneSideFamilyType connectorOneSideFamilyType )
    {
      var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( RoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
      return symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
    }

    private ConnectorOneSideFamilyType GetConnectorFamilyType( Document doc, string familyTypeName )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      var connectorFamilyTypeStorable = doc.GetConnectorFamilyTypeStorable() ;
      var connectorFamilyTypeName = connectorFamilyTypeStorable.ConnectorFamilyTypeModelData.FirstOrDefault( c => c.FamilyTypeName == familyTypeName )!.ConnectorFamilyTypeName ;
      if ( string.IsNullOrEmpty( connectorFamilyTypeName ) ) return connectorOneSideFamilyType ;
      if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide1.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide2.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide2 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide3.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide3 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide4.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide4 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide5.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide5 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide6.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide6 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide7.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide7 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide8.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide8 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide9.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide9 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide10.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide10 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide11.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide11 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide12.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide12 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide13.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide13 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide14.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide14 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide15.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide15 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide16.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide16 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide17.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide17 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide18.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide18 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide19.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide19 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide20.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide20 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide21.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide21 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide22.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide22 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide23.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide23 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide24.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide24 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide25.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide25 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide26.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide26 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide27.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide27 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide28.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide28 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide29.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide29 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide30.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide30 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide31.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide31 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide32.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide32 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide33.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide33 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide34.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide34 ;
      else if ( connectorFamilyTypeName == ConnectorOneSideFamilyType.ConnectorOneSide35.GetFieldName() )
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide35 ;
      else
        connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide36 ;

      return connectorOneSideFamilyType ;
    }
  }
}