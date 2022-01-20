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
      var csvStorable = doc.GetCsvStorable() ;
      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      var conduitsModelData = csvStorable.ConduitsModelData ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var hiroiSetCdMasterNormalModelData = csvStorable.HiroiSetCdMasterNormalModelData ;
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
          string constructionItem = element.LookupParameter( "Construction Item" ).AsString() ;
          string pipingType = "E" ;
          var existSymbolDetails = detailSymbolStorable.DetailSymbolModelData.Where( x => element.Id.ToString() == x.ConduitId ).ToList() ;
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
                if ( ! processedDetailSymbol.Contains( existSymbolRoute + "-" + existSymbolDetail.CountCableSamePosition ) ) {
                  string pipingCrossSectionalArea = "↑" ;
                  var conduitInformationModel = new ConduitInformationModel( false, floor, string.Empty,
                    existSymbolDetail.DetailSymbol, string.Empty, string.Empty, string.Empty, "1", string.Empty,
                    string.Empty, string.Empty, pipingType, string.Empty, pipingNumber, string.Empty, string.Empty,
                    constructionItem, constructionItem, "", pipingCrossSectionalArea, existSymbolDetail.CountCableSamePosition, true ) ;
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
                        listMaterialCode.Add( item.MaterialCode1 ) ;
                      }

                      if ( listMaterialCode.Any() ) {
                        var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
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

                  if ( pipingNumber == "1" ) {
                    pipingCrossSectionalArea = GetPipingCrossSectionalArea( ceedStorable!, hiroiSetCdMasterNormalModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, wiresAndCablesModelData, detailSymbolStorable.DetailSymbolModelData, existSymbolDetail! ) ;
                    Dictionary<string, int> pipingData = GetPipingData( conduitsModelData, pipingType, double.Parse( pipingCrossSectionalArea ) ) ;
                    foreach ( var pipingModel in pipingData ) {
                      var parentConduitInformationModel = new ConduitInformationModel( false, floor, conduitInformationModel.CeeDCode, existSymbolDetail!.DetailSymbol, conduitInformationModel.WireType, conduitInformationModel.WireSize, conduitInformationModel.WireStrip, "1", string.Empty, string.Empty, string.Empty, pipingType, pipingModel.Key.Replace( "mm", string.Empty ), pipingModel.Value.ToString(), conduitInformationModel.ConstructionClassification, conduitInformationModel.Classification, constructionItem, constructionItem, "", pipingCrossSectionalArea, conduitInformationModel.CountCableSamePosition, false ) ;
                      conduitInformationModels.Add( parentConduitInformationModel ) ;
                    }
                  }
                  else {
                    conduitInformationModel.PipingType = pipingCrossSectionalArea ;
                    conduitInformationModel.PipingSize = pipingCrossSectionalArea ;
                    conduitInformationModels.Add( conduitInformationModel ) ;
                  }
                }
              }
            }
          }
        }

        var sortedConduitModels = conduitInformationModels.OrderBy( x => x.DetailSymbol ).ThenByDescending( y=>y.CountCableSamePosition ).ThenByDescending( y=>y.PipingSize ).ToList();
        conduitInformationModels = new ObservableCollection<ConduitInformationModel>( sortedConduitModels ) ;
      }
      catch {
        return Result.Cancelled ;
      }

      var conduitTypeNames = conduitsModelData.Select( c => c.PipingType ).Distinct().ToList() ;
      List<ConduitType> conduitTypes = ( from conduitTypeName in conduitTypeNames select new ConduitType( conduitTypeName, conduitTypeName ) ).ToList() ;
      conduitTypes.Add( new ConduitType( "↑", "↑" ) ) ;

      ConduitInformationViewModel viewModel = new ConduitInformationViewModel( conduitInformationModels, conduitTypes ) ;
      var dialog = new ConduitInformationDialog( viewModel, conduitsModelData ) ;
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

    private string GetPipingCrossSectionalArea( CeedStorable ceedStorable, List<HiroiSetCdMasterModel> hiroiSetCdMasterNormalModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<WiresAndCablesModel> wiresAndCablesModelData, List<DetailSymbolModel> allDetailSymbolModels, DetailSymbolModel parentDetailSymbolModel )
    {
      double pipingCrossSectionalArea = 0 ;
      List<string> routeNames = new List<string>() ;
      var detailSymbolModels = allDetailSymbolModels.Where( d => d.DetailSymbolId == parentDetailSymbolModel.DetailSymbolId && d.CountCableSamePosition == parentDetailSymbolModel.CountCableSamePosition ).ToList() ;
      foreach ( var detailSymbolModel in detailSymbolModels ) {
        if ( routeNames.Contains( detailSymbolModel.RouteName ) ) continue ;
        routeNames.Add( detailSymbolModel.RouteName ) ;
        var ceeDModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeeDSetCode == detailSymbolModel.Code ) ;
        if ( ceeDModel == null ) continue ;
        {
          var hiroiSetCdMasterNormalModel = hiroiSetCdMasterNormalModelData.FirstOrDefault( x => x.SetCode == ceeDModel.CeeDSetCode ) ;
          if ( hiroiSetCdMasterNormalModel == null ) continue ;
          {
            var hiroiSetMasterNormalModel = hiroiSetMasterNormalModelData.FirstOrDefault( x => x.ParentPartModelNumber == hiroiSetCdMasterNormalModel.LengthParentPartModelNumber ) ;
            if ( hiroiSetMasterNormalModel == null ) continue ;
            {
              var materialCode = int.Parse( hiroiSetMasterNormalModel.MaterialCode1 ).ToString() ;
              if ( string.IsNullOrEmpty( materialCode ) ) continue ;
              var masterModels = hiroiMasterModelData.FirstOrDefault( x => int.Parse( x.Buzaicd ).ToString() == materialCode ) ;
              if ( masterModels != null ) {
                var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == masterModels.Type && w.DiameterOrNominal == masterModels.Size1 && ( ( w.NumberOfHeartsOrLogarithm == "0" && masterModels.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && masterModels.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
                if ( wiresAndCablesModel != null )
                  pipingCrossSectionalArea += double.Parse( wiresAndCablesModel.CrossSectionalArea ) ;
              }
            }
          }
        }
      }

      pipingCrossSectionalArea /= 0.32 ;

      return pipingCrossSectionalArea.ToString() ;
    }

    public static Dictionary<string, int> GetPipingData( List<ConduitsModel> conduitsModelData, string pipingType, double pipingCrossSectionalArea )
    {
      Dictionary<string, int> pipingData = new Dictionary<string, int>() ;
      var conduitsModels = conduitsModelData.Where( c => c.PipingType == pipingType ).OrderBy( c => double.Parse( c.InnerCrossSectionalArea ) ).ToList() ;
      double crossSectionalArea = 0 ;
      while ( crossSectionalArea < pipingCrossSectionalArea ) {
        var conduitType = conduitsModels.FirstOrDefault( c => double.Parse( c.InnerCrossSectionalArea ) >= pipingCrossSectionalArea - crossSectionalArea ) ;
        if ( conduitType != null ) {
          if ( ! pipingData.ContainsKey( conduitType.Size ) )
            pipingData.Add( conduitType.Size, 1 ) ;
          else
            pipingData[ conduitType.Size ] = pipingData[ conduitType.Size ] + 1 ;
          break ;
        }
        else {
          var size = conduitsModels.Last().Size ;
          if ( ! pipingData.ContainsKey( size ) )
            pipingData.Add( size, 1 ) ;
          else
            pipingData[ size ] = pipingData[ size ] + 1 ;
          crossSectionalArea += double.Parse( conduitsModels.Last().InnerCrossSectionalArea ) ;
        }
      }

      return pipingData ;
    }

    public class ConduitType
    {
      public string Type { get ; set ; }
      public string Name { get ; set ; }
      
      public ConduitType( string type, string name )
      {
        Type = type ;
        Name = name ;
      }
    }
  }
}