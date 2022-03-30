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

    private static Dictionary<ElementId, IList<ScheduleSheetInstance>> GetScheduleGroups( Document document, IList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var groups = new Dictionary<ElementId, IList<ScheduleSheetInstance>>() ;
      foreach ( var scheduleSheetInstance in scheduleSheetInstances ) {
        if ( document.GetElement( scheduleSheetInstance.ScheduleId ) is not ViewSchedule schedule ) continue ;
        if ( ! schedule.GetSplitStatus() ) continue ;
        var groupId = schedule.GetParentScheduleId() ;
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
      var scheduleLevelDictionary = new Dictionary<int, IList<ScheduleSheetInstance>>() ;
      foreach ( var scheduleSheetInstance in scheduleSheetInstances ) {
        if ( document.GetElement( scheduleSheetInstance.ScheduleId ) is not ViewSchedule schedule )
          continue ;
        var splitLevel = schedule.GetSplitLevel() ;
        if ( scheduleLevelDictionary.ContainsKey( splitLevel ) ) {
          scheduleLevelDictionary[ splitLevel ].Add( scheduleSheetInstance ) ;
        }
        else {
          scheduleLevelDictionary.Add( splitLevel, new List<ScheduleSheetInstance> { scheduleSheetInstance } ) ;
        }
      }

      var sortedScheduleLevelDictionary = new Dictionary<int, IList<ScheduleSheetInstance>>() ;
      foreach ( var groupId in scheduleLevelDictionary.Keys ) {
        sortedScheduleLevelDictionary.Add( groupId, scheduleLevelDictionary[ groupId ].OrderBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetSplitIndex() ).ToList() ) ;
      }

      var sortedKeys = sortedScheduleLevelDictionary.Keys.ToList() ;
      sortedKeys.Sort() ;
      var mergedSchedules = new List<ScheduleSheetInstance>() ;
      for ( int i = sortedKeys.Count - 1 ; i > -1 ; i-- ) {
        var level = sortedKeys[ i ] ;
        var schedulesInCurrentLevel = sortedScheduleLevelDictionary[ level ].ToList() ;
        schedulesInCurrentLevel.AddRange( mergedSchedules ) ;
        schedulesInCurrentLevel = schedulesInCurrentLevel.OrderBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetSplitIndex() ).ToList() ;
        mergedSchedules.Clear() ;
        for ( int j = 0 ; j < schedulesInCurrentLevel.Count ; j++ ) {
          var scheduleSheet = schedulesInCurrentLevel[ j ] ;
          if ( document.GetElement( scheduleSheet.ScheduleId ) is not ViewSchedule schedule ) continue ;
          var splitIndex = schedule.GetSplitIndex() ;
          if ( j == schedulesInCurrentLevel.Count - 1 || splitIndex % 2 == 0 ) {
            schedule.SetSplitLevel( schedule.GetSplitLevel() - 1 ) ;
            schedule.SetSplitIndex( ( schedule.GetSplitIndex() - 1 ) / 2 ) ;
            mergedSchedules.Add( scheduleSheet ) ;
            continue ;
          }

          if ( splitIndex % 2 == 1 && j < schedulesInCurrentLevel.Count - 1 ) {
            var nextScheduleSheet = schedulesInCurrentLevel[ j + 1 ] ;
            if ( document.GetElement( nextScheduleSheet.ScheduleId ) is not ViewSchedule nextSchedule ) continue ;
            var nextSplitIndex = nextSchedule.GetSplitIndex() ;
            if ( nextSplitIndex == splitIndex + 1 ) {
              var (mergeResult, mergedSchedule) = MergeSameLevelScheduleSheetInstance( document, scheduleSheet, nextScheduleSheet ) ;
              if ( mergeResult != Result.Succeeded ) continue ;
              mergedSchedules.Add( mergedSchedule! ) ;
              j++ ;
            }
            else {
              schedule.SetSplitLevel( schedule.GetSplitLevel() - 1 ) ;
              schedule.SetSplitIndex( ( schedule.GetSplitIndex() - 1 ) / 2 ) ;
              mergedSchedules.Add( scheduleSheet ) ;
            }
          }
        }
      }

      return Result.Succeeded ;
    }

    private static (Result, ScheduleSheetInstance?) MergeSameLevelScheduleSheetInstance( Document document, ScheduleSheetInstance firstScheduleSheet, ScheduleSheetInstance secondScheduleSheet )
    {
      if ( document.GetElement( firstScheduleSheet.ScheduleId ) is not ViewSchedule firstSchedule )
        return ( Result.Failed, null ) ;

      var imageMap = firstSchedule.GetImageMap() ;
      var firstSessionData = firstSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
      var firstSessionDataRowCount = firstSessionData.NumberOfRows ;
      var firstSessionDataColumnCount = firstSessionData.NumberOfColumns ;
      var headerRowCount = firstSchedule.GetScheduleHeaderRowCount() ;
      if ( document.GetElement( secondScheduleSheet.ScheduleId ) is not ViewSchedule secondSchedule )
        return ( Result.Failed, null ) ;
      var sectionData = secondSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
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

      document.Delete( secondSchedule.Id ) ;

      firstSchedule.Name = firstSchedule.GetParentScheduleName() ;
      firstSchedule.SetSplitLevel( firstSchedule.GetSplitLevel() - 1 ) ;
      firstSchedule.SetSplitIndex( ( firstSchedule.GetSplitIndex() - 1 ) / 2 ) ;
      return ( Result.Succeeded, firstScheduleSheet ) ;
    }
  }
}