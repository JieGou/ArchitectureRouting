using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Reflection ;
using System.Windows.Forms ;
using Arent3d.Architecture.Routing.AppBase.Extensions;
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
    private const int HeaderRowCount = 3 ;
    private const string DefaultPlumbingType = "配管なし" ;
    public const string CreateScheduleSuccessfullyMessage = "集計表 \"'{0}'\" を作成しました" ;

    private readonly struct ConnectorInfo
    {
      public ConnectorInfo( string ceedSetCode, string deviceSymbol, string modelNumber )
      {
        CeedSetCode = ceedSetCode ;
        DeviceSymbol = deviceSymbol ;
        ModelNumber = modelNumber ;
      }

      public string CeedSetCode { get ; }
      public string DeviceSymbol { get ; }
      public string ModelNumber { get ; }
    }

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var ceedStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceedStorable == null ) return Result.Cancelled ;
      var electricalSymbolModels = new List<ElectricalSymbolModel>() ;
      try {
        var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ) ;
        CreateElectricalSymbolModels( doc, ceedStorable, electricalSymbolModels, pickedObjects ) ;
        if ( ! electricalSymbolModels.Any() ) {
          return Result.Cancelled ;
        }
      }
      catch {
        return Result.Cancelled ;
      }

      return doc.Transaction( "TransactionName.Commands.Initialization.ShowElectricSymbolsCommand".GetAppStringByKeyOrDefault( "Create electrical schedule" ), _ =>
      {
        var level = doc.ActiveView.GenLevel.Name ;
        var scheduleName = CreateElectricalSchedule( doc, electricalSymbolModels, level ) ;
        MessageBox.Show( string.Format( "Revit.Electrical.CreateSchedule.Message".GetAppStringByKeyOrDefault( CreateScheduleSuccessfullyMessage ), scheduleName ), "Message" ) ;
        return Result.Succeeded ;
      } ) ;
    }

    public static void CreateElectricalSymbolModels( Document doc, CeedStorable ceedStorable, List<ElectricalSymbolModel> electricalSymbolModels, IEnumerable<Element> conduits )
    {
      string[] showCeedModelSymbols = {"〇","○"} ;
      var csvStorable = doc.GetCsvStorable() ;
      var wiresAndCablesModelData = csvStorable.WiresAndCablesModelData ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.OtherElectricalElements ).ToList() ;
      var errorMess = string.Empty ;
      var ceedModelList = new List<(CeedModel ceedModel, string ceedConnectorUniqueId, string routeName)>() ;
      
      var routePicked = conduits.Select( e => e.GetRouteName() ).Distinct().ToList() ;
      foreach ( var routeName in routePicked ) {
        var fromConnectorInfoAndToConnectorInfo = GetFromConnectorInfoAndToConnectorInfo( doc, allConnectors, routeName!, ref errorMess ) ;
        if ( ! string.IsNullOrEmpty( errorMess ) ) {
          errorMess = string.Empty ;
          continue ;
        }
        var fromConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.CeedSetCode && x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.DeviceSymbol && x.ModelNumber == fromConnectorInfoAndToConnectorInfo.fromConnectorInfo.ModelNumber ) ;
        var toConnectorCeedModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.CeedSetCode == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.CeedSetCode && x.GeneralDisplayDeviceSymbol == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.DeviceSymbol && x.ModelNumber == fromConnectorInfoAndToConnectorInfo.toConnectorInfo.ModelNumber ) ;
        if ( fromConnectorCeedModel == null && toConnectorCeedModel == null ) continue ;

        if ( fromConnectorCeedModel != null && showCeedModelSymbols.Any( legend => legend == fromConnectorCeedModel.LegendDisplay ) ) {
          ceedModelList.Add( new( fromConnectorCeedModel, fromConnectorInfoAndToConnectorInfo.fromConnectorUniqueId, routeName! ) ) ;
        }

        if ( toConnectorCeedModel != null && showCeedModelSymbols.Any( legend => legend == toConnectorCeedModel.LegendDisplay ) ) {
          ceedModelList.Add( new( toConnectorCeedModel, fromConnectorInfoAndToConnectorInfo.toConnectorUniqueId, routeName! ) ) ;
        }
      }
      
      ceedModelList = ceedModelList.OrderBy( cmd => cmd.ceedModel.CeedModelNumber, new CeedModelNumberComparer() ).ToList() ;

      foreach ( var (ceedModel, ceedConnectorUniqueId, routeName) in ceedModelList ) {
        InsertDataFromRegularDatabaseIntoElectricalSymbolModel( doc, wiresAndCablesModelData, hiroiSetMasterEcoModelData, hiroiSetMasterNormalModelData, hiroiMasterModelData, allConnectors, electricalSymbolModels, ceedModel, ceedConnectorUniqueId, routeName ) ;
      }
    }

    private static void InsertDataFromRegularDatabaseIntoElectricalSymbolModel( Document document, List<WiresAndCablesModel> wiresAndCablesModelData, List<HiroiSetMasterModel> hiroiSetMasterEcoModelData, List<HiroiSetMasterModel> hiroiSetMasterNormalModelData, List<HiroiMasterModel> hiroiMasterModelData, List<Element> allConnectors, List<ElectricalSymbolModel> electricalSymbolModels,  CeedModel? connectorCeedModel, string connectorUniqueId, string routeName )
    {
      if ( connectorCeedModel == null ) return ;
      var isEcoMode = string.Empty ;
      var endConnector = allConnectors.First( c => c.UniqueId == connectorUniqueId ) ;
      endConnector?.TryGetProperty( ElectricalRoutingElementParameter.IsEcoMode, out isEcoMode ) ;
      var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode! ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( connectorCeedModel.CeedModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( connectorCeedModel.CeedModelNumber ) ).Skip( 1 ) ;
      foreach ( var item in hiroiSetModels ) {
        List<string> listMaterialCode = new() ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode1 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode2 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode2 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode3 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode3 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode4 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode4 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode5 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode5 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode6 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode6 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode7 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode7 ).ToString() ) ;
        if ( ! string.IsNullOrEmpty( item.MaterialCode8 ) ) listMaterialCode.Add( int.Parse( item.MaterialCode8 ).ToString() ) ;

        if ( ! listMaterialCode.Any() ) continue ;
        var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ).ToList() ;
        foreach ( var master in masterModels ) {
          var wiresAndCablesModel = wiresAndCablesModelData.FirstOrDefault( w => w.WireType == master.Type && w.DiameterOrNominal == master.Size1 && ( ( w.NumberOfHeartsOrLogarithm == "0" && master.Size2 == "0" ) || ( w.NumberOfHeartsOrLogarithm != "0" && master.Size2 == w.NumberOfHeartsOrLogarithm + w.COrP ) ) ) ;
          if ( wiresAndCablesModel == null ) continue ;
          var wireType = master.Type ;
          var wireSize = master.Size1 ;
          var wireStrip = string.IsNullOrEmpty( master.Size2 ) || master.Size2 == "0" ? "-" : master.Size2 ;

          var ( plumbingType, plumbingSize, isExposure, isInDoor ) = GetPlumbingInfoFromChangePlumbingInformationStorable( document, connectorUniqueId, routeName ) ;
          if ( plumbingType == DefaultPlumbingType && ! string.IsNullOrEmpty( item.MaterialCode2 ) ) {
            var masterModel = masterModels.FirstOrDefault( x => int.Parse( x.Buzaicd ).ToString() == int.Parse( item.MaterialCode2 ).ToString() ) ;
            if ( masterModel != null ) {
              plumbingType = masterModel.Type ;
              plumbingSize = masterModel.Size1 ;
            }
          }
          var endElectricalSymbolModel = new ElectricalSymbolModel( connectorUniqueId, connectorCeedModel.FloorPlanType, connectorCeedModel.GeneralDisplayDeviceSymbol, wireType, wireSize, wireStrip, plumbingType, plumbingSize, isExposure, isInDoor ) ;
          electricalSymbolModels.Add( endElectricalSymbolModel ) ;
        }
      }
    }

    private static (string plumbingType, string plumbingSize, bool isExposure, bool isInDoor) GetPlumbingInfoFromChangePlumbingInformationStorable( Document document, string connectorUniqueId, string routeName )
    {
      var changePlumbingInformationStorable = document.GetChangePlumbingInformationStorable() ;
      var plumbingType = DefaultPlumbingType ;
      var plumbingSize = string.Empty ;
      var isExposure = false ;
      var isInDoor = true ;
      var allConduitWithDirectionByZ = document.GetAllElements<Element>().OfCategory( BuiltInCategory.OST_Conduit ).Where( c => c.Location is LocationCurve { Curve: Line line } && ( line.Direction.Z is 1 or -1 ) ).Distinct().ToDictionary( c => c, c => (( c.Location as LocationCurve )?.Curve as Line)?.Origin ?? new XYZ() ) ;
      var connector = document.GetElement( connectorUniqueId ) ;
      var connectorLocation = ( connector.Location as LocationPoint ) ! ;
      var (x , y, z ) = connectorLocation.Point ;
      var conduit = allConduitWithDirectionByZ.Where( c => c.Key.GetRouteName() == routeName && Math.Abs( c.Value!.X - x ) < 0.01 && Math.Abs( c.Value.Y - y ) < 0.01 ).Select( c => c.Key ).FirstOrDefault() ;
      if ( conduit == null ) return ( plumbingType, plumbingSize, isExposure, isInDoor) ;
      {
        var conduitChangePlumbingInformation = changePlumbingInformationStorable.ChangePlumbingInformationModelData.SingleOrDefault( c => c.ConduitId == conduit.UniqueId ) ;
        if ( conduitChangePlumbingInformation != null ) {
          plumbingType = conduitChangePlumbingInformation.PlumbingType ;
          plumbingSize = conduitChangePlumbingInformation.PlumbingSize ;
          isExposure = conduitChangePlumbingInformation.IsExposure ;
          isInDoor = conduitChangePlumbingInformation.IsInDoor ;
        }

        return ( plumbingType, plumbingSize, isExposure, isInDoor) ;
      }
    }

  
    private static ( string fromConnectorUniqueId, ConnectorInfo fromConnectorInfo, string toConnectorUniqueId, ConnectorInfo toConnectorInfo) GetFromConnectorInfoAndToConnectorInfo( Document document, IReadOnlyCollection<Element> allConnectors, string routeName, ref string errorMess )
    {
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ) ;
      Element? fromConnector = null ;
      Element? toConnector = null ;
      foreach ( var conduit in conduitsOfRoute ) {
        var fromEndPoint = conduit.GetNearestEndPoints( true ).ToList() ;
        if ( fromEndPoint.Any() ) {
          var fromEndPointKey = fromEndPoint.FirstOrDefault()?.Key ;
          var fromUniqueId = fromEndPointKey?.GetElementUniqueId() ;
          if ( ! string.IsNullOrEmpty( fromUniqueId ) ) {
            var fromElement = allConnectors.FirstOrDefault( c => c.UniqueId == fromUniqueId ) ;
            if ( fromElement != null && ! fromElement.IsTerminatePoint() && ! fromElement.IsPassPoint() )
              fromConnector = fromElement ;
          }
        }
        
        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.FirstOrDefault()?.Key ;
        var toUniqueId = toEndPointKey?.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toUniqueId ) ) continue ;
        var toElement = allConnectors.FirstOrDefault( c => c.UniqueId == toUniqueId ) ;
        if ( toElement == null || toElement.IsTerminatePoint() || toElement.IsPassPoint() ) continue ;
        toConnector = toElement ;
      }

      if ( fromConnector == null || toConnector == null ) {
        errorMess = routeName ;
        return ( string.Empty, new ConnectorInfo( string.Empty, string.Empty, string.Empty ), string.Empty, new ConnectorInfo( string.Empty, string.Empty, string.Empty ) ) ;
      }

      var fromConnectorInfo = GetConnectorCeedCodeInfo( fromConnector ) ;
      var toConnectorInfo = GetConnectorCeedCodeInfo( toConnector ) ;
      return ( fromConnector.UniqueId, fromConnectorInfo, toConnector.UniqueId, toConnectorInfo ) ;
    }

    private static ConnectorInfo GetConnectorCeedCodeInfo( Element connector )
    {
      connector.TryGetProperty( ElectricalRoutingElementParameter.CeedCode, out string? ceedCode ) ;
      if ( string.IsNullOrEmpty( ceedCode ) ) return new ConnectorInfo( string.Empty, string.Empty, string.Empty ) ;
      var ceedCodeInfo = ceedCode!.Split( ':' ).ToList() ;
      var ceedSetCode = ceedCodeInfo.First() ;
      var deviceSymbol = ceedCodeInfo.Count > 1 ? ceedCodeInfo.ElementAt( 1 ) : string.Empty ;
      var modelNumber = ceedCodeInfo.Count > 2 ? ceedCodeInfo.ElementAt( 2 ) : string.Empty ;
      return new ConnectorInfo( ceedSetCode, deviceSymbol, modelNumber ) ;
    }

    public static string CreateElectricalSchedule( Document document, List<ElectricalSymbolModel> electricalSymbolModels, string level )
    {
      string scheduleName = "Revit.Electrical.Schedule.Name".GetDocumentStringByKeyOrDefault( document, "Electrical Symbol Table" ) + " - " + level + DateTime.Now.ToString( " yyyy-MM-dd HH-mm-ss" ) ;
      var electricalSchedule = document.GetAllElements<ViewSchedule>().SingleOrDefault( v => v.Name.Contains( scheduleName ) ) ;
      if ( electricalSchedule == null ) {
        electricalSchedule = ViewSchedule.CreateSchedule( document, new ElementId( BuiltInCategory.OST_ElectricalFixtures ) ) ;
        electricalSchedule.Name = scheduleName ;
        electricalSchedule.TrySetProperty( ElectricalRoutingElementParameter.ScheduleBaseName, scheduleName ) ;
      }

      CreateScheduleData( document, electricalSchedule, electricalSymbolModels ) ;
      return scheduleName ;
    }

    private enum ScheduleColumns
    {
      [DisplayStringKey( "シンボル" )]
      FloorPlanSymbol,

      [DisplayStringKey( "記号" )]
      DeviceSymbol,

      [DisplayStringKey( "配線" )]
      WiringType,

      [DisplayStringKey( "（屋内）" )]
      InPlumbingType,

      [DisplayStringKey( "（屋外）" )]
      OutPlumbingType,
    }

    private static void CreateScheduleData( Document document, ViewSchedule viewSchedule, List<ElectricalSymbolModel> electricalSymbolModels )
    {
      const int startRowData = 3 ;
      const int defaultColumnCount = 5 ;

      TableData tableData = viewSchedule.GetTableData() ;
      viewSchedule.SetScheduleHeaderRowCount( HeaderRowCount ) ;
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
      List<string> floorPlanSymbols = new() ;
      List<string> generalDisplayDeviceSymbols = new() ;
      List<string> wiringTypes = new() ;
      List<string> plumingTypes = new() ;
      List<bool> isExposures = new() ;
      List<bool> isInDoors = new() ;
      SummarizeElectricalSymbolByUniqueId( electricalSymbolModelsGroupByUniqueId, floorPlanSymbols, generalDisplayDeviceSymbols, wiringTypes, plumingTypes, isExposures, isInDoors ) ;

      for ( var i = 0 ; i < defaultColumnCount ; i++ ) {
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

      for ( var i = 0 ; i < defaultColumnCount ; i++ ) {
        if ( i < 3 ) tsdHeader.MergeCells( new TableMergedCell( 1, i, 2, i ) ) ;
        var columnWidth = 0.1 ;
        switch ( i ) {
          case 0 :
            tsdHeader.SetCellText( 1, i, ScheduleColumns.FloorPlanSymbol.GetDisplayStringKey().StringKey ) ;
            break ;
          case 1 :
            tsdHeader.SetCellText( 1, i, ScheduleColumns.DeviceSymbol.GetDisplayStringKey().StringKey ) ;
            break ;
          case 2 :
            tsdHeader.SetCellText( 1, i, ScheduleColumns.WiringType.GetDisplayStringKey().StringKey ) ;
            columnWidth = 0.2 ;
            break ;
          case 3 :
            tsdHeader.SetCellText( 2, i, ScheduleColumns.InPlumbingType.GetDisplayStringKey().StringKey ) ;
            columnWidth = 0.15 ;
            break ;
          default :
            tsdHeader.SetCellText( 2, i, ScheduleColumns.OutPlumbingType.GetDisplayStringKey().StringKey ) ;
            columnWidth = 0.15 ;
            break ;
        }

        tsdHeader.SetColumnWidth( i, columnWidth ) ;
      }

      for ( var j = 0 ; j < wiringTypes.Count ; j++ ) {
        if ( ! string.IsNullOrEmpty( floorPlanSymbols.ElementAt( j ) ) ) {
          var pathToImage = GetFloorPlanImagePath( floorPlanSymbols.ElementAt( j ) ) ;
          var imageType = document.GetAllElements<ImageType>().FirstOrDefault( i => i.Path == pathToImage ) ;
          if ( imageType == null ) {
#if REVIT2019
            imageType = ImageType.Create( document, pathToImage ) ;
#elif REVIT2020
            imageType = ImageType.Create( document, new ImageTypeOptions( pathToImage, false ) ) ;
#elif REVIT2021
            imageType = ImageType.Create( document, new ImageTypeOptions( pathToImage, false, ImageTypeSource.Import ) ) ;
#elif REVIT2022
            imageType = ImageType.Create( document, new ImageTypeOptions( pathToImage, false, ImageTypeSource.Import ) ) ;
#endif
          }

          if ( imageType != null ) {
            tsdHeader.InsertImage( startRowData + j, 0, imageType.Id ) ;
            viewSchedule.AddImageToImageMap( startRowData + j, 0, imageType.Id ) ;
          }

          tsdHeader.SetCellText( startRowData + j, 1, generalDisplayDeviceSymbols.ElementAt( j ) ) ;
        }

        tsdHeader.SetCellText( startRowData + j, 2, wiringTypes.ElementAt( j ) ) ;
        tsdHeader.SetCellText( startRowData + j, isInDoors.ElementAt( j ) ? 3 : 4, plumingTypes.ElementAt( j ) ) ;
      }
    }

    private static void SummarizeElectricalSymbolByUniqueId( Dictionary<string, List<ElectricalSymbolModel>> electricalSymbolModelsGroupByUniqueId, List<string> floorPlanSymbols, List<string> generalDisplayDeviceSymbols, List<string> wiringTypes, List<string> plumingTypes, List<bool> isExposures, List<bool> isInDoors )
    {
      foreach ( var (_, electricalSymbolModels) in electricalSymbolModelsGroupByUniqueId ) {
        List<string> wiringAndPlumbingTypes = new() ;
        var detailTableModel = electricalSymbolModels.FirstOrDefault() ;
        foreach ( var item in electricalSymbolModels ) {
          var count = electricalSymbolModels.Count( d => d.WireType == item.WireType && d.WireSize == item.WireSize && d.WireStrip == item.WireStrip && d.PipingType + d.PipingSize == item.PipingType + item.PipingSize ) ;
          string wiringType = string.IsNullOrEmpty( item.WireStrip ) || item.WireStrip == "-" ? $"{item.WireType + item.WireSize,-15}{"x " + count,15}" : $"{item.WireType + item.WireSize,-15}{"－" + item.WireStrip + " x " + count,15}" ;
          string plumbingType = "(" + item.PipingType + item.PipingSize + ")" ;
          if ( wiringAndPlumbingTypes.Contains( wiringType + "-" + plumbingType ) ) continue ;
          wiringAndPlumbingTypes.Add( wiringType + "-" + plumbingType ) ;
          floorPlanSymbols.Add( wiringAndPlumbingTypes.Count == 1 ? detailTableModel!.FloorPlanSymbol : string.Empty ) ;
          generalDisplayDeviceSymbols.Add( wiringAndPlumbingTypes.Count == 1 ? detailTableModel!.GeneralDisplayDeviceSymbol : string.Empty ) ;
          wiringTypes.Add( wiringType ) ;
          plumingTypes.Add( plumbingType ) ;
          isExposures.Add( item.IsExposure ) ;
          isInDoors.Add( item.IsInDoor ) ;
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