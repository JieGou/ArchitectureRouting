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
      var wiresAndCablesModelData = doc.GetCsvStorable().WiresAndCablesModelData ;
      var conduitsModelData = doc.GetCsvStorable().ConduitsModelData ;
      var hiroiSetMasterNormalModelData = doc.GetCsvStorable().HiroiSetMasterNormalModelData ;
      var hiroiMasterModelData = doc.GetCsvStorable().HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = doc.GetCsvStorable().HiroiSetCdMasterNormalModelData ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      var detailSymbolStorable = doc.GetAllStorables<DetailSymbolStorable>().FirstOrDefault() ?? doc.GetDetailSymbolStorable() ;    
      try {
        var pickedObjects = uiDoc.Selection
          .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
          .Where( p => p is FamilyInstance or Conduit ) ;
        foreach ( var element in pickedObjects ) {
          string floor = doc.GetElementById<Level>( element.GetLevelId() )?.Name ?? string.Empty ;
          string costructionItem = element.LookupParameter( "Construction Item" ).AsValueString() ;
          var existSymbolDetail =
            detailSymbolStorable.DetailSymbolModelData.FirstOrDefault( x => element.Id.ToString() == x.ConduitId ) ;
          if(existSymbolDetail!=null) {
            if ( ceedStorable != null ) {
              var ceedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == existSymbolDetail.Code ) ;
              if ( ceedModel != null ) {
                var hiroiCdModel =
                  hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceedModel.CeeDSetCode ) ;
                var hiroiSetModels =
                  hiroiSetMasterNormalModelData.Where( x => x.ParentPartName == ceedModel.CeeDSetCode ) ;
                foreach ( var item in hiroiSetModels ) {
                  List<string> listMaterialCode = new List<string>() ;
                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
                    listMaterialCode.Add( item.MaterialCode1 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode2 ) ) {
                    listMaterialCode.Add( item.MaterialCode2 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode3 ) ) {
                    listMaterialCode.Add( item.MaterialCode3 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode4 ) ) {
                    listMaterialCode.Add( item.MaterialCode4 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode5 ) ) {
                    listMaterialCode.Add( item.MaterialCode5 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode6 ) ) {
                    listMaterialCode.Add( item.MaterialCode6 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode7 ) ) {
                    listMaterialCode.Add( item.MaterialCode7 ) ;
                  }

                  if ( ! string.IsNullOrWhiteSpace( item.MaterialCode8 ) ) {
                    listMaterialCode.Add( item.MaterialCode8 ) ;
                  }

                  if ( listMaterialCode.Any() ) {
                    var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( x.Buzaicd ) ) ;
                    foreach ( var master in masterModels ) {
                      var conduitModels =
                        conduitsModelData.Where( x => x.PipingType == master.Type && x.Size == master.Size1 ) ;
                      conduitInformationModels.Add( new ConduitInformationModel( false, floor,
                        existSymbolDetail.DetailSymbol, master.Type, master.Hinmei, master.Size1, master.Size2,
                        string.Empty, string.Empty, string.Empty, master.Type, master.Size1,
                        conduitModels.Count().ToString(), hiroiCdModel?.ConstructionClassification,
                        conduitModels.FirstOrDefault().Classification, costructionItem, costructionItem, "" ) ) ;
                    }
                  }
                }
              }
            }
            }
        }
        conduitInformationModels =new ObservableCollection<ConduitInformationModel>( conduitInformationModels.GroupBy( x => x.WireType )
          .Select( g =>
          {
            g.First().Quantity = g.Count() ;
            g.First().Remark = $"x{g.Count()}" ;
            return g.First() ;
          } ) );
      }
      catch{
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
              TextNote.Create( doc, doc.ActiveView.Id, txtPosition, noteWidth, GenerateTextTable( viewModel, level.Name ), opts ) ;
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
      var conduitInformationDictionary = conduitInformationModels.GroupBy( x => x.DetailSymbol )
        .ToDictionary( g => g.Key, g => g.ToList() ) ;
      result += $"{line}\r{level}階平面图" ;
      foreach ( var group in conduitInformationDictionary ) {
        result += $"\r{line}\r{group.Key}" ;
        result = @group.Value.Aggregate( result, ( current, item ) => current + $"\r{line}\r{item.WireType + item.WireSize}\t-{item.WireStrip}\tX{item.Quantity}\t({item.PipingType + item.PipingSize})\t{item.Remark}" ) ;
      }

      result += $"\r{line}" ;
      return result ;
    }
  }
}
