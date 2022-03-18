using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Electrical ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using ImageType = Autodesk.Revit.DB.ImageType ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
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
        
        
        var pickedBox = selection.PickBox( PickBoxStyle.Crossing, "分割範囲を選択してください。" ) ;
        var min = new XYZ( Math.Min( pickedBox.Min.X, pickedBox.Max.X ), Math.Min( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        var max = new XYZ( Math.Max( pickedBox.Min.X, pickedBox.Max.X ), Math.Max( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        pickedBox.Min = min ;
        pickedBox.Max = max ;


        if ( pickedBox.Max.Y >= boundingBoxXYZ.Max.Y && pickedBox.Min.Y <= boundingBoxXYZ.Min.Y ) {
          TaskDialog.Show( DialogTitle, "選択された領域は集計表の範囲内です。" ) ;
          return Result.Cancelled ;
        }

        var heightRequest = 0d ;
        for ( int i = 0 ; i < SplitFromRow ; i++ ) {
          heightRequest += schedule.GetTableData().GetSectionData( SectionType.Header ).GetRowHeight( i ) ;
        }

        if ( pickedBox.Max.Y >= boundingBoxXYZ.Max.Y - heightRequest ) {
          TaskDialog.Show( DialogTitle, "選択された領域は無効です。" ) ;
          return Result.Cancelled ;
        }

        using Transaction transaction = new Transaction( document ) ;
        transaction.Start( "Split Schedule" ) ;

        if ( document.GetElement( schedule.Duplicate( ViewDuplicateOption.Duplicate ) ) is not ViewSchedule cloneSchedule )
          return Result.Failed ;

        var (newName, oldName) = GetNewScheduleName( document, schedule.Name ) ;
        cloneSchedule.Name = newName ;
        schedule.Name = oldName ;
        
        var (topIndex, bottomIndex) =
          GetIndexRowIntersect( schedule.GetTableData().GetSectionData( SectionType.Header ), boundingBoxXYZ, pickedBox ) ;

        var numberOfRows = schedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( int i = numberOfRows - 1 ; i >= 0 ; i-- ) {
          if ( i >= topIndex && i <= bottomIndex )
            schedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        numberOfRows = cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( int i = numberOfRows - 1 ; i >= SplitFromRow - 1 ; i-- ) {
          if ( i < topIndex || i > bottomIndex )
            cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        ScheduleSheetInstance.Create( document, document.ActiveView.Id, cloneSchedule.Id,
          Transform.CreateTranslation(XYZ.BasisX * 10.0.MillimetersToRevitUnits()).OfPoint(boundingBoxXYZ.Max) ) ;

        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch (Autodesk.Revit.Exceptions.OperationCanceledException)
      {
        return Result.Cancelled;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException(exception);
        return Result.Cancelled ;
      }
    }

    private (int, int) GetIndexRowIntersect( TableSectionData sectionData, BoundingBoxXYZ boxXYZ, PickedBox pickedBox )
    {
      var heightTable = boxXYZ.Max.Y ;
      var topIndex = SplitFromRow - 1 ;
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

    private (string NewScheduleName, string OldScheduleName) GetNewScheduleName(Document document, string oldScheduleName)
    {
      var prefix = "Splited-" ;
      var pattern = @$"{prefix}(\d+)$" ;
      var match = Regex.Match( oldScheduleName, pattern ) ;
      var scheduleNames = document.GetAllElements<ViewSchedule>().Select( x => x.Name ) ;

      if ( match.Success ) {
        var start = Regex.Split( oldScheduleName, match.Value ).First() ;
        var count = scheduleNames.Where( x => Regex.IsMatch( x, $"{start}{pattern}" ) ).Count() ;
        return ($"{start}{prefix}{count + 1}", oldScheduleName) ;
      }
      
      return ($"{oldScheduleName} {prefix}2", $"{oldScheduleName} {prefix}1");
    }
  }
}