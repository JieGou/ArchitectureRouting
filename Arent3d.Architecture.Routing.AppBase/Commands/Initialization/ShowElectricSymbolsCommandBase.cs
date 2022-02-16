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
      const string defaultPlumbingType = "配管なし" ;
      var doc = commandData.Application.ActiveUIDocument.Document ;
      var uiDoc = commandData.Application.ActiveUIDocument ;
      var ceeDStorable = doc.GetAllStorables<CeedStorable>().FirstOrDefault() ;
      if ( ceeDStorable == null ) return Result.Failed ;
      var csvStorable = doc.GetCsvStorable() ;
      var hiroiSetMasterNormalModelData = csvStorable.HiroiSetMasterNormalModelData ;
      var hiroiSetMasterEcoModelData = csvStorable.HiroiSetMasterEcoModelData ;
      var hiroiMasterModelData = csvStorable.HiroiMasterModelData ;
      var detailTableModelData = doc.GetDetailTableStorable().DetailTableModelData ;
      var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ) ;
      var routePicked = pickedObjects.Select( e => e.GetRouteName() ).Distinct().ToList() ;
      var electricalSymbolModels = new ObservableCollection<ElectricalSymbolModel>() ;
      foreach ( var routeName in routePicked ) {
        string wireType = string.Empty ;
        string wireSize = string.Empty ;
        string wireStrip = string.Empty ;
        string plumbingType = string.Empty ;
        var plumbingSize = string.Empty ;
        var (startUniqueId, startCeeDSymbol, startCondition, endUniqueId, endCeeDSymbol, endCondition) = GetFromConnectorAndToConnectorCeeDCode( doc, routeName! ) ;
        var startCeeDModel = ceeDStorable.CeedModelData.FirstOrDefault( x => x.Condition.Equals( startCondition.Trim( '\r' ) ) && x.GeneralDisplayDeviceSymbol.Equals( startCeeDSymbol.Trim( '\r' ) ) ) ;
        var endCeeDModel = ceeDStorable.CeedModelData.FirstOrDefault( x => x.Condition.Equals( endCondition.Trim( '\r' ) ) && x.GeneralDisplayDeviceSymbol.Equals( endCeeDSymbol.Trim( '\r' ) ) ) ;
        if ( startCeeDModel == null || endCeeDModel == null ) continue ;
        var detailTableModelsByRouteName = detailTableModelData.Where( d => d.RouteName == routeName ).ToList() ;
        if ( detailTableModelData.Any() && detailTableModelsByRouteName.Any() ) {
          foreach ( var element in detailTableModelsByRouteName ) {
            wireType = element.WireType ;
            wireSize = element.WireSize ;
            wireStrip = element.WireStrip ;
            if ( element.IsParentRoute ) {
              plumbingType = element.PlumbingType ;
              plumbingSize = element.PlumbingSize ;
            }
            else {
              var parentRouteDetailTableModel = detailTableModelData.FirstOrDefault( d => d.DetailSymbolId == element.DetailSymbolId && d.IsParentRoute ) ;
              if ( parentRouteDetailTableModel != null ) {
                plumbingType = parentRouteDetailTableModel.PlumbingType ;
                plumbingSize = parentRouteDetailTableModel.PlumbingSize ;
              }
            }

            var startElectricalSymbolModel = new ElectricalSymbolModel( startUniqueId, startCeeDModel?.FloorPlanType ?? string.Empty, startCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
            var endElectricalSymbolModel = new ElectricalSymbolModel( endUniqueId, endCeeDModel?.FloorPlanType ?? string.Empty, endCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
            electricalSymbolModels.Add( startElectricalSymbolModel ) ;
            electricalSymbolModels.Add( endElectricalSymbolModel ) ;
          }
        }
        else {
          var allConnectors = doc.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).ToList() ;
          var endConnector = allConnectors.FirstOrDefault( c => c.UniqueId == endUniqueId ) ;
          string isEcoMode = endConnector!.LookupParameter( "IsEcoMode" ).AsString() ;
          var hiroiSetModels = ! string.IsNullOrEmpty( isEcoMode ) && bool.Parse( isEcoMode ) ? hiroiSetMasterEcoModelData.Where( x => x.ParentPartModelNumber.Contains( endCeeDModel.CeeDModelNumber ) ).Skip( 1 ) : hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( endCeeDModel.CeeDModelNumber ) ).Skip( 1 ) ;
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

          plumbingType = defaultPlumbingType ;
          var startElectricalSymbolModel = new ElectricalSymbolModel( startUniqueId, startCeeDModel?.FloorPlanType ?? string.Empty, startCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
          var endElectricalSymbolModel = new ElectricalSymbolModel( endUniqueId, endCeeDModel?.FloorPlanType ?? string.Empty, endCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, wireType, wireSize, wireStrip, plumbingType, plumbingSize ) ;
          electricalSymbolModels.Add( startElectricalSymbolModel ) ;
          electricalSymbolModels.Add( endElectricalSymbolModel ) ;
        }
      }

      return doc.Transaction( "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault( "Create electrical symbol schedules" ), _ =>
      {
        CreateSchedule( doc, electricalSymbolModels ) ;
        return Result.Succeeded ;
      } ) ;
    }

    private static void SetConnectorProperties( Document document, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      using Transaction transaction = new Transaction( document ) ;
      transaction.Start( "Set connector's properties" ) ;
      var detailTableModelsGroupByUniqueId = electricalSymbolModels.GroupBy( d => d.UniqueId ).ToDictionary( g => g.Key, g => g.ToList() ) ;
      foreach ( var (uniqueId, detailTableModelGroupByUniqueId) in detailTableModelsGroupByUniqueId ) {
        List<string> wiringAndPlumbingTypes = new List<string>() ;
        List<string> wiringTypes = new List<string>() ;
        List<string> plumingTypes = new List<string>() ;
        var detailTableModel = detailTableModelGroupByUniqueId.FirstOrDefault() ;
        foreach ( var item in detailTableModelGroupByUniqueId ) {
          var count = detailTableModelGroupByUniqueId.Count( d => d.WireType == item.WireType && d.WireSize == item.WireSize && d.WireStrip == item.WireStrip && d.PipingType == item.PipingType && d.PipingSize == item.PipingSize ) ;
          string wiringType = $"{item.WireType + item.WireSize,-15}{"－" + item.WireStrip + " x " + count,15}" ;
          string plumbingType = "(" + item.PipingType + item.PipingSize + ")" ;
          if ( wiringAndPlumbingTypes.Contains( wiringType + "-" + plumbingType ) ) continue ;
          wiringAndPlumbingTypes.Add( wiringType + "-" + plumbingType ) ;
          wiringTypes.Add( wiringType ) ;
          plumingTypes.Add( plumbingType ) ;
        }

        var connector = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).FirstOrDefault( c => c.UniqueId == uniqueId ) ;
        if ( connector == null ) continue ;
        UnGroupConnector( document, connector, connectorGroups ) ;
        connector.SetProperty( ConnectorFamilyParameter.DeviceSymbol, detailTableModel!.GeneralDisplayDeviceSymbol ) ;
        connector.SetProperty( ConnectorFamilyParameter.WiringType, string.Join( "\n", wiringTypes ) ) ;
        connector.SetProperty( ConnectorFamilyParameter.InPlumbingType, string.Join( "\n", plumingTypes ) ) ;
        var pathToImage = GetFloorPlanImagePath( detailTableModel.FloorPlanSymbol ) ;
        var imageType = document.GetAllElements<ImageType>().FirstOrDefault( i => i.Path == pathToImage ) ?? ImageType.Create( document, new ImageTypeOptions( pathToImage, false, ImageTypeSource.Import ) ) ;
        Parameter param = connector.get_Parameter( BuiltInParameter.ALL_MODEL_IMAGE ) ;
        param.Set( imageType.Id ) ;
      }

      var connectorUniqueIds = electricalSymbolModels.Select( c => c.UniqueId ).ToList() ;
      var connectorsNotSchedule = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).Where( c => ! connectorUniqueIds.Contains( c.UniqueId ) && c is FamilyInstance ).ToList() ;
      foreach ( var connector in connectorsNotSchedule ) {
        UnGroupConnector( document, connector, connectorGroups ) ;
        connector.SetProperty( ConnectorFamilyParameter.DeviceSymbol, string.Empty ) ;
        connector.SetProperty( ConnectorFamilyParameter.WiringType, string.Empty ) ;
        connector.SetProperty( ConnectorFamilyParameter.InPlumbingType, string.Empty ) ;
      }

      transaction.Commit() ;

      using Transaction transaction2 = new Transaction( document ) ;
      transaction2.Start( "Group connector" ) ;
      foreach ( var (connectorId, textNoteIds) in connectorGroups ) {
        // create group for updated connector (with new property) and related text note if any
        List<ElementId> groupIds = new List<ElementId> { connectorId } ;
        groupIds.AddRange( textNoteIds ) ;
        document.Create.NewGroup( groupIds ) ;
      }

      transaction2.Commit() ;
    }

    private static void UnGroupConnector( Document document, Element connector, Dictionary<ElementId, List<ElementId>> connectorGroups )
    {
      var parentGroup = document.GetElement( connector.GroupId ) as Group ;
      if ( parentGroup == null ) return ;
      // ungroup before set property
      var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
      List<ElementId> listTextNoteIds = new List<ElementId>() ;
      // ungroup textNote before ungroup connector
      foreach ( var group in attachedGroup ) {
        var ids = @group.GetMemberIds() ;
        listTextNoteIds.AddRange( ids ) ;
        @group.UngroupMembers() ;
      }

      connectorGroups.Add( connector.Id, listTextNoteIds ) ;
      parentGroup.UngroupMembers() ;
    }

    private static (string, string, string, string, string, string) GetFromConnectorAndToConnectorCeeDCode( Document document, string routeName )
    {
      var allConnectors = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.PickUpElements ).ToList() ;
      var conduitsOfRoute = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Conduits ).Where( c => c.GetRouteName() == routeName ).ToList() ;
      var fromUniqueId = string.Empty ;
      var toUniqueId = string.Empty ;
      foreach ( var conduit in conduitsOfRoute ) {
        var fromEndPoint = conduit.GetNearestEndPoints( true ).ToList() ;
        if ( ! fromEndPoint.Any() ) continue ;
        var fromEndPointKey = fromEndPoint.First().Key ;
        fromUniqueId = fromEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( fromUniqueId ) ) continue ;
        var fromConnector = allConnectors.FirstOrDefault( c => c.UniqueId == fromUniqueId ) ;
        if ( fromConnector!.IsTerminatePoint() || fromConnector!.IsPassPoint() ) {
          fromConnector!.TryGetProperty( PassPointParameter.RelatedFromConnectorUniqueId, out string? fromConnectorUniqueId ) ;
          if ( ! string.IsNullOrEmpty( fromConnectorUniqueId ) )
            fromUniqueId = fromConnectorUniqueId! ;
        }

        var toEndPoint = conduit.GetNearestEndPoints( false ).ToList() ;
        if ( ! toEndPoint.Any() ) continue ;
        var toEndPointKey = toEndPoint.First().Key ;
        toUniqueId = toEndPointKey.GetElementUniqueId() ;
        if ( string.IsNullOrEmpty( toUniqueId ) ) continue ;
        var toConnector = allConnectors.FirstOrDefault( c => c.UniqueId == toUniqueId ) ;
        if ( toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) {
          toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorUniqueId, out string? toConnectorUniqueId ) ;
          if ( ! string.IsNullOrEmpty( toConnectorUniqueId ) )
            toUniqueId = toConnectorUniqueId! ;
        }
      }

      var (fromGeneralSymbol, fromCondition) = GetTextFromGroup( document, allConnectors, fromUniqueId ) ;
      var (toGeneralSymbol, toCondition) = GetTextFromGroup( document, allConnectors, toUniqueId ) ;
      return ( fromUniqueId, fromGeneralSymbol, fromCondition, toUniqueId, toGeneralSymbol, toCondition ) ;
    }

    private static (string, string) GetTextFromGroup( Document document, IReadOnlyCollection<Element> allConnectors, string uniqueId )
    {
      var (result1, result2) = ( string.Empty, string.Empty ) ;
      Group? parentGroup = null ;
      var allGroup = document.GetAllElements<Group>().ToList() ;
      foreach ( var group in allGroup ) {
        var elementIds = group.GetMemberIds().ToList() ;
        var connector = allConnectors.FirstOrDefault( c => elementIds.Contains( c.Id ) && c.UniqueId == uniqueId ) ;
        if ( connector == null ) continue ;
        parentGroup = @group ;
        break ;
      }

      if ( parentGroup == null ) return ( result1, result2 ) ;
      // ungroup before set property
      var attachedGroup = document.GetAllElements<Group>().Where( x => x.AttachedParentId == parentGroup.Id ) ;
      var enumerable = attachedGroup as Group[] ?? attachedGroup.ToArray() ;
      if ( enumerable.Any() ) {
        var textNoteId = enumerable.FirstOrDefault()?.GetMemberIds().FirstOrDefault() ;
        var textNote = document.GetAllElements<TextNote>().FirstOrDefault( x => x.Id == textNoteId ) ;
        if ( textNote != null ) {
          result1 = textNote.Text ;
        }

        var textNoteId2 = enumerable.FirstOrDefault()?.GetMemberIds().Skip( 1 ).FirstOrDefault() ;
        var textNote2 = document.GetAllElements<TextNote>().FirstOrDefault( x => x.Id == textNoteId2 ) ;
        if ( textNote2 != null ) {
          result2 = textNote2.Text ;
        }
      }

      return ( result1, result2 ) ;
    }

    private static void CreateSchedule( Document document, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
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

      for ( var i = 1 ; i < fields.Count ; i++ ) {
        tsdHeader.InsertColumn( i ) ;
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
          var count = electricalSymbolModels.Count( d => d.WireType == item.WireType && d.WireSize == item.WireSize && d.WireStrip == item.WireStrip && d.PipingType == item.PipingType && d.PipingSize == item.PipingSize ) ;
          string wiringType = $"{item.WireType + item.WireSize,-15}{"－" + item.WireStrip + " x " + count,15}" ;
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