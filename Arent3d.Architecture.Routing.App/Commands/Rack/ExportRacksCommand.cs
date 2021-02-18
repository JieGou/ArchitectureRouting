using System.Collections.Generic ;
using System.ComponentModel ;
using System.Globalization ;
using System.IO ;
using Arent3d.Revit.UI ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.UI ;
using CsvHelper ;
using MathLib ;
using RackInfo = Arent3d.Architecture.Routing.Rack.Rack ;

namespace Arent3d.Architecture.Routing.App.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayName( "Export Racks" )]
  [Image( "resources/MEP.ico" )]
  public class ExportRacksCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var doc = commandData.Application.ActiveUIDocument.Document ;

      using var dlg = new FileSaveDialog( "Routing rack list (*.csv)|*.csv" ) { Title = "Export rack list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return Result.Succeeded ;

      WriteRackList( doc, ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ) ;

      return Result.Succeeded ;
    }

    private static void WriteRackList( Document document, string csvFileName )
    {
      // var rackList = GetRackList( document ) ;
      //
      // using var reader = new StreamWriter( csvFileName, false ) ;
      // using var csv = new CsvWriter( reader, CultureInfo.CurrentCulture ) ;
      // csv.Configuration.HasHeaderRecord = true ;
      //
      // foreach ( var header in RackParser.GetHeaders() ) {
      //   csv.WriteField( header ) ;
      // }
      // csv.NextRecord() ;
      //
      // foreach ( var record in rackList ) {
      //   foreach ( var value in RackParser.GetRow( record ) ) {
      //     csv.WriteField( value ) ;
      //   }
      //   csv.NextRecord() ;
      // }
    }

    // private static IEnumerable<RackInfo> GetRackList( Document document )
    // {
    //   foreach ( var familyInstance in document.GetAllFamilyInstances( RoutingFamilyType.RackGuide ) ) {
    //     var (min, max) = familyInstance.get_BoundingBox( view ).To3d() ;
    //
    //     yield return new RackInfo { Box = new Box3d( min, max ), IsMainRack = true, BeamInterval = 5 } ;
    //   }
    // }
  }
}