using System ;
using System.Collections.Generic ;
using System.Linq ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
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
        var scheduleSheetInstances = selectedElements.Cast<ScheduleSheetInstance>().ToList() ;
        scheduleSheetInstances = DistinctScheduleSheet( scheduleSheetInstances ) ;
        using Transaction transaction = new(document, TransactionName) ;
        transaction.Start() ;
        var result = MergeScheduleSheetInstance( document, scheduleSheetInstances ) ;
        if ( result != Result.Succeeded ) return result ;
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

    private static List<ScheduleSheetInstance> DistinctScheduleSheet( IList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var distinctScheduleSheets = new List<ScheduleSheetInstance>() ;
      distinctScheduleSheets.AddRange( scheduleSheetInstances ) ;
      foreach ( var scheduleSheet in scheduleSheetInstances ) {
        var removes = scheduleSheetInstances.Where( s => scheduleSheet.ScheduleId.IntegerValue == s.ScheduleId.IntegerValue ).ToList() ;
        foreach ( var remove in removes ) {
          distinctScheduleSheets.Remove( remove ) ;
        }

        if ( removes.Contains( scheduleSheet ) )
          distinctScheduleSheets.Add( scheduleSheet ) ;
      }

      return distinctScheduleSheets ;
    }

    private static Result MergeScheduleSheetInstance( Document document, IEnumerable<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var sheetInstances = scheduleSheetInstances.ToList() ;
      var scheduleSheetInstancesByParentIds = sheetInstances.GroupBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetParentScheduleId() ) ;
      foreach ( var scheduleSheetInstancesByParentId in scheduleSheetInstancesByParentIds ) {
        var sortedScheduleSheetInstances = scheduleSheetInstancesByParentId.OrderBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetSplitIndex() ).ToList() ;
        var mergedSchedules = MergeMultiplesScheduleSheetInstance( document, sortedScheduleSheetInstances ) ;
        if ( mergedSchedules == null ) return Result.Failed ;
        if ( document.GetElement( mergedSchedules.ScheduleId ) is not ViewSchedule mergedViewSchedule ) return Result.Failed ;
        UpdateInfoOfScheduleHasSameBase( document, mergedViewSchedule ) ;
      }

      return Result.Succeeded ;
    }

    private static void UpdateInfoOfScheduleHasSameBase( Document document, ViewSchedule currentSchedule )
    {
      var allSchedulesHasTheSameBase = new FilteredElementCollector( document ).OfClass( typeof( ViewSchedule ) ).ToElements().ConvertAll( e => (ViewSchedule) e ).Where( s => s.GetScheduleBaseName().Equals( currentSchedule.GetScheduleBaseName() ) ).ToList() ;
      var sortedSchedulesHasTheSameBase = allSchedulesHasTheSameBase.OrderBy( s => ( document.GetElement( s.Id ) as ViewSchedule )?.GetSplitIndex() ).ToList() ;
      var splitLevel = allSchedulesHasTheSameBase.Count ;
      if ( splitLevel <= 1 ) {
        var schedule = sortedSchedulesHasTheSameBase.First() ;
        schedule.SetSplitIndex( 0 ) ;
        schedule.SetSplitLevel( 1 ) ;
        schedule.Name = schedule.GetScheduleBaseName() ;
      }
      else {
        for ( var i = 0 ; i < splitLevel ; i++ ) {
          var splitIndex = i + 1 ;
          var schedule = sortedSchedulesHasTheSameBase[ i ] ;
          schedule.SetSplitIndex( splitIndex ) ;
          schedule.SetSplitLevel( splitLevel ) ;
          schedule.Name = schedule.GetScheduleBaseName() + " " + splitIndex + "/" + splitLevel ;
        }
      }
    }


    private static ScheduleSheetInstance? MergeMultiplesScheduleSheetInstance( Document document, IReadOnlyList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var firstScheduleSheet = scheduleSheetInstances.First() ;
      if ( document.GetElement( firstScheduleSheet.ScheduleId ) is not ViewSchedule firstViewSchedule ) return null ;

      var imageMap = firstViewSchedule.GetImageMap() ;
      var firstSessionData = firstViewSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
      var firstSessionDataRowCount = firstSessionData.NumberOfRows ;
      var firstSessionDataColumnCount = firstSessionData.NumberOfColumns ;
      var headerRowCount = firstViewSchedule.GetScheduleHeaderRowCount() ;

      for ( var i = 1 ; i < scheduleSheetInstances.Count ; i++ ) {
        var nextScheduleSheet = scheduleSheetInstances[ i ] ;
        if ( document.GetElement( nextScheduleSheet.ScheduleId ) is not ViewSchedule nextViewSchedule ) return null ;
        var secondImageMap = nextViewSchedule.GetImageMap() ;
        var sectionData = nextViewSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
        var rowCount = sectionData.NumberOfRows ;
        var columnCount = Math.Min( sectionData.NumberOfColumns, firstSessionDataColumnCount ) ;
        for ( var row = headerRowCount ; row < rowCount ; row++ ) {
          firstSessionData.InsertRow( firstSessionDataRowCount ) ;
          firstSessionData.SetRowHeight( firstSessionDataRowCount, sectionData.GetRowHeight( row ) ) ;
          for ( var column = 0 ; column < columnCount ; column++ ) {
            var mergedCell = sectionData.GetMergedCell( row, column ) ;
            if ( mergedCell.Top == row && mergedCell.Left == column && ( mergedCell.Top != mergedCell.Bottom || mergedCell.Left != mergedCell.Right ) )
              firstSessionData.MergeCells( new TableMergedCell( firstSessionDataRowCount + mergedCell.Top - row, mergedCell.Left, firstSessionDataRowCount + mergedCell.Bottom - row, mergedCell.Right ) ) ;
            firstSessionData.SetCellText( firstSessionDataRowCount, column, sectionData.GetCellText( row, column ) ) ;
            firstSessionData.SetCellStyle( firstSessionDataRowCount, column, sectionData.GetTableCellStyle( row, column ) ) ;
            firstSessionData.SetCellType( firstSessionDataRowCount, column, sectionData.GetCellType( row, column ) ) ;
            if ( ! secondImageMap.ContainsKey( ( row, column ) ) ) continue ;
            firstSessionData.InsertImage( firstSessionDataRowCount, column, secondImageMap[ ( row, column ) ] ) ;
            imageMap.Add( ( firstSessionDataRowCount, column ), secondImageMap[ ( row, column ) ] ) ;
          }

          firstSessionDataRowCount++ ;
        }

        document.Delete( nextViewSchedule.Id ) ;
        firstViewSchedule.SetImageMap( imageMap ) ;
        firstViewSchedule.SetSplitLevel( firstViewSchedule.GetSplitLevel() - 1 ) ;
      }

      return firstScheduleSheet ;
    }
  }
}