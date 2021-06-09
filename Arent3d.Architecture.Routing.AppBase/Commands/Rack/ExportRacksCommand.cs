using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.IO ;
using Arent3d.Revit ;
using Arent3d.Revit.Csv ;
using Arent3d.Revit.I18n ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.AppBase.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Rack.ExportRacksCommand", DefaultString = "Export\nPS" )]
  [Image( "resources/ExportPS.png" )]
  public class ExportRacksCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      using var dlg = new FileSaveDialog( $"{"Dialog.Commands.Rack.PS.FileName".GetAppStringByKeyOrDefault( "Pipe space list file" )} (*.csv)|*.csv" )
      {
        Title = "Dialog.Commands.Rack.PS.Title.Export".GetAppStringByKeyOrDefault( null )
      } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return Result.Succeeded ;

      WriteFromTo( doc, ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ) ;

      return Result.Succeeded ;
    }

    private static void WriteFromTo( Document document, string csvFileName )
    {
      var fromToList = GetRackList( document ) ;

      using var writer = new StreamWriter( csvFileName, false ) ;
      writer.WriteCsvFile( fromToList ) ;
    }

    private static IEnumerable<RackRecord> GetRackList( Document document )
    {
      foreach ( var familyInstance in document.GetAllFamilyInstances( RoutingFamilyType.RackGuide ) ) {
        var transform = familyInstance.GetTotalTransform() ;
        yield return new RackRecord
        {
          Origin = transform.Origin,
          RotationDegree = Math.Atan2( transform.BasisX.Y, transform.BasisX.X ).Rad2Deg(),
          Size_X = LengthParameterData.From( familyInstance, "Arent-Width" ),
          Size_Y = LengthParameterData.From( familyInstance, "Arent-Height" ),
          Size_Z = LengthParameterData.From( familyInstance, "Arent-Length" ),
          Offset = LengthParameterData.From( familyInstance, "Arent-Offset" ),
          Elevation = LengthParameterData.From( familyInstance, BuiltInParameter.INSTANCE_ELEVATION_PARAM ),
          Level = GetLevelName( document, familyInstance ),
        } ;
      }
    }

    private static string GetLevelName( Document document, FamilyInstance familyInstance )
    {
      return document.GetElementById<Level>( familyInstance.LevelId )?.Name ?? string.Empty ;
    }
  }
}