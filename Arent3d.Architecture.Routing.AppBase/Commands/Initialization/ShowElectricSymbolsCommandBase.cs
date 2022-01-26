using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowElectricSymbolsCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var conduitsModelData = doc.GetCsvStorable().ConduitsModelData ;
      var hiroiSetMasterNormalModelData = doc.GetCsvStorable().HiroiSetMasterNormalModelData ;
      var hiroiMasterModelData = doc.GetCsvStorable().HiroiMasterModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var pickedObjects = uiDoc.Selection
        .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
        .Where( p => p is Conduit ) ;
      var electricalSymbolModels = new ObservableCollection<ElectricalSymbolModel>() ;
      var detailSymbolStorable =
        doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      var processedDetailSymbol = new List<string>() ;
      foreach ( var element in pickedObjects ) {
        string pipingType = element.Name ;
        var existSymbolDetails = detailSymbolStorable.DetailSymbolModelData
          .Where( x => element.Id.ToString() == x.ConduitId ).ToList() ;
        foreach ( var existSymbolDetail in existSymbolDetails ) {
          if ( existSymbolDetail != null ) {
            var existSymbolRoute = element.GetRouteName() ?? string.Empty ;
            if ( ! string.IsNullOrEmpty( existSymbolRoute ) && ceedStorable != null ) {
              if ( ! processedDetailSymbol.Contains( existSymbolRoute + "-" +
                                                     existSymbolDetail.CountCableSamePosition ) ) {
                string startTeminateId = string.Empty ;
                string endTeminateId = string.Empty ;
                var startPoint = element.GetNearestEndPoints( true ) ;
                var startPointKey = startPoint.FirstOrDefault()?.Key ;
                if ( startPointKey != null ) {
                  startTeminateId = startPointKey.GetElementId() ;
                }

                var endPoint = element.GetNearestEndPoints( false ) ;
                var endPointKey = endPoint.FirstOrDefault()?.Key ;
                if ( endPointKey != null ) {
                  endTeminateId = endPointKey!.GetElementId() ;
                }

                var (startCeeDSymbol, endCeeDSymbol) =
                  GetFromConnectorAndToConnectorCeeDCode( doc, startTeminateId, endTeminateId ) ;
                var startCeeDModel =
                  ceedStorable.CeedModelData.FirstOrDefault( x =>
                    x.Condition.Equals( startCeeDSymbol.Trim( '\r' ) ) ) ;
                var endCeeDModel =
                  ceedStorable.CeedModelData.FirstOrDefault( x => x.Condition.Equals( endCeeDSymbol.Trim( '\r' ) ) ) ;
                var startElectricalSymbolModel = new ElectricalSymbolModel(
                  startCeeDModel?.FloorPlanSymbol ?? string.Empty,
                  startCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, string.Empty, string.Empty, string.Empty,
                  pipingType, string.Empty ) ;
                var endElectricalSymbolModel = new ElectricalSymbolModel( endCeeDModel?.FloorPlanSymbol ?? string.Empty,
                  endCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, string.Empty, string.Empty, string.Empty,
                  pipingType, string.Empty ) ;
                processedDetailSymbol.Add( existSymbolRoute + "-" + existSymbolDetail.CountCableSamePosition ) ;
                var ceedModel =
                  ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == existSymbolDetail.Code ) ;
                if ( ceedModel != null ) {
                  var hiroiSetModels = hiroiSetMasterNormalModelData
                    .Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ).Skip( 1 ) ;
                  foreach ( var item in hiroiSetModels ) {
                    List<string> listMaterialCode = new List<string>() ;
                    if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
                      listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
                    }

                    if ( listMaterialCode.Any() ) {
                      var masterModels = hiroiMasterModelData.Where( x =>
                        listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
                      foreach ( var master in masterModels ) {
                        if ( existSymbolDetail != null ) {
                          startElectricalSymbolModel.WireType = master.Type ;
                          startElectricalSymbolModel.WireSize = master.Size1 ;
                          startElectricalSymbolModel.WireStrip = master.Size2 ;
                          endElectricalSymbolModel.WireType = master.Type ;
                          endElectricalSymbolModel.WireSize = master.Size1 ;
                          endElectricalSymbolModel.WireStrip = master.Size2 ;
                        }
                      }
                    }
                  }
                }

                electricalSymbolModels.Add( startElectricalSymbolModel ) ;
                electricalSymbolModels.Add( endElectricalSymbolModel ) ;
              }
            }
          }
        }
      }

      var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
      var level = uiDoc.ActiveView.GenLevel ;
      var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

      ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
      var noteWidth = 0.54 ;
      TextNoteOptions opts = new(defaultTextTypeId) ;
      var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
      return doc.Transaction(
        "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault(
          "Create electrical symbol schedules" ), _ =>
        {
          TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable( electricalSymbolModels ),
            opts ) ;
          return Result.Succeeded ;
        } ) ;
    }

    private string GenerateTextTable( ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      var line = GenerateString( '━', 45, string.Empty ) ;
      var result = string.Empty ;
      result += "　機器凡例" ;
      result += $"\r{line}\r{GenerateString( '　', 40, String.Empty )}{GenerateString( '　', 28, "配　　管" )}" ;
      result +=
        $"\r{GenerateString( '　', 6, "シンボル" )}{GenerateString( '　', 6, "記号" )}{GenerateString( '　', 28, "配　線" )}{GenerateString( '━', 16, string.Empty )}" ;
      result +=
        $"\r{GenerateString( '　', 40, String.Empty )}{GenerateString( '　', 16, "（屋内）" )}{GenerateString( '　', 10, "（屋外）" )}" ;
      foreach ( var item in electricalSymbolModels ) {
        result +=
          $"\r{line}\r{GenerateString( '　', 6, item.FloorPlanSymbol )}{GenerateString( '　', 8, item.GeneralDisplayDeviceSymbol )}{GenerateString( '　', 14, item.WireType + item.WireSize )} {GenerateString( '　', 14, "－" + item.WireStrip + " x 1" )}{GenerateString( '　', 16, item.PipingType + item.PipingSize )}" ;
      }

      result += $"\r{line}" ;
      return result ;
    }

    private string GenerateString( char chr, int lenght, string middle )
    {
      if ( middle.Length % 2 != 0 ) {
        middle += "　" ;
      }

      int lesslength = 0 ;
      if ( middle.Contains( '.' ) ) {
        lesslength = 2 ;
        
      }
      var partOfLength = ( lenght - middle.Length + lesslength) / 2 ;
      if ( partOfLength < 0 ) {
        partOfLength = 0 ;
      }
      var partOfStr = new string( chr, partOfLength ) ;
      return partOfStr + middle + partOfStr ;
    }

    private static (string, string) GetFromConnectorAndToConnectorCeeDCode( Document document, string fromElementId,
      string toElementId )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;

      if ( ! string.IsNullOrEmpty( fromElementId ) ) {
        var fromConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == fromElementId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorId, out string? fromConnectorId ) ;
          if ( ! string.IsNullOrEmpty( fromConnectorId ) )
            fromElementId = fromConnectorId! ;
        }
      }

      if ( string.IsNullOrEmpty( toElementId ) ) return ( fromElementId, toElementId ) ;
      {
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if (  toConnector!.IsTerminatePoint() ||  toConnector!.IsPassPoint() ) {
          toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? toConnectorId ) ;
          if ( ! string.IsNullOrEmpty( toConnectorId ) )
            toElementId = toConnectorId! ;
        }
      }
      string fromText = GetTextFromGroup( document, fromElementId ) ;
      string toText = GetTextFromGroup( document, toElementId ) ;
      return ( fromText, toText ) ;
    }

    private static string GetTextFromGroup( Document document, string elementId )
    {
      string result = string.Empty ;
      var parentGroup = document.GetAllElements<Group>()
        .FirstOrDefault( x => x.GetMemberIds().Any( y => y.ToString().Equals( elementId ) ) ) ;
      if ( parentGroup != null ) {
        // ungroup before set property
        var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
        var enumerable = attachedGroup as Group[] ?? attachedGroup.ToArray() ;
        if ( enumerable.Any() ) {
          var textNoteId = enumerable.FirstOrDefault()?.GetMemberIds().Skip( 1 ).FirstOrDefault() ;
          var textNote = document.GetAllElements<TextNote>().FirstOrDefault( x => x.Id == textNoteId ) ;
          if ( textNote != null ) {
            result = textNote.Text ;
          }
        }
      }

      return result ;
    }
  }
}
