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
    private const string DeviceSymbolTextNoteTypeName = "Left_2.5mm_DeviceSymbolText" ;
    private const string ConditionTextNoteTypeName = "1.5mm_ConditionText" ;
    private const string DefaultConstructionItem = "未設定" ;

    protected abstract ElectricalRoutingFamilyType ElectricalRoutingFamilyType { get ; }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      const string switch2DSymbol = "2Dシンボル切り替え" ;
      const string symbolMagnification = "シンボル倍率" ;
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var data = doc.GetSetupPrintStorable() ;
      var defaultSymbolMagnification = data.Scale * data.Ratio;
      
      var dlgCeedModel = new CeedModelDialog( commandData.Application ) ;

      dlgCeedModel.ShowDialog() ;
      if ( ! ( dlgCeedModel.DialogResult ?? false ) ) return Result.Cancelled ;
      ICollection<ElementId> groupIds = new List<ElementId>() ;
      if ( string.IsNullOrEmpty( dlgCeedModel.SelectedDeviceSymbol ) ) return Result.Succeeded ;
      Element? element = null ;
      var result = doc.Transaction( "TransactionName.Commands.Routing.PlacementDeviceSymbol".GetAppStringByKeyOrDefault( "Placement Device Symbol" ), _ =>
      {
        var uiDoc = commandData.Application.ActiveUIDocument ;

        var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
        var level = uiDoc.ActiveView.GenLevel ;
        var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;
        element = GenerateConnector( uiDoc, originX, originY, heightOfConnector, level, dlgCeedModel.SelectedFloorPlanType ) ;
        var ceedCode = string.Join( ":", dlgCeedModel.SelectedCeedCode, dlgCeedModel.SelectedDeviceSymbol, dlgCeedModel.SelectedModelNumber ) ;
        if ( element is FamilyInstance familyInstance ) {
          element.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
          element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
        }

        var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
        TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

        var txtPosition = new XYZ( originX - 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification, originY + ( 1.5 + 4 * TextNoteHelper.TextSize ).MillimetersToRevitUnits() * defaultSymbolMagnification, heightOfConnector ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, dlgCeedModel.SelectedDeviceSymbol, opts ) ;

        var deviceSymbolTextNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( DeviceSymbolTextNoteTypeName, tt.Name ) ) ;
        if ( deviceSymbolTextNoteType == null ) {
          var elementType = textNote.TextNoteType.Duplicate( DeviceSymbolTextNoteTypeName ) ;
          deviceSymbolTextNoteType = elementType as TextNoteType ;
          deviceSymbolTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
          deviceSymbolTextNoteType?.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 0 ) ;
        }

        if ( deviceSymbolTextNoteType != null ) textNote.ChangeTypeId( deviceSymbolTextNoteType.Id ) ;

        // create group of selected element and new text note
        groupIds.Add( element.Id ) ;
        groupIds.Add( textNote.Id ) ;
        if ( ! string.IsNullOrEmpty( dlgCeedModel.SelectedCondition ) ) {
          var txtConditionPosition = new XYZ( originX - 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification, originY + ( 1.5 + 2 * TextNoteHelper.TextSize ).MillimetersToRevitUnits() * defaultSymbolMagnification, heightOfConnector ) ;
          var conditionTextNote = TextNote.Create( doc, doc.ActiveView.Id, txtConditionPosition, dlgCeedModel.SelectedCondition, opts ) ;

          var textNoteType = new FilteredElementCollector( doc ).OfClass( typeof( TextNoteType ) ).WhereElementIsElementType().Cast<TextNoteType>().FirstOrDefault( tt => Equals( ConditionTextNoteTypeName, tt.Name ) ) ;
          if ( textNoteType == null ) {
            Element ele = conditionTextNote.TextNoteType.Duplicate( ConditionTextNoteTypeName ) ;
            textNoteType = ( ele as TextNoteType )! ;
            TextElementType textType = conditionTextNote.Symbol ;
            const BuiltInParameter paraIndex = BuiltInParameter.TEXT_SIZE ;
            Parameter textSize = textNoteType.get_Parameter( paraIndex ) ;
            textSize.Set( .005 ) ;
            textNoteType.get_Parameter( BuiltInParameter.TEXT_BOX_VISIBILITY ).Set( 0 ) ;
            textNoteType.get_Parameter( BuiltInParameter.TEXT_BACKGROUND ).Set( 0 ) ;
          }

          conditionTextNote.ChangeTypeId( textNoteType.Id ) ;
          groupIds.Add( conditionTextNote.Id ) ;
        }

        return Result.Succeeded ;
      } ) ;

      if ( ! groupIds.Any() ) return result ;
      using Transaction t = new ( doc, "Create connector group." ) ;
      t.Start() ;
      if ( element != null ) {
        var isHasParameterSwitch2DSymbol = element.HasParameter( switch2DSymbol ) ;
        if ( isHasParameterSwitch2DSymbol ) element.SetProperty( switch2DSymbol, true ) ;
        var isHasParameterSymbolMagnification = element.HasParameter( symbolMagnification ) ;
        if ( isHasParameterSymbolMagnification ) element.SetProperty( symbolMagnification, defaultSymbolMagnification ) ;
      }
      doc.Create.NewGroup( groupIds ) ;
      t.Commit() ;

      return result ;
    }

    private Element GenerateConnector( UIDocument uiDocument, double originX, double originY, double originZ, Level level, string floorPlanType )
    {
      FamilyInstance instance;
      if ( ! string.IsNullOrEmpty( floorPlanType ) ) {
        var connectorOneSideFamilyTypeNames = ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ).Select( f => f.GetFieldName() ).ToHashSet() ;
        if ( connectorOneSideFamilyTypeNames.Contains( floorPlanType ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
          var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
          instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
          SetIsEcoMode( uiDocument, instance );
          return instance ;
        }
        else {
          if ( new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == floorPlanType ) is Family family ) {
            foreach ( ElementId familySymbolId in (IEnumerable<ElementId>) family.GetFamilySymbolIds() ) {
              var symbol = uiDocument.Document.GetElementById<FamilySymbol>( familySymbolId ) ?? throw new InvalidOperationException() ;
              instance = symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
              SetIsEcoMode( uiDocument, instance );
              return instance ;
            }
          }
        }
      }

      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      instance = routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
      SetIsEcoMode( uiDocument, instance );
      return instance ;
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

    private void SetIsEcoMode(UIDocument uiDocument, FamilyInstance instance)
    { 
      if ( false == instance.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out string? _ ) ) return ;
      instance.SetProperty( ElectricalRoutingElementParameter.IsEcoMode, uiDocument.Document.GetEcoSettingStorable().EcoSettingData.IsEcoMode.ToString() ) ;
    }
  }
}