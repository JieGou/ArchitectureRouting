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
      var hiroiSetCdMasterNormalModelData = doc.GetCsvStorable().HiroiSetCdMasterNormalModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      var pickedObjects = uiDoc.Selection
        .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
        .Where( p => p is Conduit ) ;
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      var detailSymbolStorable =
        doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      var processedDetailSymbol = new List<string>() ;
      foreach ( var element in pickedObjects ) {
        string floor = doc.GetElementById<Level>( element.GetLevelId() )?.Name ?? string.Empty ;
        string constructionItem = element.LookupParameter( "Construction Item" ).AsString() ;
        string pipingType = element.Name ;
        var existSymbolDetails = detailSymbolStorable.DetailSymbolModelData
          .Where( x => element.Id.ToString() == x.ConduitId ).ToList() ;
        foreach ( var existSymbolDetail in existSymbolDetails ) {
          string pipingNumber = "1" ;
          var routeName = element.GetRouteName() ;
          if ( ! string.IsNullOrEmpty( routeName ) ) {
            var route = uiDoc.Document.CollectRoutes( AddInType.Electrical )
              .FirstOrDefault( x => x.RouteName == routeName ) ;
            if ( route != null && existSymbolDetail.CountCableSamePosition != 1 ) {
              var parentRouteName = route.GetParentBranches().ToList().LastOrDefault()?.RouteName ;
              var childBranches = route.GetChildBranches().ToList() ;
              if ( ! string.IsNullOrEmpty( parentRouteName ) ) {
                pipingNumber = "↑" ;
              }
            }
          }

          if ( existSymbolDetail != null ) {
            var existSymbolRoute = element.GetRouteName() ?? string.Empty ;
            if ( ! string.IsNullOrEmpty( existSymbolRoute ) && ceedStorable != null ) {
              if ( ! processedDetailSymbol.Contains( existSymbolRoute + "-" +
                                                     existSymbolDetail.CountCableSamePosition ) ) {
                var conduitInformationModel = new ConduitInformationModel( false, floor, string.Empty,
                  existSymbolDetail.DetailSymbol, string.Empty, string.Empty, string.Empty, "1", string.Empty,
                  string.Empty, string.Empty, pipingType, string.Empty, pipingNumber, string.Empty, string.Empty,
                  constructionItem, constructionItem, "" ) ;
                processedDetailSymbol.Add( existSymbolRoute + "-" + existSymbolDetail.CountCableSamePosition ) ;
                var ceedModel =
                  ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == existSymbolDetail.Code ) ;
                if ( ceedModel != null ) {
                  conduitInformationModel.CeeDCode = ceedModel.CeeDSetCode ;

                  var hiroiCdModel =
                    hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeeDSetCode ) ;
                  var hiroiSetModels = hiroiSetMasterNormalModelData
                    .Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ).Skip( 1 ) ;
                  conduitInformationModel.ConstructionClassification = hiroiCdModel?.ConstructionClassification ;
                  foreach ( var item in hiroiSetModels ) {
                    List<string> listMaterialCode = new List<string>() ;
                    if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
                      listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
                    }

                    if ( listMaterialCode.Any() ) {
                      var masterModels = hiroiMasterModelData.Where( x =>
                        listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
                      foreach ( var master in masterModels ) {
                        var conduitModels = conduitsModelData
                          .Where( x => x.PipingType == master.Type && x.Size == master.Size1 ).ToList() ;
                        conduitInformationModel.Classification = conduitModels.FirstOrDefault()?.Classification ?? "" ;
                        if ( existSymbolDetail != null ) {
                          conduitInformationModel.WireType = master.Type ;
                          conduitInformationModel.WireSize = master.Size1 ;
                          conduitInformationModel.WireStrip = master.Size2 ;
                        }
                      }
                    }
                  }
                }

                conduitInformationModels.Add( conduitInformationModel ) ;
              }
            }
          }
        }
      }

      var sortedConduitModels = conduitInformationModels.OrderBy( x => x.DetailSymbol )
        .ThenByDescending( y => y.NumberOfPipes ).ToList() ;
      conduitInformationModels = new ObservableCollection<ConduitInformationModel>( sortedConduitModels ) ;
      var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
      var level = uiDoc.ActiveView.GenLevel ;
      var heightOfConnector = doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

      ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
      var noteWidth = 0.6 ;
      TextNoteOptions opts = new(defaultTextTypeId) ;
      var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
      return doc.Transaction(
        "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault( "Set conduit information" ),
        _ =>
        {
          ConduitInformationViewModel viewModel = new ConduitInformationViewModel( conduitInformationModels ) ;
          TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable( viewModel, level.Name ),
            opts ) ;
          return Result.Succeeded ;
        } ) ;
    }

    private string GenerateTextTable( ConduitInformationViewModel viewModel, string level )
    {
      var line = GenerateString( '━', 45,string.Empty ) ;
      var result = string.Empty ;
      var conduitInformationModels = viewModel.ConduitInformationModels ;
      var maxWireType = conduitInformationModels.Max( x => ( x.WireType + x.WireSize ).Length ) ;
      var maxWireStrip = conduitInformationModels.Max( x => x.WireStrip?.Length ) ?? 0 ;
      var maxPipingType = conduitInformationModels.Max( x => ( x.PipingType + x.PipingSize ).Length ) ;
      var conduitInformationDictionary = conduitInformationModels.GroupBy( x => x.DetailSymbol )
        .ToDictionary( g => g.Key, g => g.ToList() ) ;
      result += $"　機器凡例" ;
      foreach ( var group in conduitInformationDictionary ) {
        result += $"\r{line}\r{GenerateString( '　',40,String.Empty )}{GenerateString( '　',28,"配　　管" )}" ;
        result += $"\r{GenerateString( '　',6,"シンボル" )}{GenerateString( '　',6,"記号" )}{GenerateString( '　',28,"配　様" )}{GenerateString( '━',16,string.Empty )}" ;
        result += $"\r{GenerateString( '　',40,String.Empty )}{GenerateString( '　',16,"（屋内）" )}{GenerateString( '　',10,"（屋外）" )}" ;
        result = @group.Value.Aggregate( result,
          ( current, item ) => current +
                               $"\r{line}\r{GenerateString( '　',8,"∅" )}{GenerateString( '　',10,"JBOX()" )}{GenerateString( '　',14,item.WireType + item.WireSize )} {GenerateString('　',16, "－"+(item.WireStrip ?? string.Empty, maxWireStrip )+"X1")}{GenerateString( '　',16,item.PipingType + item.PipingSize)}" ) ;
      }

      result += $"\r{line}" ;
      return result ;
    }

    private string GenerateString( char chr, int lenght, string middle )
    {
      int partOfLength = ( lenght - middle.Length ) / 2 ;
      string partOfStr = new string( chr, partOfLength ) ;
      return partOfStr + middle + partOfStr ;
    }
  }
}