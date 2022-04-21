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
      const string switch2DSymbol = "2Dシンボル切り替え" ;
      const string symbolMagnification = "シンボル倍率" ;
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var defaultSymbolMagnification = doc.GetSetupPrintStorable().Scale ;
      
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
        var ceedCode = dlgCeedModel.SelectedCeedCode + "-" + dlgCeedModel.SelectedDeviceSymbol + "-" + dlgCeedModel.SelectedModelNumber ;
        if ( element is FamilyInstance familyInstance ) {
          element.SetProperty( ElectricalRoutingElementParameter.CeedCode, ceedCode ) ;
          element.SetProperty( ElectricalRoutingElementParameter.ConstructionItem, DefaultConstructionItem ) ;
          familyInstance.SetConnectorFamilyType( ConnectorFamilyType.Sensor ) ;
        }

        var textTypeId = TextNoteHelper.FindOrCreateTextNoteType( doc )!.Id ;
        TextNoteOptions opts = new(textTypeId) { HorizontalAlignment = HorizontalTextAlignment.Left } ;

        var txtPosition = new XYZ( originX - 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification, originY + ( 1.5 + 4 * TextNoteHelper.TextSize ).MillimetersToRevitUnits() * defaultSymbolMagnification, heightOfConnector ) ;
        var textNote = TextNote.Create( doc, doc.ActiveView.Id, txtPosition, dlgCeedModel.SelectedDeviceSymbol, opts ) ;

        // create group of selected element and new text note
        groupIds.Add( element.Id ) ;
        groupIds.Add( textNote.Id ) ;
        if ( ! string.IsNullOrEmpty( dlgCeedModel.SelectedCondition ) ) {
          var txtConditionPosition = new XYZ( originX - 2 * TextNoteHelper.TextSize.MillimetersToRevitUnits() * defaultSymbolMagnification, originY + ( 1.5 + 2 * TextNoteHelper.TextSize ).MillimetersToRevitUnits() * defaultSymbolMagnification, heightOfConnector ) ;
          var conditionTextNote = TextNote.Create( doc, doc.ActiveView.Id, txtConditionPosition, dlgCeedModel.SelectedCondition, opts ) ;

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
      if ( ! string.IsNullOrEmpty( floorPlanType ) ) {
        var connectorOneSideFamilyTypeNames = ( (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ).Select( f => f.GetFieldName() ).ToHashSet() ;
        if ( connectorOneSideFamilyTypeNames.Contains( floorPlanType ) ) {
          var connectorOneSideFamilyType = GetConnectorFamilyType( floorPlanType ) ;
          var symbol = uiDocument.Document.GetFamilySymbols( connectorOneSideFamilyType ).FirstOrDefault() ?? ( uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ) ;
          return symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
        }
        else {
          if ( new FilteredElementCollector( uiDocument.Document ).OfClass( typeof( Family ) ).FirstOrDefault( f => f.Name == floorPlanType ) is Family family ) {
            foreach ( ElementId familySymbolId in (IEnumerable<ElementId>) family.GetFamilySymbolIds() ) {
              var symbol = uiDocument.Document.GetElementById<FamilySymbol>( familySymbolId ) ?? throw new InvalidOperationException() ;
              return symbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
            }
          }
        }
      }

      var routingSymbol = uiDocument.Document.GetFamilySymbols( ElectricalRoutingFamilyType ).FirstOrDefault() ?? throw new InvalidOperationException() ;
      return routingSymbol.Instantiate( new XYZ( originX, originY, originZ ), level, StructuralType.NonStructural ) ;
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