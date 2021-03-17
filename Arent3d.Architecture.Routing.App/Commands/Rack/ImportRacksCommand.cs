using System ;
using System.Collections.Generic ;
using System.ComponentModel ;
using System.IO ;
using System.Linq ;
using Arent3d.Revit ;
using Arent3d.Revit.Csv ;
using Arent3d.Revit.UI ;
using Arent3d.Utility ;
using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;

namespace Arent3d.Architecture.Routing.App.Commands.Rack
{
  [Transaction( TransactionMode.Manual )]
  [DisplayNameKey( "App.Commands.Rack.ImportRacksCommand", DefaultString = "Import\nPS" )]
  [Image( "resources/ImportPS.png" )]
  public class ImportRacksCommand : IExternalCommand
  {
    public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
    {
      var csvFileName = OpenFromToCsv() ;
      if ( null == csvFileName ) return Result.Cancelled ;

      var list = ReadRackRecordsFromFile( csvFileName ).EnumerateAll() ;
      if ( 0 == list.Count ) return Result.Succeeded ;

      var document = commandData.Application.ActiveUIDocument.Document ;
      using var transaction = new Transaction( document ) ;
      try {
        transaction.Start( "Import racks" ) ;

        foreach ( var rackRecord in list ) {
          GenerateRack( document, rackRecord ) ;
        }

        transaction.Commit() ;
        return Result.Succeeded ;
      }
      catch {
        transaction.RollBack() ;
        return Result.Failed ;
      }
    }

    private static void GenerateRack( Document document, RackRecord rackRecord )
    {
      var symbol = document.GetFamilySymbol( RoutingFamilyType.RackGuide ) ! ;
      var instance = symbol.Instantiate( rackRecord.Origin, rackRecord.Level, StructuralType.NonStructural ) ;

      instance.get_Parameter( BuiltInParameter.INSTANCE_ELEVATION_PARAM ).Set( 0.0 ) ;

      rackRecord.Size_X.To( instance, "幅" ) ; // TODO
      rackRecord.Size_Y.To( instance, "高さ" ) ; // TODO
      rackRecord.Size_Z.To( instance, "奥行き" ) ; // TODO
      rackRecord.Offset.To( instance, "Arent-Offset" ) ;
      rackRecord.Elevation.To( instance, BuiltInParameter.INSTANCE_ELEVATION_PARAM ) ;

      ElementTransformUtils.RotateElement( document, instance.Id, Line.CreateBound( XYZ.Zero, XYZ.BasisZ ), rackRecord.RotationDegree.Deg2Rad() ) ;
      ElementTransformUtils.MoveElement( document, instance.Id, rackRecord.Origin - instance.GetTotalTransform().Origin ) ;
    }

    private static IEnumerable<RackRecord> ReadRackRecordsFromFile( string csvFileName )
    {
      using var reader = new StreamReader( csvFileName, true ) ;
      // Cannot use return directly, because `reader` will be closed in that case.
      foreach ( var item in reader.ReadCsvFile<RackRecord>() ) {
        yield return item ;
      }
    }

    private static string? OpenFromToCsv()
    {
      using var dlg = new FileOpenDialog( "Routing from-to list (*.csv)|*.csv" ) { Title = "Import from-to list file" } ;

      if ( ItemSelectionDialogResult.Confirmed != dlg.Show() ) return null ;

      return ModelPathUtils.ConvertModelPathToUserVisiblePath( dlg.GetSelectedModelPath() ) ;
    }
  }
}