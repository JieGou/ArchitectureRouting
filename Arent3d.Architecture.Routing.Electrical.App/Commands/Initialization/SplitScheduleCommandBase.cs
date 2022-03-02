using System ;
using System.Collections.Generic ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  public class SplitScheduleCommandBase : IExternalCommand
  {
    private const string TITLE = "Arent Notification" ;
    private const int SPLIT_FROM_ROW = 4 ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        if ( document.ActiveView is not ViewSheet ) {
          TaskDialog.Show( TITLE, "Only active in sheet!" ) ;
          return Result.Cancelled ;
        }
        
        var reference = selection.PickObject( ObjectType.Element, "Select schedule in sheet!" ) ;
        if ( document.GetElement( reference ) is not ScheduleSheetInstance sheetInstance )
          return Result.Failed ;

        var boundingBoxXYZ = sheetInstance.get_BoundingBox( document.ActiveView ) ;

        if ( document.GetElement( sheetInstance.ScheduleId ) is not ViewSchedule schedule )
          return Result.Failed ;
        
        if ( schedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows < SPLIT_FROM_ROW
            && schedule.GetTableData().GetSectionData( SectionType.Body ).LastColumnNumber != -1)
          return Result.Cancelled ;

        var pickedBox = selection.PickBox( PickBoxStyle.Crossing, "Select the data area to split schedule!" ) ;
        var min = new XYZ( Math.Min( pickedBox.Min.X, pickedBox.Max.X ), Math.Min( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        var max = new XYZ( Math.Max( pickedBox.Min.X, pickedBox.Max.X ), Math.Max( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        pickedBox.Min = min ;
        pickedBox.Max = max ;

        if ( pickedBox.Max.Y <= boundingBoxXYZ.Min.Y || pickedBox.Min.Y >= boundingBoxXYZ.Max.Y || pickedBox.Max.X <= boundingBoxXYZ.Min.X ||
             pickedBox.Min.X >= boundingBoxXYZ.Max.X ) {
          TaskDialog.Show( TITLE, "The selected area is outside the schedule!" ) ;
          return Result.Cancelled ;
        }

        if ( pickedBox.Max.Y >= boundingBoxXYZ.Max.Y && pickedBox.Min.Y <= boundingBoxXYZ.Min.Y ) {
          TaskDialog.Show( TITLE, "The schedule is inside the selected area!" ) ;
          return Result.Cancelled ;
        }

        var heightRequest = 0d ;
        for ( int i = 0 ; i < SPLIT_FROM_ROW - 2 ; i++ ) {
          heightRequest += schedule.GetTableData().GetSectionData( SectionType.Header ).GetRowHeight( i ) ;
        }

        if ( pickedBox.Max.Y >= boundingBoxXYZ.Max.Y - heightRequest ) {
          TaskDialog.Show( TITLE, "The selected area invalid!" ) ;
          return Result.Cancelled ;
        }

        using Transaction transaction = new Transaction( document ) ;
        transaction.Start( "Split Schedule" ) ;

        if ( document.GetElement( schedule.Duplicate( ViewDuplicateOption.Duplicate ) ) is not ViewSchedule cloneSchedule )
          return Result.Failed ;

        cloneSchedule.Name = $"{schedule.Name}_{DateTime.Now.ToString( "HHmmss" )}" ;
        var (topIndex, bottomIndex) =
          GetIndexRowIntersect( schedule.GetTableData().GetSectionData( SectionType.Header ), boundingBoxXYZ, pickedBox ) ;

        var numberOfRows = schedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( int i = numberOfRows - 1 ; i >= 0 ; i-- ) {
          if ( i >= topIndex && i <= bottomIndex )
            schedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        numberOfRows = cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( int i = numberOfRows - 1 ; i >= SPLIT_FROM_ROW - 2 ; i-- ) {
          if ( i < topIndex || i > bottomIndex )
            cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        ScheduleSheetInstance.Create( document, document.ActiveView.Id, cloneSchedule.Id,
          selection.PickPoint( "Select a point to place schedule!" ) ) ;

        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Exception exception ) {
        TaskDialog.Show( "Notification", exception.Message ) ;
        return Result.Cancelled ;
      }
    }

    private (int, int) GetIndexRowIntersect( TableSectionData sectionData, BoundingBoxXYZ boxXYZ, PickedBox pickedBox )
    {
      var heightTable = boxXYZ.Max.Y ;
      var topIndex = SPLIT_FROM_ROW - 1 ;
      var bottomIndex = sectionData.NumberOfRows - 1 ;

      for ( int i = 0 ; i < sectionData.NumberOfRows ; i++ ) {
        heightTable -= sectionData.GetRowHeight( i ) ;

        var old = heightTable + sectionData.GetRowHeight( i ) ;
        if ( ( heightTable <= pickedBox.Max.Y ) && ( pickedBox.Max.Y < old ) )
          topIndex = i ;

        if ( ( heightTable <= pickedBox.Min.Y ) && ( pickedBox.Min.Y < old ) )
          bottomIndex = i ;
      }

      return ( topIndex, bottomIndex ) ;
    }
  }
}