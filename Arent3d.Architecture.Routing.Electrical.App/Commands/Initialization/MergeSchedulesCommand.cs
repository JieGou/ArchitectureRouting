using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  public class ScheduleSheetInstanceFilter : ISelectionFilter
  {
    public bool AllowElement( Element elem )
    {
      if ( elem is ScheduleSheetInstance ) return true ;
      return false ;
    }

    public bool AllowReference( Reference refer, XYZ pos )
    {
      return true ;
    }
  }

  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.MergeSchedulesCommand", DefaultString = "Merge Schedules" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class MergeSchedulesCommand : IExternalCommand
  {
    private const string DialogTitle = "Arent Notification" ;
    private const string PickElementMessage = "Select split schedules to merge" ;
    private const string TransactionName = "Electrical.App.Commands.Initialization.MergeSchedulesCommand" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;
      try {
        if ( document.ActiveView is not ViewSheet ) {
          TaskDialog.Show( DialogTitle, "表分割機能はシート上にしか動作しないため、シートへ移動してください。" ) ;
          return Result.Cancelled ;
        }

        var selectedElements = selection.PickElementsByRectangle( new ScheduleSheetInstanceFilter(), PickElementMessage ) ;
        if ( ! selectedElements.Any() ) return Result.Succeeded ;
        var scheduleSheetInstances = new List<ScheduleSheetInstance>() ;
        foreach ( var element in selectedElements ) {
          scheduleSheetInstances.Add( (ScheduleSheetInstance) element ) ;
        }

        var scheduleGroups = GetScheduleGroups( document, scheduleSheetInstances ) ;
        using Transaction transaction = new Transaction( document, TransactionName ) ;
        transaction.Start() ;
        foreach ( var scheduleGroup in scheduleGroups.Values ) {
          var result = MergeScheduleSheetInstance( document, scheduleGroup ) ;
          if ( result != Result.Succeeded ) return result ;
        }

        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Failed ;
      }
    }

    private static int GetHeaderRowCount( TableSectionData splitTable1, TableSectionData splitTable2 )
    {
      int rowCount = 0 ;
      int columnCount = splitTable1.NumberOfColumns ;
      bool stop = false ;
      for ( int i = 0 ; i < splitTable1.NumberOfRows ; i++ ) {
        for ( int j = 0 ; j < columnCount ; j++ ) {
          var cellText1 = splitTable1.GetCellText( i, j ) ;
          var cellText2 = splitTable2.GetCellText( i, j ) ;
          if ( cellText1 != cellText2 ) {
            stop = true ;
            break ;
          }
        }

        if ( stop ) break ;
        rowCount++ ;
      }

      return rowCount ;
    }

    private static Dictionary<ElementId, IList<ScheduleSheetInstance>> GetScheduleGroups( Document document, IList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var groups = new Dictionary<ElementId, IList<ScheduleSheetInstance>>() ;
      foreach ( var scheduleSheetInstance in scheduleSheetInstances ) {
        if ( document.GetElement( scheduleSheetInstance.ScheduleId ) is not ViewSchedule schedule ) continue ;
        if ( ! schedule.GetSplitStatus() ) continue ;
        var groupId = schedule.GetSplitGroupId() ;
        if ( groupId == null ) continue ;
        if ( groups.ContainsKey( groupId ) )
          groups[ groupId ].Add( scheduleSheetInstance ) ;
        else groups.Add( groupId, new List<ScheduleSheetInstance>() { scheduleSheetInstance } ) ;
      }

      var sortedGroups = new Dictionary<ElementId, IList<ScheduleSheetInstance>>() ;
      foreach ( var groupId in groups.Keys ) {
        sortedGroups.Add( groupId, groups[ groupId ].OrderBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetSplitIndex() ).ToList() ) ;
      }

      return sortedGroups ;
    }

    private static Result MergeScheduleSheetInstance( Document document, IList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      if ( scheduleSheetInstances.Count < 2 ) return Result.Succeeded ;
      var firstScheduleSheet = scheduleSheetInstances.First() ;
      if ( document.GetElement( firstScheduleSheet.ScheduleId ) is not ViewSchedule firstSchedule )
        return Result.Failed ;

      var imageMap = firstSchedule.GetImageMap() ;
      var firstSessionData = firstSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
      var firstSessionDataRowCount = firstSessionData.NumberOfRows ;
      var firstSessionDataColumnCount = firstSessionData.NumberOfColumns ;
      if ( document.GetElement( scheduleSheetInstances[ 1 ].ScheduleId ) is not ViewSchedule secondSchedule )
        return Result.Failed ;
      var headerRowCount = firstSchedule.GetHeaderRowCount() ;
      if ( headerRowCount == -1 ) headerRowCount = GetHeaderRowCount( firstSessionData, secondSchedule.GetTableData().GetSectionData( SectionType.Header ) ) ;
      for ( int i = 1 ; i < scheduleSheetInstances.Count ; i++ ) {
        if ( document.GetElement( scheduleSheetInstances[ i ].ScheduleId ) is not ViewSchedule schedule )
          return Result.Failed ;
        var sectionData = schedule.GetTableData().GetSectionData( SectionType.Header ) ;
        var rowCount = sectionData.NumberOfRows ;
        var columnCount = Math.Min( sectionData.NumberOfColumns, firstSessionDataColumnCount ) ;
        for ( int row = headerRowCount ; row < rowCount ; row++ ) {
          firstSessionData.InsertRow( firstSessionDataRowCount ) ;
          firstSessionData.SetRowHeight( firstSessionDataRowCount, sectionData.GetRowHeight( row ) ) ;
          for ( int column = 0 ; column < columnCount ; column++ ) {
            firstSessionData.SetCellText( firstSessionDataRowCount, column, sectionData.GetCellText( row, column ) ) ;
            firstSessionData.SetCellStyle( firstSessionDataRowCount, column, sectionData.GetTableCellStyle( row, column ) ) ;
            firstSessionData.SetCellType( firstSessionDataRowCount, column, sectionData.GetCellType( row, column ) ) ;
            if ( imageMap.ContainsKey( ( firstSessionDataRowCount, column ) ) )
              firstSessionData.InsertImage( firstSessionDataRowCount, column, imageMap[ ( firstSessionDataRowCount, column ) ] ) ;
          }

          firstSessionDataRowCount++ ;
        }

        document.Delete( schedule.Id ) ;
      }

      firstSchedule.Name = firstSchedule.GetOriginalTableName() ;
      return Result.Succeeded ;
    }
  }
}