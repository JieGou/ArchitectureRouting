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
using NPOI.XSSF.UserModel ;

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
          string pipingType = element.Name ;
          var existSymbolDetail =
            detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( x => element.Id.ToString() == x.ConduitId ) ;
          string pipingNumber = "↑" ;
          var routeName = element.GetRouteName() ;
          if ( ! string.IsNullOrEmpty( routeName ) ) {
            var route = uiDoc.Document.CollectRoutes( AddInType.Electrical )
              .FirstOrDefault( x => x.RouteName == routeName ) ;
            if ( route != null ) {
              var parentRouteName = route.GetParentBranches().ToList().LastOrDefault()?.RouteName ;
              var childBranches = route.GetChildBranches().ToList() ;
              if ( (string.IsNullOrEmpty( parentRouteName ) && childBranches.Any()) || !conduitInformationModels.Any(x=>existSymbolDetail != null && x.DetailSymbol == existSymbolDetail.DetailSymbol ) ) {
                pipingNumber = "1" ;
              }
            }
          }


          if ( existSymbolDetail != null ) {
            var existSymbolRoute = element.GetRouteName() ?? string.Empty ;
            if ( ! string.IsNullOrEmpty( existSymbolRoute ) && ceedStorable != null ) {
              if ( ! processedDetailSymbol.Contains( existSymbolRoute ) ) {
                var conduitInformationModel = new ConduitInformationModel( false, floor, string.Empty,
                  existSymbolDetail.DetailSymbol, string.Empty, string.Empty, string.Empty, "1", string.Empty,
                  string.Empty, string.Empty, pipingType, string.Empty, pipingNumber, string.Empty, string.Empty,
                  constructionItem, constructionItem, "" ) ;
                processedDetailSymbol.Add( existSymbolRoute ) ;
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
                      listMaterialCode.Add( item.MaterialCode1 ) ;
                    }

                    if ( listMaterialCode.Any() ) {
                      var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( x.Buzaicd ) ) ;
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
                               $"\r{line}\r{AddFullString( item.WireType + item.WireSize, maxWireType )}\t-{AddFullString( item.WireStrip ?? string.Empty, maxWireStrip )}\tX1\t{AddFullString( CheckEmptyString( item.PipingType + item.PipingSize, maxPipingType ), maxPipingType )}\t{item.Remark}" ) ;
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