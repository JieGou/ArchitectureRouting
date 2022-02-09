using System ;
using System.Collections.Generic ;
using System.Collections.ObjectModel ;
using System.Linq ;
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
      var pickedObjects = uiDoc.Selection.PickElementsByRectangle( ConduitSelectionFilter.Instance, "ドラックで複数コンジットを選択して下さい。" ).Where( p => p is Conduit ) ;
      var electricalSymbolModels = new ObservableCollection<ElectricalSymbolModel>() ;
      var processedDetailSymbol = new List<string>() ;
      foreach ( var element in pickedObjects ) {
        string pipingType = element.Name ;
        var existSymbolRoute = element.GetRouteName() ?? string.Empty ;
        if ( ! string.IsNullOrEmpty( existSymbolRoute ) && ceedStorable != null ) {
          if ( ! processedDetailSymbol.Contains( existSymbolRoute ) ) {
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

            var ( startElementId, startCeeDSymbol, startCondition, endElementId, endCeeDSymbol, endCondition) = GetFromConnectorAndToConnectorCeeDCode( doc, startTeminateId, endTeminateId ) ;
            var startCeeDModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.Condition.Equals( startCondition.Trim( '\r' ) ) && x.GeneralDisplayDeviceSymbol.Equals( startCeeDSymbol.Trim( '\r' ) ) ) ;
            var endCeeDModel = ceedStorable.CeedModelData.FirstOrDefault( x => x.Condition.Equals( endCondition.Trim( '\r' ) ) && x.GeneralDisplayDeviceSymbol.Equals( endCeeDSymbol.Trim( '\r' ) ) ) ;
            var startFloorPlanImage = string.IsNullOrEmpty( startCeeDModel?.Base64FloorPlanImages ) ? null : CeedModel.Base64StringToBitmap( startCeeDModel?.Base64FloorPlanImages! ) ;
            var startElectricalSymbolModel = new ElectricalSymbolModel( startElementId, startCeeDModel?.FloorPlanSymbol ?? string.Empty, startFloorPlanImage, startCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, string.Empty, string.Empty, string.Empty, pipingType, string.Empty ) ;
            var endFloorPlanImage =  string.IsNullOrEmpty(  endCeeDModel?.Base64FloorPlanImages ) ? null : CeedModel.Base64StringToBitmap( endCeeDModel?.Base64FloorPlanImages! ) ;
            var endElectricalSymbolModel = new ElectricalSymbolModel( endElementId, endCeeDModel?.FloorPlanSymbol ?? string.Empty, endFloorPlanImage, endCeeDModel?.GeneralDisplayDeviceSymbol ?? string.Empty, string.Empty, string.Empty, string.Empty, pipingType, string.Empty ) ;
            processedDetailSymbol.Add( existSymbolRoute ) ;
            if ( endCeeDModel != null ) {
              var hiroiSetModels = hiroiSetMasterNormalModelData.Where( x => x.ParentPartModelNumber.Contains( endCeeDModel.CeeDModelNumber ) ).Skip( 1 ) ;
              foreach ( var item in hiroiSetModels ) {
                List<string> listMaterialCode = new List<string>() ;
                if ( ! string.IsNullOrWhiteSpace( item.MaterialCode1 ) ) {
                  listMaterialCode.Add( int.Parse( item.MaterialCode1 ).ToString() ) ;
                }

                if ( listMaterialCode.Any() ) {
                  var masterModels = hiroiMasterModelData.Where( x => listMaterialCode.Contains( int.Parse( x.Buzaicd ).ToString() ) ) ;
                  foreach ( var master in masterModels ) {
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

            if ( startCeeDModel != null && endCeeDModel != null ) {
              electricalSymbolModels.Add( startElectricalSymbolModel ) ;
              electricalSymbolModels.Add( endElectricalSymbolModel ) ;
            }
          }
        }
      }

      SetConnectorProperties( doc, electricalSymbolModels ) ;
      return doc.Transaction( "TransactionName.Commands.Routing.ConduitInformation".GetAppStringByKeyOrDefault( "Create electrical symbol schedules" ), _ =>
      {
        CreateSchedule( doc ) ;
        return Result.Succeeded ;
      } ) ;
    }

    private void SetConnectorProperties( Document document, ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      Dictionary<ElementId, List<ElementId>> connectorGroups = new Dictionary<ElementId, List<ElementId>>() ;
      using Transaction transaction = new Transaction( document ) ;
      transaction.Start( "Set connector's properties" ) ;
      foreach ( var item in electricalSymbolModels ) {
        var connector =  document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).FirstOrDefault( c => c.Id.IntegerValue.ToString() == item.ElementId ) ;
        if ( connector == null ) continue ;
        UnGroupConnector( document, connector, connectorGroups ) ;
        connector.SetProperty( ConnectorFamilyParameter.DeviceSymbol, item.GeneralDisplayDeviceSymbol ) ;
        connector.SetProperty( ConnectorFamilyParameter.WiringType, $"{item.WireType + item.WireSize,-15}{"－" + item.WireStrip + " x 1",15}" ) ;
        connector.SetProperty( ConnectorFamilyParameter.InPlumbingType, item.PipingType + item.PipingSize ) ;
      }

      var connectorIds = electricalSymbolModels.Select( c => c.ElementId ).ToList() ;
      var connectorsNotSchedule = document.GetAllElements<Element>().OfCategory( BuiltInCategorySets.Connectors ).Where( c => ! connectorIds.Contains(c.Id.IntegerValue.ToString() ) && c is FamilyInstance ).ToList() ;
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
      if ( parentGroup == null ) return;
      // ungroup before set property
      var attachedGroup = document.GetAllElements<Group>()
        .Where( x => x.AttachedParentId == parentGroup.Id ) ;
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

    private string GenerateTextTable( ObservableCollection<ElectricalSymbolModel> electricalSymbolModels )
    {
      var line = GenerateString( '━', 45, string.Empty ) ;
      var result = string.Empty ;
      result += "　機器凡例" ;
      result += $"\r{line}\r{GenerateString( '　', 40, String.Empty )}{GenerateString( '　', 28, "配　　管" )}" ;
      result += $"\r{GenerateString( '　', 6, "シンボル" )}{GenerateString( '　', 6, "記号" )}{GenerateString( '　', 28, "配　線" )}{GenerateString( '━', 16, string.Empty )}" ;
      result += $"\r{GenerateString( '　', 40, String.Empty )}{GenerateString( '　', 16, "（屋内）" )}{GenerateString( '　', 10, "（屋外）" )}" ;
      result = electricalSymbolModels.Aggregate( result, ( current, item ) => current + $"\r{line}\r{GenerateString( '　', 6, item.FloorPlanSymbol )}{GenerateString( '　', 8, item.GeneralDisplayDeviceSymbol )}{GenerateString( '　', 14, item.WireType + item.WireSize )} {GenerateString( '　', 14, "－" + item.WireStrip + " x 1" )}{GenerateString( '　', 16, item.PipingType + item.PipingSize )}" ) ;

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

      var partOfLength = ( lenght - middle.Length + lesslength ) / 2 ;
      if ( partOfLength < 0 ) {
        partOfLength = 0 ;
      }

      var partOfStr = new string( chr, partOfLength ) ;
      return partOfStr + middle + partOfStr ;
    }

    private static (string, string, string, string, string, string) GetFromConnectorAndToConnectorCeeDCode( Document document, string fromElementId, string toElementId )
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

      if ( ! string.IsNullOrEmpty( toElementId ) ) {
        var toConnector = allConnectors.FirstOrDefault( c => c.Id.IntegerValue.ToString() == toElementId ) ;
        if ( toConnector!.IsTerminatePoint() || toConnector!.IsPassPoint() ) {
          toConnector!.TryGetProperty( PassPointParameter.RelatedConnectorId, out string? toConnectorId ) ;
          if ( ! string.IsNullOrEmpty( toConnectorId ) )
            toElementId = toConnectorId! ;
        }
      }

      var (fromGeneralSymbol, fromCondition) = GetTextFromGroup( document, fromElementId ) ;
      var (toGeneralSymbol, toCondition) = GetTextFromGroup( document, toElementId ) ;
      return ( fromElementId, fromGeneralSymbol, fromCondition, toElementId, toGeneralSymbol, toCondition ) ;
    }

    private static (string, string) GetTextFromGroup( Document document, string elementId )
    {
      var (result1, result2) = ( string.Empty, string.Empty ) ;
      var parentGroup = document.GetAllElements<Group>().FirstOrDefault( x => x.GetMemberIds().Any( y => y.ToString().Equals( elementId ) ) ) ;
      if ( parentGroup != null ) {
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
      }

      return ( result1, result2 ) ;
    }

    private static void CreateSchedule( Document document )
    {
      var electricalSchedule = document.GetAllElements<ViewSchedule>().FirstOrDefault( v => v.Name.Contains( "Electrical Fixture" ) ) ;
      if ( electricalSchedule != null ) return ;
      electricalSchedule = ViewSchedule.CreateSchedule( document, new ElementId( BuiltInCategory.OST_ElectricalFixtures ) ) ;
      AddFieldToSchedule( document, electricalSchedule ) ;
      SetScheduleStyle( electricalSchedule ) ;
    }

    private static void AddFieldToSchedule( Document document, ViewSchedule viewSchedule )
    {
      var fields = new List<string>()
      {
        "シンボル",
        "記号",
        "配線",
        "配管（屋内）",
        "配管（屋外）"
      } ;
      List<SchedulableField> schedulableFields = new List<SchedulableField>() ;
      //Get all schedulable fields from view schedule definition.
      IList<SchedulableField> allSchedulableFields = viewSchedule.Definition.GetSchedulableFields() ;
      foreach ( SchedulableField sf in allSchedulableFields ) {
        //Get all schedule field ids
        IList<ScheduleFieldId> ids = viewSchedule.Definition.GetFieldOrder() ;
        var fieldAlreadyAdded = ids.Any( id => viewSchedule.Definition.GetField( id ).GetSchedulableField() == sf ) ;
        //If schedulable field doesn't exist in view schedule, add it.
        if ( fieldAlreadyAdded == false && fields.Contains( sf.GetName( document ) ) ) {
          schedulableFields.Add( sf ) ;
        }
      }

      foreach ( var schedulableField in fields.Select( field => schedulableFields.FirstOrDefault( s => s.GetName( document ) == field ) ).Where( schedulableField => schedulableField != null ) ) {
        var scheduleField = viewSchedule.Definition.AddField( schedulableField ) ;
        if ( schedulableField == null || schedulableField.GetName( document ) != "記号" ) continue ;
        var filter = new ScheduleFilter( scheduleField.FieldId, ScheduleFilterType.NotEqual, string.Empty ) ;
        viewSchedule.Definition.AddFilter( filter ) ;
      }
    }

    private static void SetScheduleStyle( ViewSchedule viewSchedule )
    {
      TableData tableData = viewSchedule.GetTableData() ;
      TableSectionData tsd = tableData.GetSectionData( SectionType.Header ) ;
      tsd.SetCellText( 0,0, "機器凡例" ) ;
      
      TableSectionData tsd2 = tableData.GetSectionData( SectionType.Body ) ;
      tsd2.SetColumnWidth( 2, 0.15 ) ;
      tsd2.SetColumnWidth( 3, 0.2 ) ;
    }
  }
}