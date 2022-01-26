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
using MathLib ;

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
          var connectorOneSideFamilyType = GetRoutingFamilyType( dlgCeeDModel.SelectedFamilyType ) ;
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

          TextNoteOptions opts = new(defaultTextTypeId) ;
          opts.HorizontalAlignment = HorizontalTextAlignment.Left ;

          var txtPosition = new XYZ( originX - 2, originY + 3, heightOfConnector ) ;
          var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, dlgCeeDModel.SelectedDeviceSymbol, opts ) ;

          // create group of selected element and new text note
          ICollection<ElementId> groupIds = new List<ElementId>() ;
          groupIds.Add( element.Id ) ;
          groupIds.Add( textNote.Id ) ;
          if ( ! string.IsNullOrEmpty( dlgCeeDModel.SelectedCondition ) ) {
            if ( dlgCeeDModel.SelectedCondition.Length > 6 ) noteWidth += (dlgCeeDModel.SelectedCondition.Length - 6) * 0.007 ;
            var txtConditionPosition = new XYZ( originX - 2, originY + 1.5, heightOfConnector ) ;
            var conditionTextNote = TextNote.Create( doc, doc.ActiveView.Id, txtConditionPosition, noteWidth, dlgCeeDModel.SelectedCondition, opts ) ;
            
            var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) )
              .WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( ConditionTextNoteTypeName, tt.Name ) ) ;
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
    
    private ConnectorOneSideFamilyType GetRoutingFamilyType( string familyTypeName )
    {
      var connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
      switch ( familyTypeName ) {
        case "FamilyType1" :
        case "FamilyType2" :
        case "FamilyType3" :
        case "FamilyType4" :
        case "FamilyType37" :
        case "FamilyType44" :
        case "FamilyType45" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide1 ;
          break;
        case "FamilyType5" :
        case "FamilyType21" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide2 ;
          break;
        case "FamilyType" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide3 ;
          break;
        case "FamilyType_" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide4 ;
          break;
        case "FamilyType6" :
        case "FamilyType7" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide5 ;
          break;
        case "FamilyType8" :
        case "FamilyType9" :
        case "FamilyType10" :
        case "FamilyType15" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide6 ;
          break;
        case "FamilyType11" :
        case "FamilyType12" :
        case "FamilyType13" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide7 ;
          break;
        case "FamilyType14" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide8 ;
          break;
        case "FamilyType16" :
        case "FamilyType18" :
        case "FamilyType25" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide9 ;
          break;
        case "FamilyType19" :
        case "FamilyType20" :
        case "FamilyType26" :
        case "FamilyType32" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide10 ;
          break;
        case "TE2M":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide11 ;
          break;
        case "TE2F":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide12 ;
          break;
        case "TEP":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide13 ;
          break;
        case "TEE":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide14 ;
          break;
        case "TEM":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide15 ;
          break;
        case "TEF":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide16 ;
          break;
        case "TE1P":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide17 ;
          break;
        case "TE1E":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide18 ;
          break;
        case "FamilyType28" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide19 ;
          break;
        case "FamilyType29" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide20 ;
          break;
        case "FamilyType30" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide21 ;
          break;
        case "LTEE":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide22 ;
          break;
        case "FamilyType31" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide23 ;
          break;
        case "FamilyType33" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide24 ;
          break;
        case "FamilyType34" :
        case "FamilyType36" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide25 ;
          break;
        case "FamilyType35" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide26 ;
          break;
        case "FamilyType38" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide27 ;
          break;
        case "FamilyType40" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide28 ;
          break;
        case "FamilyType39" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide29 ;
          break;
        case "FamilyType41" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide30 ;
          break;
        case "HEM":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide31 ;
          break;
        case "HEF":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide32 ;
          break;
        case "FamilyType42" :
        case "FamilyType43" :
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide33 ;
          break;
        case "THE3E":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide34 ;
          break;
        case "THE3M":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide35 ;
          break;
        case "THE3F":
          connectorOneSideFamilyType = ConnectorOneSideFamilyType.ConnectorOneSide36 ;
          break;
      }

      return connectorOneSideFamilyType ;
    }
  }
}