using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Runtime.InteropServices ;
using Arent3d.Architecture.Routing.Storable.Model ;
using Arent3d.Revit ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.ExtensibleStorage ;
using Excel = Microsoft.Office.Interop.Excel ;

namespace Arent3d.Architecture.Routing.Storable
{
  [Guid( "998cd31f-ada6-4fbf-ae78-971fc071c030" )]
  [StorableVisibility( AppInfo.VendorId )]
  public class CeedStorable : StorableBase
  {
    public const string StorableName = "CeeD Model" ;
    private const string CeeDModelField = "CeeDModel" ;
    private const string CeeDFileName = "CeeDModelList" ;

    public List<CeedModel> CeedModelData { get ; private set ; }

    public CeedStorable( DataStorage owner ) : base( owner, false )
    {
      CeedModelData = new List<CeedModel>() ;
    }

    public CeedStorable( Document document ) : base( document, false )
    {
      CeedModelData = GetAllCeeDModelNumber( CeeDFileName ) ;
    }

    protected override void LoadAllFields( FieldReader reader )
    {
      CeedModelData = reader.GetArray<CeedModel>( CeeDModelField ).ToList() ;
    }

    protected override void SaveAllFields( FieldWriter writer )
    {
      writer.SetArray( CeeDModelField, CeedModelData ) ;
    }

    protected override void SetupAllFields( FieldGenerator generator )
    {
      generator.SetArray<CeedModel>( CeeDModelField ) ;
    }

    public override string Name => StorableName ;

    private static List<CeedModel> GetAllCeeDModelNumber( string ceeDFileName )
    {
      List<CeedModel> ceedModelData = new List<CeedModel>() ;
      var path = AssetManager.GetCeeDModelPath( ceeDFileName ) ;

      Excel.ApplicationClass app = new Excel.ApplicationClass() ;
      Excel.Workbook workBook = app.Workbooks.Open( path, 0, true, 5, "", "", true, Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0 ) ;
      Excel.Worksheet workSheet = (Excel.Worksheet) workBook.Sheets[ 2 ] ;
      const int startRow = 8 ;
      var endRow = workSheet.Cells.SpecialCells( Excel.XlCellType.xlCellTypeLastCell, Type.Missing ).Row ;
      try {
        for ( int i = startRow ; i <= endRow ; i++ ) {
          List<string> ceeDModelNumbers = new List<string>() ;
          List<string> ceeDSetCodes = new List<string>() ;
          List<string> modelNumbers = new List<string>() ;
          string generalDisplayDeviceSymbols = string.Empty ;
          string floorPlanSymbol = string.Empty ;

          var record = (Excel.Range) workSheet.Cells[ i, 4 ] ;
          if ( record.Rows.Hidden.Equals( true ) ) continue ;
          var name = record.Value2 ;
          if ( name != null ) {
            var firstIndexGroup = i ;
            var nextName = record.Value2 ;
            do {
              i++ ;
              if ( i > endRow ) break ;
              name = nextName ;
              nextName = ( (Excel.Range) workSheet.Cells[ i, 4 ] ).Value2 ;
            } while ( ! ( name == null && nextName != null ) ) ;

            var lastIndexGroup = i ;
            for ( int j = firstIndexGroup ; j < lastIndexGroup ; j++ ) {
              var ceeDSetCode = ( (Excel.Range) workSheet.Cells[ j, 1 ] ).Value2 ;
              if ( ceeDSetCode != null ) ceeDSetCodes.Add( ceeDSetCode.ToString() ) ;

              var ceeDModelNumber = ( (Excel.Range) workSheet.Cells[ j, 2 ] ).Value2 ;
              if ( ceeDModelNumber != null ) ceeDModelNumbers.Add( ceeDModelNumber.ToString() ) ;

              var generalDisplayDeviceSymbol = ( (Excel.Range) workSheet.Cells[ j, 3 ] ).Value2 ;
              if ( generalDisplayDeviceSymbol != null && ! generalDisplayDeviceSymbol.ToString().Contains( "．" ) ) generalDisplayDeviceSymbols = generalDisplayDeviceSymbol.ToString() ;

              var modelNumber = ( (Excel.Range) workSheet.Cells[ j, 5 ] ).Value2 ;
              if ( modelNumber != null ) modelNumbers.Add( modelNumber.ToString() ) ;

              var symbol = ( (Excel.Range) workSheet.Cells[ j, 6 ] ).Value2 ;
              if ( symbol != null && ! symbol.ToString().Contains( "又は" ) ) floorPlanSymbol = symbol.ToString() ;
            }

            var strModelNumbers = modelNumbers.Any() ? string.Join( "\n", modelNumbers ) : string.Empty ;
            if ( ! ceeDModelNumbers.Any() ) {
              CeedModel ceeDModel = new CeedModel( string.Empty, string.Empty, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol ) ;
              ceedModelData.Add( ceeDModel ) ;
            }
            else {
              for ( int k = 0 ; k < ceeDModelNumbers.Count ; k++ ) {
                var ceeDSetCode = ceeDSetCodes.Any() ? ceeDSetCodes[ k ] : string.Empty ;
                CeedModel ceeDModel = new CeedModel( ceeDModelNumbers[ k ], ceeDSetCode, generalDisplayDeviceSymbols, strModelNumbers, floorPlanSymbol ) ;
                ceedModelData.Add( ceeDModel ) ;
              }
            }
          }

          i-- ;
        }
      }
      catch ( Exception ) {
        app.Quit() ;
      }

      return ceedModelData ;
    }
  }
}