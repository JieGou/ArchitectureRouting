using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Architecture.Routing.Extensions ;
using Arent3d.Architecture.Routing.Storable ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using ImageType = Autodesk.Revit.DB.ImageType ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Initialization
{
  public class ShowElectricSymbolsCommandBase : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null ) return Result.Cancelled ;
      var csvStorable = doc.GetCsvStorable() ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var detailTableModelData = doc.GetDetailTableStorable().DetailTableModelData ;
      var allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).ToList() ;
      var electricalSymbolModels = new ObservableCollection<ElectricalSymbolModel>() ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ) ;
        var routePicked = pickedObjects.Select( e => e.GetRouteName() ).Distinct().ToList() ;
        foreach ( var routeName in routePicked ) {
          var (fromConnectorUniqueId, fromConnectorCeedSetCode, fromConnectorDeviceSymbol, fromConnectorModelNumber, toConnectorUniqueId, toConnectorCeedSetCode, toConnectorDeviceSymbol, toConnectorModelNumber) = GetFromConnectorInfoAndToConnectorInfo( doc, allConnectors, routeName! ) ;
          var fromConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == fromConnectorCeedSetCode && x.GeneralDisplayDeviceSymbol == fromConnectorDeviceSymbol && x.ModelNumber == fromConnectorModelNumber ) ;
          var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == toConnectorCeedSetCode && x.GeneralDisplayDeviceSymbol == toConnectorDeviceSymbol && x.ModelNumber == toConnectorModelNumber ) ;
          if ( fromConnectorCeedModel == null && toConnectorCeedModel == null ) continue ;
          var detailTableModelsByRouteName = detailTableModelData.Where( d => d.RouteName == routeName ).ToList() ;
          if ( detailTableModelsByRouteName.Any() ) {
            GetElectricalSymbolInfoFromDetailTableModelData( electricalSymbolModels, detailTableModelsByRouteName, fromConnectorCeedModel, toConnectorCeedModel, fromConnectorUniqueId, toConnectorUniqueId ) ;
          }
          else {
            GetElectricalSymbolInfoFromRegularDatabase( hiroiSetMasterEcoModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, allConnectors, electricalSymbolModels, fromConnectorCeedModel, toConnectorCeedModel, fromConnectorUniqueId, toConnectorUniqueId ) ;
          }
        }
      }
      catch {
        return Result.Cancelled ;
      }

      return doc.Transaction( "TransactionName.Commands.Initialization.ShowElectricSymbolsCommand".GetAppStringByKeyOrDefault( "Create electrical schedule" ), _ =>
      {
        CreateElectricalSchedule( doc, electricalSymbolModels ) ;
        return Result.Succeeded ;
      } ) ;
    }

    private void GetElectricalSymbolInfoFromDetailTableModelData( ObservableCollection<ElectricalSymbolModel> electricalSymbolModels, List<DetailTableModel> detailTableModelsByRouteName, CeedModel? fromConnectorCeedModel, CeedModel? toConnectorCeedModel, string fromConnectorUniqueId, string toConnectorUniqueId )
    {
      const string defaultChildPlumbingSymbol = "↑" ;
      foreach ( var element in detailTableModelsByRouteName ) {
        var wireType = element.WireType ;
        var wireSize = element.WireSize ;
        var wireStrip = element.WireStrip ;
        string plumbingType ;
        var plumbingSize = string.Empty ;
        if ( element.IsParentRoute ) {
          plumbingType = element.PlumbingType ;
          plumbingSize = element.PlumbingSize ;
        }
        else {
          plumbingType = element.PlumbingIdentityInfo.Split( '-' ).First().Replace( defaultChildPlumbingSymbol, "" ) ;
        }

        if ( fromConnectorCeedModel != null ) {
          var startElectricalSymbolModel = new ElectricalSymbolModel( fromConnectorUniqueId, fromConnectorCeedModel.FloorPlanType, fromConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
          electricalSymbolModels.Add( startElectricalSymbolModel ) ;
        }

        if ( toConnectorCeedModel == null ) continue ;
        var endElectricalSymbolModel = new ElectricalSymbolModel( toConnectorUniqueId, toConnectorCeedModel.FloorPlanType, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
        electricalSymbolModels.Add( endElectricalSymbolModel ) ;
      }
    }

    private void GetElectricalSymbolInfoFromRegularDatabase( List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<Element> allConnectors, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels, CeedModel? fromConnectorCeedModel, CeedModel? toConnectorCeedModel, string fromConnectorUniqueId, string toConnectorUniqueId )
    {
      const string defaultPlumbingType = "配管なし" ;
      var endConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toConnectorUniqueId ) ;
      endConnector!.TryGetProperty( RoutingFamilyLinkedParameter.IsEcoMode, out string? isEcoMode ) ;
      if ( toConnectorCeedModel == null ) return ;
      var wireType = string.Empty ;
      var wireSize = string.Empty ;
      var wireStrip = string.Empty ;
      var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode! ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( toConnectorCeedModel.CeedModelNumber ) ).Skip( 1 ) ;
      foreach ( var item in hiroiSetModels ) {
        List<string> listMaterialCode = new List<string>() ;
        if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
          listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
        }

        if ( ! listMaterialCode.Any() ) continue ;
        var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
        foreach ( var master in masterModels ) {
          wireType = master.Type ;
          wireSize = master.Size1 ;
          wireStrip = master.Size2 ;
        }
      }

      if ( fromConnectorCeedModel != null ) {
        var startElectricalSymbolModel = new ElectricalSymbolModel( fromConnectorUniqueId, fromConnectorCeedModel.FloorPlanType, fromConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, string.Empty ) ;
        electricalSymbolModels.Add( startElectricalSymbolModel ) ;
      }

      var endElectricalSymbolModel = new ElectricalSymbolModel( toConnectorUniqueId, toConnectorCeedModel.FloorPlanType, toConnectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, defaultPlumbingType, string.Empty ) ;
      electricalSymbolModels.Add( endElectricalSymbolModel ) ;
    }

    private static (string, string, string, string, string, string, string, string) GetFromConnectorInfoAndToConnectorInfo( Document document, IReadOnlyCollection<Element> allConnectors, string routeName )
    {
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      Element? fromConnector = null ;
      Element? toConnector = null ;
      foreach ( var conduit in conduitsOfRoute ) {
        var fromEndPoint = conduit.GetNearestEndPoints( true ).ToList() ;
        if ( ! fromEndPoint.Any() ) continue ;
        var fromEndPointKey = fromEndPoint.First().Key ;
        var fromUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( fromUniqueId ) ) continue ;
        var fromElement = allConnectors.FirstOrDefault( c => c.UniqueId == fromUniqueId ) ;
        if ( fromElement != null && ! fromElement.IsTerminatePoint() && ! fromElement.IsPassPoint() )
          fromConnector = fromElement ;

        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        var toUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toUniqueId ) ) continue ;
        var toElement = allConnectors.FirstOrDefault( c => c.UniqueId == toUniqueId ) ;
        if ( toElement == null || toElement.IsTerminatePoint() || toElement.IsPassPoint() ) continue ;
        toConnector = toElement ;
      }

      var (fromCeedCode, fromDeviceSymbol, fromModelNumber) = GetConnectorCeedCodeInfo( fromConnector ) ;
      var (toCeedCode, toDeviceSymbol, toModelNumber) = GetConnectorCeedCodeInfo( toConnector ) ;
      return ( fromConnector!.UniqueId, fromCeedCode, fromDeviceSymbol, fromModelNumber, toConnector!.UniqueId, toCeedCode, toDeviceSymbol, toModelNumber ) ;
    }

    private static (string, string, string) GetConnectorCeedCodeInfo( Element? connector )
    {
      var (ceedSetCode, deviceSymbol, modelNumber) = ( string.Empty, string.Empty, string.Empty ) ;
      if ( connector == null ) return ( ceedSetCode, deviceSymbol, modelNumber ) ;
      connector.TryGetProperty( ConnectorFamilyParameter.CeedCode, out string? ceedCode ) ;
      if ( string.IsNullOrEmpty( ceedCode ) ) return ( ceedSetCode, deviceSymbol, modelNumber ) ;
      var ceedCodeInfo = ceedCode!.Split( '-' ).ToList() ;
      ceedSetCode = ceedCodeInfo.First() ;
      deviceSymbol = ceedCodeInfo.Count > 1 ? ceedCodeInfo.ElementAt( 1 ) : string.Empty ;
      modelNumber = ceedCodeInfo.Count > 2 ? ceedCodeInfo.ElementAt( 2 ) : string.Empty ;
      return ( ceedSetCode, deviceSymbol, modelNumber ) ;
    }

    private static void CreateElectricalSchedule( Document document, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      const string scheduleName = "Electrical Schedule" ;
      var electricalSchedule = document.GetAllElements<ViewSchedule>().FirstOrDefault( v => v.Name.Contains( scheduleName ) ) ;
      if ( electricalSchedule == null ) {
        electricalSchedule = ViewSchedule.CreateSchedule( document, new ElementId( BuiltInCategory.OST_ElectricalFixtures ) ) ;
        electricalSchedule.Name = scheduleName ;
      }

      CreateScheduleData( document, electricalSchedule, electricalSymbolModels ) ;
    }

    private static void CreateScheduleData( Document document, ViewSchedule viewSchedule, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      const int startRowData = 3 ;
      var fields = new List<string>()
      {
        "シンボル",
        "記号",
        "配線",
        "（屋内）",
        "（屋外）"
      } ;
      TableData tableData = viewSchedule.GetTableData() ;
      TableSectionData tsdHeader = tableData.GetSectionData( SectionType.Header ) ;
      var rowCount = tsdHeader.NumberOfRows ;
      var columnCount = tsdHeader.NumberOfColumns ;

      // remove old data
      if ( columnCount != 1 ) {
        for ( var i = 1 ; i < rowCount ; i++ ) {
          tsdHeader.RemoveRow( tsdHeader.FirstRowNumber ) ;
        }

        for ( var i = 1 ; i < columnCount ; i++ ) {
          tsdHeader.RemoveColumn( tsdHeader.FirstColumnNumber ) ;
        }
      }

      var electricalSymbolModelsGroupByUniqueId = electricalSymbolModels.GroupBy( d => d.UniqueId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      List<string> floorPlanSymbols = new List<string>() ;
      List<string> generalDisplayDeviceSymbols = new List<string>() ;
      List<string> wiringTypes = new List<string>() ;
      List<string> plumingTypes = new List<string>() ;
      SummarizeElectricalSymbolByUniqueId( electricalSymbolModelsGroupByUniqueId, ref floorPlanSymbols, ref generalDisplayDeviceSymbols, ref wiringTypes, ref plumingTypes ) ;

      for ( var i = 0 ; i < fields.Count ; i++ ) {
        if ( i != 2 ) tsdHeader.InsertColumn( i ) ;
      }

      for ( var i = 1 ; i < wiringTypes.Count + startRowData ; i++ ) {
        tsdHeader.InsertRow( tsdHeader.FirstRowNumber ) ;
      }

      // Set columns name
      tsdHeader.MergeCells( new TableMergedCell( 0, 0, 0, 4 ) ) ;
      tsdHeader.SetCellText( 0, 0, "機器凡例" ) ;
      tsdHeader.MergeCells( new TableMergedCell( 1, 3, 1, 4 ) ) ;
      tsdHeader.SetCellText( 1, 3, "配管" ) ;

      for ( var i = 0 ; i < fields.Count ; i++ ) {
        if ( i < 3 ) tsdHeader.MergeCells( new TableMergedCell( 1, i, 2, i ) ) ;
        tsdHeader.SetCellText( i < 3 ? 1 : 2, i, fields.ElementAt( i ) ) ;
        var columnWidth = i == 2 ? 0.2 : 0.1 ;
        tsdHeader.SetColumnWidth( i, columnWidth ) ;
      }

      for ( var j = 0 ; j < wiringTypes.Count ; j++ ) {
        if ( ! string.IsNullOrEmpty( floorPlanSymbols.ElementAt( j ) ) ) {
          var pathToImage = GetFloorPlanImagePath( floorPlanSymbols.ElementAt( j ) ) ;
          var imageType = document.GetAllElements<ImageType>().FirstOrDefault( i => i.Path == pathToImage ) ?? ImageType.Create( document, new ImageTypeOptions( pathToImage, false, ImageTypeSource.Import ) ) ;
          tsdHeader.InsertImage( startRowData + j, 0, imageType.Id ) ;
          tsdHeader.SetCellText( startRowData + j, 1, generalDisplayDeviceSymbols.ElementAt( j ) ) ;
        }

        tsdHeader.SetCellText( startRowData + j, 2, wiringTypes.ElementAt( j ) ) ;
        tsdHeader.SetCellText( startRowData + j, 3, plumingTypes.ElementAt( j ) ) ;
      }
    }

    private static void SummarizeElectricalSymbolByUniqueId( Dictionary<string, List<ElectricalSymbolModel>> electricalSymbolModelsGroupByUniqueId, ref List<string> floorPlanSymbols, ref List<string> generalDisplayDeviceSymbols, ref List<string> wiringTypes, ref List<string> plumingTypes )
    {
      foreach ( var (_, electricalSymbolModels) in electricalSymbolModelsGroupByUniqueId ) {
        List<string> wiringAndPlumbingTypes = new List<string>() ;
        var detailTableModel = electricalSymbolModels.FirstOrDefault() ;
        foreach ( var item in electricalSymbolModels ) {
          var count = electricalSymbolModels.Count( d => d.WireType == item.WireType && d.WireSize == item.WireSize && d.WireStrip == item.WireStrip && d.PipingType + d.PipingSize == item.PipingType + item.PipingSize ) ;
          string wiringType = string.IsNullOrEmpty( item.WireStrip ) ? $"{item.WireType + item.WireSize,-15}{"x " + count,28}" : $"{item.WireType + item.WireSize,-15}{"－" + item.WireStrip + " x " + count,15}" ;
          string plumbingType = "(" + item.PipingType + item.PipingSize + ")" ;
          if ( wiringAndPlumbingTypes.Contains( wiringType + "-" + plumbingType ) ) continue ;
          wiringAndPlumbingTypes.Add( wiringType + "-" + plumbingType ) ;
          floorPlanSymbols.Add( wiringAndPlumbingTypes.Count == 1 ? detailTableModel!.FloorPlanSymbol : string.Empty ) ;
          generalDisplayDeviceSymbols.Add( wiringAndPlumbingTypes.Count == 1 ? detailTableModel!.GeneralDisplayDeviceSymbol : string.Empty ) ;
          wiringTypes.Add( wiringType ) ;
          plumingTypes.Add( plumbingType ) ;
        }
      }
    }

    private static string GetFloorPlanImagePath( string floorPlanType )
    {
      string fileName = "ConnectorOneSide37.png" ;
      string directory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location ) ! ;
      var resourcesPath = Path.Combine( directory.Substring( 0, directory.IndexOf( "bin", StringComparison.Ordinal ) ), "resources" ) ;
      foreach ( var item in (ConnectorOneSideFamilyType[]) Enum.GetValues( typeof( ConnectorOneSideFamilyType ) ) ) {
        if ( floorPlanType != item.GetFieldName() ) continue ;
        fileName = item.GetFieldName() + ".png" ;
        break ;
      }

      return Path.Combine( resourcesPath, "Images", fileName ) ;
    }
  }
}