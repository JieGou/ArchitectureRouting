using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Windows ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Architecture.Routing.AppBase.Selection ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Autodesk.Revit.DB.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  public class SelectionFilter1 : ISelectionFilter
  {
    public bool AllowElement( Element elem )
    {
      if ( elem is ScheduleSheetInstance ) return true ;
      // if ( elem.Category.Id.IntegerValue == (int) BuiltInCategory.OST_WallsDefault ) return true ;
      // if ( elem.Category.Id.IntegerValue == (int) BuiltInCategory.OST_WallsInsulation ) return true ;
      // if ( elem.Category.Id.IntegerValue == (int) BuiltInCategory.OST_WallsStructure ) return true ;

      return false ;

    }

    public bool AllowReference( Reference refer, XYZ pos )
    {
      // if ( refer.GeometryObject == null ) return false ;
      //
      // if ( refer.GeometryObject is Edge ) return true ;
      // if ( refer.GeometryObject is Face ) return true ;

      return true ;

    }
  }

  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.MergeSchedulesCommand", DefaultString = "Merge Schedules" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class MergeSchedulesCommand : IExternalCommand
  {
    private const string DialogTitle = "Arent Notification" ;
    private const int SplitFromRow = 4 ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;
      try {
        if ( document.ActiveView is not ViewSheet ) {
          TaskDialog.Show( DialogTitle, "表分割機能はシート上にしか動作しないため、シートへ移動してください。" ) ;
          return Result.Cancelled ;
        }

        var selectedElements = selection.PickElementsByRectangle( new SelectionFilter1(), "ドラックで複数コネクタを選択して下さい。" ) ;
        if ( ! selectedElements.Any() ) return Result.Succeeded ;
        var scheduleSheetInstances = new List<ScheduleSheetInstance>() ;
        foreach ( var element in selectedElements ) {
          scheduleSheetInstances.Add( (ScheduleSheetInstance) element ) ;
        }
        var scheduleGroups = GetScheduleGroups( document, scheduleSheetInstances ) ;
        foreach ( var scheduleGroup in scheduleGroups.Values ) {
          var result = MergeScheduleSheetInstance( document, scheduleSheetInstances ) ;
          if ( result != Result.Succeeded ) return result ;
        }
        
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

    private static int GetHeaderRowCount( TableSectionData splitedTable1, TableSectionData splitedTable2 )
    {
      int rowCount = 0 ;
      int columnCount = splitedTable1.NumberOfColumns ;
      bool stop = false ;
      for ( int i = 0 ; i < splitedTable1.NumberOfRows ; i++ ) {
        for ( int j = 0 ; j < columnCount ; j++ ) {
          var cellText1 = splitedTable1.GetCellText( i, j ) ;
          var cellText2 = splitedTable2.GetCellText( i, j ) ;
          var cellType1 = splitedTable1.GetCellType( i, j ) ;
          var cellType2 = splitedTable2.GetCellType( i, j ) ;
          if ( cellText1 != cellText2 ) {
            stop = true ;
            break;
          }
        }
        if(stop) break;
        rowCount++ ;
      }

      return rowCount ;
      
    }

    private static Dictionary<ElementId, IList<ScheduleSheetInstance>> GetScheduleGroups( Document document,IList<ScheduleSheetInstance> scheduleSheetInstances )
    {
      var groups = new Dictionary<ElementId, IList<ScheduleSheetInstance>>() ;
      foreach ( var scheduleSheetInstance in scheduleSheetInstances ) {
        if ( document.GetElement( scheduleSheetInstance.ScheduleId ) is not ViewSchedule schedule ) continue;
        if(!schedule.GetSplitStatus()) continue;
        var groupId = schedule.GetSplitGroupId() ;
        if(groupId == null) continue;
        if(groups.ContainsKey( groupId ))
          groups[groupId].Add( scheduleSheetInstance );
        else groups.Add( groupId, new List<ScheduleSheetInstance>(){scheduleSheetInstance} );
      }
      var groups1 = new Dictionary<ElementId, IList<ScheduleSheetInstance>>() ;
      foreach ( var groupId in groups.Keys ) {
        groups1.Add( groupId, groups[ groupId ].OrderBy( s => ( document.GetElement( s.ScheduleId ) as ViewSchedule )?.GetSplitIndex() ).ToList()) ;
      }
      return groups1 ;
    }
    private static Result MergeScheduleSheetInstance(Document document, IList<ScheduleSheetInstance> scheduleSheetInstances)
    {
      if(scheduleSheetInstances.Count < 2) return Result.Succeeded; 
      var firstScheduleSheet = scheduleSheetInstances.First() ;
      if ( document.GetElement( firstScheduleSheet.ScheduleId ) is not ViewSchedule firstSchedule )
        return Result.Failed ;
      using Transaction transaction = new Transaction(document,"MergeScheduleSheetInstance") ;
      transaction.Start() ;
      var imageMap = firstSchedule.GetImageMap() ;
      var firstSessionData = firstSchedule.GetTableData().GetSectionData( SectionType.Header ) ;
      var firstSessionDataRowCount = firstSessionData.NumberOfRows ;
      var firstSessionDataColumnCount = firstSessionData.NumberOfColumns ;
      if ( document.GetElement( scheduleSheetInstances[1].ScheduleId ) is not ViewSchedule secondSchedule )
        return Result.Failed ;
      var headerRowCount = firstSchedule.GetHeaderRowCount() ;
      if(headerRowCount == -1) headerRowCount = GetHeaderRowCount( firstSessionData, secondSchedule.GetTableData().GetSectionData( SectionType.Header ) ) ;
      for ( int i = 1 ; i < scheduleSheetInstances.Count ; i++ ) {
        if ( document.GetElement( scheduleSheetInstances[i].ScheduleId ) is not ViewSchedule schedule )
          return Result.Failed ;
        var sectionData = schedule.GetTableData().GetSectionData( SectionType.Header ) ;
        var rowCount = sectionData.NumberOfRows ;
        var columnCount = Math.Min(sectionData.NumberOfColumns,firstSessionDataColumnCount) ;
        for ( int row = headerRowCount ; row < rowCount ; row++ ) {
          firstSessionData.InsertRow( firstSessionDataRowCount );
          firstSessionData.SetRowHeight( firstSessionDataRowCount, sectionData.GetRowHeight( row ));
          for ( int column = 0 ; column < columnCount -1 ; column++ ) {
            firstSessionData.SetCellText( firstSessionDataRowCount, column, sectionData.GetCellText( row, column ));
            firstSessionData.SetCellStyle( firstSessionDataRowCount, column, sectionData.GetTableCellStyle( row, column ));
            firstSessionData.SetCellType( firstSessionDataRowCount, column, sectionData.GetCellType( row, column ));
            if(imageMap.ContainsKey( (firstSessionDataRowCount, column )))
              firstSessionData.InsertImage( firstSessionDataRowCount, column,imageMap[(firstSessionDataRowCount, column)] );
          }
          firstSessionDataRowCount++ ;
        }

        document.Delete( schedule.Id ) ;
      }
      transaction.Commit() ;
      return Result.Succeeded ;
    }
  }
}