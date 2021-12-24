using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
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
  public abstract class ShowConduitInformationCommandBase : IExternalCommand
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
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      var detailSymbolStorable =
        doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;
      var processedDetailSymbol = new List<string>() ;
      try {
        var pickedObjects = uiDoc.Selection
          .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
          .Where( p => p is Conduit ) ;
        foreach ( var element in pickedObjects ) {
          string floor = doc.GetElementById<Level>( element.GetLevelId() )?.Name ?? string.Empty ;
          string constructionItem = element.LookupParameter( "Construction Item" ).AsValueString() ;
          var existSymbolDetail =
            detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( x => element.Id.ToString() == x.ConduitId ) ;
          if ( existSymbolDetail != null && ceedStorable != null ) {
            if ( ! processedDetailSymbol.Contains( existSymbolDetail.FromConnectorId +
                                                   existSymbolDetail.ToConnectorId ) ) {
              processedDetailSymbol.Add( existSymbolDetail.FromConnectorId + existSymbolDetail.ToConnectorId ) ;
              var ceedModel =
                ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == existSymbolDetail.Code ) ;
              if ( ceedModel != null ) {
                var hiroiCdModel =
                  hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeeDSetCode ) ;
                var hiroiSetModels = hiroiSetMasterNormalModelData
                  .Where( x => x.ParentPartModelNumber.Contains( ceedModel.CeeDModelNumber ) ).Skip( 1 ) ;
                foreach ( var item in hiroiSetModels ) {
                  List<string> listMaterialCode = new List<string>() ;
                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
                    listMaterialCode.Add( item.MaterialCode1 ) ;
                  }

                  if ( listMaterialCode.Any() ) {
                    var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( x.Buzaicd ) ) ;
                    foreach ( var master in masterModels ) {
                      var conduitModels = conduitsModelData
                        .Where( x => x.PipingType == master.Type && x.Size == master.Size1 ).ToList() ;
                      conduitInformationModels.Add( new ConduitInformationModel( false, floor,
                        existSymbolDetail.DetailSymbol, master.Type, master.Size1, master.Size2, "1", string.Empty,
                        string.Empty, string.Empty, master.Type,
                        master.Size1, "1", hiroiCdModel?.ConstructionClassification,
                        conduitModels.FirstOrDefault()?.Classification ?? "", constructionItem, constructionItem,
                        "" ) ) ;
                    }
                  }
                }
              }
            }
          }
        }

        conduitInformationModels = new ObservableCollection<ConduitInformationModel>( conduitInformationModels
          .GroupBy( x => new { x?.DetailSymbol, x?.WireType } ).Select( g =>
          {
            g.First().Quantity = g.Count() ;
            g.First().WireBook = g.Count().ToString() ;
            g.First().NumberOfPipes = g.Count().ToString() ;
            g.First().Remark = g.Count() > 1 ? $"x{g.Count()}" : string.Empty ;
            return g.First() ;
          } ).OrderBy( y => y.DetailSymbol ) ) ;
      }
      catch {
        return Result.Cancelled ;
      }

      ConduitInformationViewModel viewModel = new ConduitInformationViewModel( conduitInformationModels ) ;
      var dialog = new ConduitInformationDialog( viewModel ) ;
      dialog.ShowDialog() ;

      if ( dialog.DialogResult ?? false ) {
        return doc.Transaction(
          "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault( "Set conduit information" ),
          _ =>
          {
            if ( viewModel.IsCreateSchedule ) {
              var (originX, originY, originZ) = uiDoc.Selection.PickPoint() ;
              var level = uiDoc.ActiveView.GenLevel ;
              var heightOfConnector =
                doc.GetHeightSettingStorable()[ level ].HeightOfConnectors.MillimetersToRevitUnits() ;

              ElementId defaultTextTypeId = doc.GetDefaultElementTypeId( ElementTypeGroup.TextNoteType ) ;
              var noteWidth = 0.4 ;
              TextNoteOptions opts = new(defaultTextTypeId) ;
              var txtPosition = new XYZ( originX, originY, heightOfConnector ) ;
              TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth,
                GenerateTextTable( viewModel, level.Name ), opts ) ;
              viewModel.IsCreateSchedule = false ;
            }

            return Result.Succeeded ;
          } ) ;
      }
      else {
        return Result.Cancelled ;
      }
    }

    private string GenerateTextTable( ConduitInformationViewModel viewModel, string level )
    {
      string line = new string( '＿', 32 ) ;
      string result = string.Empty ;
      var conduitInformationModels = viewModel.ConduitInformationModels ;
      var maxWireType = conduitInformationModels.Max( x => ( x.WireType + x.WireSize ).Length ) ;
      var maxWireStrip = conduitInformationModels.Max( x => x.WireStrip?.Length ) ?? 0 ;
      var maxPipingType = conduitInformationModels.Max( x => ( x.PipingType + x.PipingSize ).Length ) ;
      var conduitInformationDictionary = conduitInformationModels.GroupBy( x => x.DetailSymbol )
        .ToDictionary( g => g.Key, g => g.ToList() ) ;
      result += $"{line}\r{level}階平面图" ;
      foreach ( var group in conduitInformationDictionary ) {
        result += $"\r{line}\r{group.Key}" ;
        result = @group.Value.Aggregate( result,
          ( current, item ) => current +
                               $"\r{line}\r{AddFullString( item.WireType + item.WireSize, maxWireType )}\t-{AddFullString( item.WireStrip ?? string.Empty, maxWireStrip )}\tX{item.Quantity}\t{AddFullString( CheckEmptyString( item.PipingType + item.PipingSize, maxPipingType ), maxPipingType )}\t{item.Remark}" ) ;
      }

      result += $"\r{line}" ;
      return result ;
    }

    private string CheckEmptyString( string str, int lenght )
    {
      return ! string.IsNullOrEmpty( str ) ? $"({str})" : new string( '　', lenght ) ;
    }

    private string AddFullString( string str, int length )
    {
      if ( str.Length < length ) {
        str += new string( '　', length - str.Length ) ;
      }

      return str ;
    }
  }
}
