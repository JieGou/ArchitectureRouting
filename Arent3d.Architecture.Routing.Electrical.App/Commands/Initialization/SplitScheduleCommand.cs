using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text.RegularExpressions ;
using Arent3d.Architecture.Routing.AppBase.Commands ;
using Arent3d.Revit ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;

namespace Arent3d.Architecture.Routing.Electrical.App.Commands.Initialization
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "Electrical.App.Commands.Initialization.SplitScheduleCommand", DefaultString = "Split Schedule" )]
  [Image( "resources/Initialize-32.bmp", ImageType = Revit.UI.ImageType.Large )]
  public class SplitScheduleCommand : IExternalCommand
  {
    private const string DialogTitle = "Arent Notification" ;

    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elementSet )
    {
      var document = commandData.Application.ActiveUIDocument.Document ;
      var selection = commandData.Application.ActiveUIDocument.Selection ;

      try {
        if ( document.ActiveView is not ViewSheet ) {
          TaskDialog.Show( DialogTitle, "表分割機能はシート上にしか動作しないため、シートへ移動してください。" ) ;
          return Result.Cancelled ;
        }

        var reference = selection.PickObject( ObjectType.Element, "シート上の集計表を選択してください。" ) ;
        if ( document.GetElement( reference ) is not ScheduleSheetInstance sheetInstance )
          return Result.Failed ;

        var boundingBox = sheetInstance.get_BoundingBox( document.ActiveView ) ;

        if ( document.GetElement( sheetInstance.ScheduleId ) is not ViewSchedule schedule )
          return Result.Failed ;
        int splitFromRow = schedule.GetScheduleHeaderRowCount() + 1 ;
        if ( schedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows < splitFromRow && schedule.GetTableData().GetSectionData( SectionType.Body ).LastColumnNumber != -1 )
          return Result.Cancelled ;

        var pickedBox = selection.PickBox( PickBoxStyle.Crossing, "分割範囲を選択してください。" ) ;
        var min = new XYZ( Math.Min( pickedBox.Min.X, pickedBox.Max.X ), Math.Min( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        var max = new XYZ( Math.Max( pickedBox.Min.X, pickedBox.Max.X ), Math.Max( pickedBox.Min.Y, pickedBox.Max.Y ), 0 ) ;
        pickedBox.Min = min ;
        pickedBox.Max = max ;

        if ( pickedBox.Max.Y <= boundingBox.Min.Y || pickedBox.Min.Y >= boundingBox.Max.Y || pickedBox.Max.X <= boundingBox.Min.X || pickedBox.Min.X >= boundingBox.Max.X ) {
          TaskDialog.Show( DialogTitle, "選択された領域は集計表の範囲外です。" ) ;
          return Result.Cancelled ;
        }

        if ( pickedBox.Max.Y >= boundingBox.Max.Y && pickedBox.Min.Y <= boundingBox.Min.Y ) {
          TaskDialog.Show( DialogTitle, "選択された領域は集計表の範囲内です。" ) ;
          return Result.Cancelled ;
        }

        var heightRequest = 0d ;
        for ( int i = 0 ; i < splitFromRow ; i++ ) {
          heightRequest += schedule.GetTableData().GetSectionData( SectionType.Header ).GetRowHeight( i ) ;
        }

        if ( pickedBox.Max.Y >= boundingBox.Max.Y - heightRequest ) {
          TaskDialog.Show( DialogTitle, "選択された領域は無効です。" ) ;
          return Result.Cancelled ;
        }

        using Transaction transaction = new Transaction( document ) ;
        transaction.Start( "Split Schedule" ) ;

        if ( document.GetElement( schedule.Duplicate( ViewDuplicateOption.Duplicate ) ) is not ViewSchedule cloneSchedule )
          return Result.Failed ;
        var (newName, oldName) = GetNewScheduleName( schedule ) ;
        try {
          cloneSchedule.Name = newName ;
        }
        catch ( Autodesk.Revit.Exceptions.ArgumentException ) {
          newName += Guid.NewGuid() ;
          cloneSchedule.Name = newName ;
        }

        schedule.Name = oldName ;

        var (topIndex, bottomIndex) = GetIndexRowIntersect( schedule.GetTableData().GetSectionData( SectionType.Header ), boundingBox, pickedBox, splitFromRow ) ;

        var numberOfRows = schedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( var i = numberOfRows - 1 ; i >= 0 ; i-- ) {
          if ( i >= topIndex && i <= bottomIndex )
            schedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        numberOfRows = cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).NumberOfRows ;
        for ( var i = numberOfRows - 1 ; i >= splitFromRow - 1 ; i-- ) {
          if ( i < topIndex || i > bottomIndex )
            cloneSchedule.GetTableData().GetSectionData( SectionType.Header ).RemoveRow( i ) ;
        }

        ScheduleSheetInstance.Create( document, document.ActiveView.Id, cloneSchedule.Id, Transform.CreateTranslation( XYZ.BasisX * 10.0.MillimetersToRevitUnits() ).OfPoint( boundingBox.Max ) ) ;
        var scheduleSheetInstancesContainSameSchedule = GetScheduleSheetInstancesContainSameSchedule( document, schedule, sheetInstance ) ;
        CreateSplitScheduleSheetInstances( document, scheduleSheetInstancesContainSameSchedule, cloneSchedule ) ;
        var (firstImageMap, secondImageMap) = schedule.SplitImageMap( topIndex, bottomIndex, schedule.GetScheduleHeaderRowCount() ) ;
        schedule.SetImageMap( firstImageMap ) ;
        cloneSchedule.SetImageMap( secondImageMap ) ;
        SetSplitInformation( schedule, cloneSchedule ) ;
        UpdateOtherSchedule( document, schedule, cloneSchedule ) ;
        transaction.Commit() ;

        return Result.Succeeded ;
      }
      catch ( Autodesk.Revit.Exceptions.OperationCanceledException ) {
        return Result.Cancelled ;
      }
      catch ( Exception exception ) {
        CommandUtils.DebugAlertException( exception ) ;
        return Result.Cancelled ;
      }
    }

    private static IList<ScheduleSheetInstance> GetScheduleSheetInstancesContainSameSchedule( Document document, ViewSchedule scheduleToFind, ScheduleSheetInstance excludedScheduleSheet )
    {
      var elementCollector = new FilteredElementCollector( document, document.ActiveView.Id ) ;
      return elementCollector.OfClass( typeof( ScheduleSheetInstance ) ).ToElements().ConvertAll( e => (ScheduleSheetInstance) e ).Where( s => excludedScheduleSheet.Id.IntegerValue != s.Id.IntegerValue && document.GetElement( s.ScheduleId ) is ViewSchedule schedule && schedule.Id.IntegerValue == scheduleToFind.Id.IntegerValue ).ToList() ;
    }

    private static void CreateSplitScheduleSheetInstances( Document document, IList<ScheduleSheetInstance> scheduleSheetInstances, ViewSchedule viewSchedule )
    {
      foreach ( var scheduleSheetInstance in scheduleSheetInstances ) {
        var boundingBox = scheduleSheetInstance.get_BoundingBox( document.ActiveView ) ;
        ScheduleSheetInstance.Create( document, document.ActiveView.Id, viewSchedule.Id, Transform.CreateTranslation( XYZ.BasisX * 10.0.MillimetersToRevitUnits() ).OfPoint( boundingBox.Max ) ) ;
      }
    }

    private static (string NewScheduleName, string OldScheduleName) GetNewScheduleName( ViewSchedule oldSchedule )
    {
      var currentSplitIndex = oldSchedule.GetSplitIndex() <= 0 ? 1 : oldSchedule.GetSplitIndex() ;
      var newSplitIndex = currentSplitIndex + 1 ;
      var currentSplitLevel = oldSchedule.GetSplitLevel() <= 0 ? 1 : oldSchedule.GetSplitLevel() ;
      var scheduleBaseName = oldSchedule.GetScheduleBaseName() ;
      var newSplitLevel = currentSplitLevel + 1 ;
      var oldScheduleName = scheduleBaseName + " " + currentSplitIndex + "/" + newSplitLevel ;
      var newScheduleName = scheduleBaseName + " " + newSplitIndex + "/" + newSplitLevel ;
      return ( newScheduleName, oldScheduleName ) ;
    }

    private static void SetSplitInformation( ViewSchedule currentSchedule, ViewSchedule newSchedule )
    {
      var currentSplitIndex = currentSchedule.GetSplitIndex() <= 0 ? 1 : currentSchedule.GetSplitIndex() ;
      var currentSplitLevel = currentSchedule.GetSplitLevel() <= 0 ? 1 : currentSchedule.GetSplitLevel() ;
      var newSplitLevel = currentSplitLevel + 1 ;
      var parentScheduleId = currentSchedule.GetParentScheduleId() ?? currentSchedule.Id ;
      currentSchedule.SetSplitStatus( true ) ;
      currentSchedule.SetParentScheduleId( parentScheduleId ) ;
      currentSchedule.SetSplitLevel( newSplitLevel ) ;
      currentSchedule.SetSplitIndex( currentSplitIndex ) ;

      newSchedule.SetSplitStatus( true ) ;
      newSchedule.SetSplitIndex( currentSplitIndex + 1 ) ;
      newSchedule.SetParentScheduleId( parentScheduleId ) ;
      newSchedule.SetSplitLevel( newSplitLevel ) ;
      newSchedule.SetSplitIndex( currentSplitIndex + 1 ) ;
    }

    private static void UpdateOtherSchedule( Document document, ViewSchedule currentSchedule, ViewSchedule newSchedule )
    {
      var allSchedules = new FilteredElementCollector( document).OfClass( typeof( ViewSchedule ) ).ToElements().ConvertAll( e => (ViewSchedule) e ).Where( s => s.GetScheduleBaseName().Equals( currentSchedule.GetScheduleBaseName() ) ).ToList() ;
      var otherSchedules = allSchedules.Where( s => s.UniqueId != currentSchedule.UniqueId && s.UniqueId != newSchedule.UniqueId ).ToList() ;
      var newSplitLevel = allSchedules.Count ;
      foreach ( var schedule in otherSchedules ) {
        var splitIndex = schedule.GetSplitIndex() <= 0 ? 1 : schedule.GetSplitIndex() ;
        var newSplitIndex = splitIndex ;
        if ( splitIndex > currentSchedule.GetSplitIndex() ) newSplitIndex += 1 ;
        schedule.SetSplitIndex( newSplitIndex ) ;
        schedule.SetSplitLevel( newSplitLevel ) ;
        schedule.Name = schedule.GetScheduleBaseName() + " " + newSplitIndex + "/" + newSplitLevel ;
      }
    }

    private static (int, int) GetIndexRowIntersect( TableSectionData sectionData, BoundingBoxXYZ boxXyz, PickedBox pickedBox, int splitFromRow )
    {
      var heightTable = boxXyz.Max.Y ;
      var topIndex = splitFromRow - 1 ;
      var bottomIndex = sectionData.NumberOfRows - 1 ;

      for ( var i = 0 ; i < sectionData.NumberOfRows ; i++ ) {
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