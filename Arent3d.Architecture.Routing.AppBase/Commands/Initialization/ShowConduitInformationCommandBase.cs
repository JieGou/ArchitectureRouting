using System ;
using System.Collections.ObjectModel ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Forms ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.AppBase.ViewModel ;
using Arent3d.Architecture.Routing.Extensions ;
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
      var hiroiSetCdMasterNormalModelData = doc.GetCsvStorable().HiroiSetCdMasterNormalModelData ;
      ObservableCollection<ConduitInformationModel> conduitInformationModels =
        new ObservableCollection<ConduitInformationModel>() ;
      try {
        var pickedObjects = uiDoc.Selection
          .PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" )
          .Where( p => p is FamilyInstance or Conduit ) ;
        foreach ( var element in pickedObjects ) {
          var conduitModel = conduitsModelData.FirstOrDefault() ;
          var wireType = wiresAndCablesModelData.FirstOrDefault() ;
          var heroiCdSet = hiroiSetCdMasterNormalModelData.FirstOrDefault() ;
          string floor = doc.GetElementById<Level>( element.GetLevelId() )?.Name ?? string.Empty ;

          conduitInformationModels.Add( new ConduitInformationModel( false, floor, wireType?.COrP, wireType?.WireType,
            wireType?.DiameterOrNominal, wireType?.NumberOfHeartsOrLogarithm, wireType?.NumberOfConnections,
            string.Empty, string.Empty, string.Empty, conduitModel?.PipingType, conduitModel?.Size, "",
            heroiCdSet?.ConstructionClassification, wireType?.Classification, "", "", "" ) ) ;
        }
        conduitInformationModels =new ObservableCollection<ConduitInformationModel>( conduitInformationModels.GroupBy( x => x.WireType )
          .Select( g =>
          {
            g.First().Quantity = g.Count() ;
            g.First().Remark = $"x{g.Count()}" ;
            return g.First() ;
          } ) );
      }
      catch ( Exception ex ) {
        string e = ex.Message ;
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
              var (originX, originY, originZ) = uiDoc.Selection.PickPoint( "Connectorの配置場所を選択して下さい。" ) ;
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
